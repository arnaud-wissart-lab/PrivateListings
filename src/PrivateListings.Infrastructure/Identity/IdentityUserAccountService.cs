using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PrivateListings.Application.Authentication;
using PrivateListings.Domain.Auditing;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Persistence;

namespace PrivateListings.Infrastructure.Identity;

public sealed class IdentityUserAccountService(
    PrivateListingsDbContext dbContext,
    UserManager<PrivateListingsUser> userManager,
    SignInManager<PrivateListingsUser> signInManager,
    RoleManager<PrivateListingsRole> roleManager,
    IEmailSender<PrivateListingsUser> emailSender,
    AccountSecurityTokenStore securityTokenStore,
    IAccountRateLimiter rateLimiter,
    IAccountPasswordPolicy passwordPolicy,
    IOptions<DataProtectionTokenProviderOptions> tokenOptions,
    TimeProvider timeProvider)
    : IUserAccountService
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AccountOperationResult> RegisterAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = await CheckRateLimitAsync(
            AccountRateLimitAction.Register,
            command.Email,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        if (!TryCreateEmail(command.Email, out var emailAddress))
        {
            return AccountOperationResult.Failed("L'inscription n'a pas pu aboutir.");
        }

        var passwordValidation = passwordPolicy.Validate(command.Password);
        if (!passwordValidation.Succeeded)
        {
            return AccountOperationResult.Failed(passwordValidation.Errors.ToArray());
        }

        if (await userManager.FindByEmailAsync(emailAddress.Value).ConfigureAwait(false) is not null)
        {
            return AccountOperationResult.Failed("L'inscription n'a pas pu aboutir.");
        }

        await EnsureRoleExistsAsync(PrivateListingsRoleNames.Member, cancellationToken).ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        var displayName = NormalizeDisplayName(command.DisplayName, emailAddress.Value);
        var profile = UserProfile.Create(emailAddress, displayName, now);
        var user = new PrivateListingsUser
        {
            Id = Guid.NewGuid(),
            UserName = emailAddress.Value,
            Email = emailAddress.Value,
            EmailConfirmed = false,
            UserProfileId = profile.Id,
            DisplayName = displayName,
            CreatedAt = now,
            MfaRequiredForAdmin = false
        };

        dbContext.UserProfiles.Add(profile);

        var result = await userManager.CreateAsync(user, command.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AccountOperationResult.Failed(ToPublicErrors(result).ToArray());
        }

        var roleResult = await userManager.AddToRoleAsync(user, PrivateListingsRoleNames.Member).ConfigureAwait(false);
        if (!roleResult.Succeeded)
        {
            return AccountOperationResult.Failed("L'inscription n'a pas pu aboutir.");
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
        await securityTokenStore.StoreAsync(
            user.Id,
            AccountSecurityTokenStore.EmailConfirmationPurpose,
            token,
            tokenOptions.Value.TokenLifespan,
            cancellationToken).ConfigureAwait(false);

        var confirmationLink = BuildCallbackUrl(command.ConfirmationUrlBase, user.Id, token);
        await emailSender.SendConfirmationLinkAsync(user, emailAddress.Value, confirmationLink).ConfigureAwait(false);

        await WriteAuditAsync(
            profile.Id,
            "Account.Registered",
            AuditLogSubject.ForUserProfile(profile.Id),
            command.RequestContext,
            new Dictionary<string, object?>
            {
                ["emailHash"] = HashForAudit(emailAddress.Value)
            },
            cancellationToken).ConfigureAwait(false);

        return AccountOperationResult.RequiresEmailConfirmation(user.Id);
    }

    public async Task<AccountOperationResult> LoginAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = await CheckRateLimitAsync(
            AccountRateLimitAction.Login,
            command.Email,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        var normalizedEmail = NormalizeEmail(command.Email);
        var user = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : await userManager.FindByEmailAsync(normalizedEmail).ConfigureAwait(false);

        if (user is null)
        {
            await WriteAuditAsync(
                null,
                "Account.LoginFailed",
                AuditLogSubject.ForSystem("Authentication"),
                command.RequestContext,
                new Dictionary<string, object?>
                {
                    ["emailHash"] = HashForAudit(command.Email),
                    ["reason"] = "UnknownAccount"
                },
                cancellationToken).ConfigureAwait(false);

            return AccountOperationResult.Failed("Identifiants invalides.");
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            command.Password,
            command.RememberMe,
            lockoutOnFailure: true).ConfigureAwait(false);

        if (result.Succeeded)
        {
            await rateLimiter.ResetAsync(
                BuildRateLimitRequest(AccountRateLimitAction.Login, command.Email, command.RequestContext),
                cancellationToken).ConfigureAwait(false);

            await WriteAuditAsync(
                user.UserProfileId,
                "Account.LoginSucceeded",
                AuditLogSubject.ForUserProfile(user.UserProfileId),
                command.RequestContext,
                null,
                cancellationToken).ConfigureAwait(false);

            return AccountOperationResult.Success(user.Id);
        }

        if (result.RequiresTwoFactor)
        {
            return AccountOperationResult.RequiresTwoFactor(user.Id);
        }

        var reason = result.IsLockedOut
            ? "LockedOut"
            : result.IsNotAllowed
                ? "NotAllowed"
                : "InvalidCredentials";

        await WriteAuditAsync(
            user.UserProfileId,
            "Account.LoginFailed",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            command.RequestContext,
            new Dictionary<string, object?>
            {
                ["reason"] = reason
            },
            cancellationToken).ConfigureAwait(false);

        if (result.IsLockedOut)
        {
            return AccountOperationResult.LockedOut();
        }

        if (result.IsNotAllowed && !await userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
        {
            return AccountOperationResult.RequiresEmailConfirmation(user.Id);
        }

        return AccountOperationResult.Failed("Identifiants invalides.");
    }

    public async Task<AccountOperationResult> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = await CheckRateLimitAsync(
            AccountRateLimitAction.ConfirmEmail,
            command.UserId.ToString("D"),
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null || !TryDecodeToken(command.EncodedToken, out var token))
        {
            return AccountOperationResult.Failed("Le lien de confirmation est invalide.");
        }

        if (!string.IsNullOrWhiteSpace(command.NewEmail))
        {
            return await ConfirmEmailChangeAsync(
                user,
                command.NewEmail,
                token,
                command.RequestContext,
                cancellationToken).ConfigureAwait(false);
        }

        var canUseToken = await securityTokenStore.TryConsumeAsync(
            user.Id,
            AccountSecurityTokenStore.EmailConfirmationPurpose,
            token,
            cancellationToken).ConfigureAwait(false);

        if (!canUseToken)
        {
            return AccountOperationResult.Failed("Le lien de confirmation est invalide ou expiré.");
        }

        var result = await userManager.ConfirmEmailAsync(user, token).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AccountOperationResult.Failed("Le lien de confirmation est invalide ou expiré.");
        }

        await WriteAuditAsync(
            user.UserProfileId,
            "Account.EmailConfirmed",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            command.RequestContext,
            null,
            cancellationToken).ConfigureAwait(false);

        return AccountOperationResult.Success(user.Id);
    }

    private async Task<AccountOperationResult> ConfirmEmailChangeAsync(
        PrivateListingsUser user,
        string newEmailValue,
        string token,
        AccountRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (!TryCreateEmail(newEmailValue, out var newEmail))
        {
            return AccountOperationResult.Failed("Le lien de confirmation est invalide.");
        }

        var existingUser = await userManager.FindByEmailAsync(newEmail.Value).ConfigureAwait(false);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            return AccountOperationResult.Failed("La nouvelle adresse email ne peut pas être utilisée.");
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            var canUseToken = await securityTokenStore.TryConsumeAsync(
                user.Id,
                AccountSecurityTokenStore.EmailChangePurpose,
                token,
                cancellationToken).ConfigureAwait(false);

            if (!canUseToken)
            {
                return AccountOperationResult.Failed("Le lien de confirmation est invalide ou expiré.");
            }

            var previousEmail = user.Email;
            var emailResult = await userManager.ChangeEmailAsync(user, newEmail.Value, token).ConfigureAwait(false);
            if (!emailResult.Succeeded)
            {
                return AccountOperationResult.Failed("Le lien de confirmation est invalide ou expiré.");
            }

            var userNameResult = await userManager.SetUserNameAsync(user, newEmail.Value).ConfigureAwait(false);
            if (!userNameResult.Succeeded)
            {
                return AccountOperationResult.Failed("La modification de l'email n'a pas pu aboutir.");
            }

            var profile = await dbContext.UserProfiles
                .SingleAsync(profile => profile.Id == user.UserProfileId, cancellationToken)
                .ConfigureAwait(false);

            profile.ChangeEmailAddress(newEmail);

            await WriteAuditAsync(
                user.UserProfileId,
                "Account.EmailChangeConfirmed",
                AuditLogSubject.ForUserProfile(user.UserProfileId),
                requestContext,
                new Dictionary<string, object?>
                {
                    ["previousEmailHash"] = HashForAudit(previousEmail),
                    ["newEmailHash"] = HashForAudit(newEmail.Value)
                },
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return AccountOperationResult.Success(user.Id);
        }).ConfigureAwait(false);
    }

    public async Task<AccountOperationResult> RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = await CheckRateLimitAsync(
            AccountRateLimitAction.ForgotPassword,
            command.Email,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        var normalizedEmail = NormalizeEmail(command.Email);
        var user = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : await userManager.FindByEmailAsync(normalizedEmail).ConfigureAwait(false);

        if (user is not null && await userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            await securityTokenStore.StoreAsync(
                user.Id,
                AccountSecurityTokenStore.PasswordResetPurpose,
                token,
                tokenOptions.Value.TokenLifespan,
                cancellationToken).ConfigureAwait(false);

            var resetLink = BuildCallbackUrl(command.ResetPasswordUrlBase, user.Id, token);
            await emailSender.SendPasswordResetLinkAsync(user, user.Email!, resetLink).ConfigureAwait(false);

            await WriteAuditAsync(
                user.UserProfileId,
                "Account.PasswordResetRequested",
                AuditLogSubject.ForUserProfile(user.UserProfileId),
                command.RequestContext,
                null,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WriteAuditAsync(
                null,
                "Account.PasswordResetRequested",
                AuditLogSubject.ForSystem("Authentication"),
                command.RequestContext,
                new Dictionary<string, object?>
                {
                    ["emailHash"] = HashForAudit(command.Email),
                    ["matchedAccount"] = false
                },
                cancellationToken).ConfigureAwait(false);
        }

        return AccountOperationResult.Success();
    }

    public async Task<AccountOperationResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = await CheckRateLimitAsync(
            AccountRateLimitAction.ResetPassword,
            command.UserId.ToString("D"),
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        var passwordValidation = passwordPolicy.Validate(command.Password);
        if (!passwordValidation.Succeeded)
        {
            return AccountOperationResult.Failed(passwordValidation.Errors.ToArray());
        }

        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null || !TryDecodeToken(command.EncodedToken, out var token))
        {
            return AccountOperationResult.Failed("Le lien de réinitialisation est invalide.");
        }

        var canUseToken = await securityTokenStore.TryConsumeAsync(
            user.Id,
            AccountSecurityTokenStore.PasswordResetPurpose,
            token,
            cancellationToken).ConfigureAwait(false);

        if (!canUseToken)
        {
            return AccountOperationResult.Failed("Le lien de réinitialisation est invalide ou expiré.");
        }

        var result = await userManager.ResetPasswordAsync(user, token, command.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AccountOperationResult.Failed(ToPublicErrors(result).ToArray());
        }

        await WriteAuditAsync(
            user.UserProfileId,
            "Account.PasswordResetSucceeded",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            command.RequestContext,
            null,
            cancellationToken).ConfigureAwait(false);

        return AccountOperationResult.Success(user.Id);
    }

    public async Task<AccountSecurityOverview?> GetSecurityOverviewAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var isAdmin = await userManager.IsInRoleAsync(user, PrivateListingsRoleNames.Admin).ConfigureAwait(false);
        var isLockedOut = await userManager.IsLockedOutAsync(user).ConfigureAwait(false);
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user).ConfigureAwait(false);

        return new AccountSecurityOverview(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            isAdmin || user.MfaRequiredForAdmin,
            isLockedOut,
            lockoutEnd);
    }

    public async Task<AccountOperationResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return AccountOperationResult.NotFound();
        }

        var passwordValidation = passwordPolicy.Validate(command.NewPassword);
        if (!passwordValidation.Succeeded)
        {
            return AccountOperationResult.Failed(passwordValidation.Errors.ToArray());
        }

        var result = await userManager.ChangePasswordAsync(
            user,
            command.CurrentPassword,
            command.NewPassword).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return AccountOperationResult.Failed("Le mot de passe actuel est invalide.");
        }

        await WriteAuditAsync(
            user.UserProfileId,
            "Account.PasswordChanged",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            command.RequestContext,
            null,
            cancellationToken).ConfigureAwait(false);

        await signInManager.RefreshSignInAsync(user).ConfigureAwait(false);

        return AccountOperationResult.Success(user.Id);
    }

    public async Task<AccountOperationResult> ChangeEmailAsync(
        ChangeEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return AccountOperationResult.NotFound();
        }

        if (!TryCreateEmail(command.NewEmail, out var newEmail))
        {
            return AccountOperationResult.Failed("La nouvelle adresse email est invalide.");
        }

        if (!await userManager.CheckPasswordAsync(user, command.CurrentPassword).ConfigureAwait(false))
        {
            return AccountOperationResult.Failed("Le mot de passe actuel est invalide.");
        }

        var existingUser = await userManager.FindByEmailAsync(newEmail.Value).ConfigureAwait(false);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            return AccountOperationResult.Failed("La nouvelle adresse email ne peut pas être utilisée.");
        }

        if (string.IsNullOrWhiteSpace(command.ConfirmationUrlBase))
        {
            return AccountOperationResult.Failed("La modification de l'email n'a pas pu aboutir.");
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail.Value).ConfigureAwait(false);
        await securityTokenStore.StoreAsync(
            user.Id,
            AccountSecurityTokenStore.EmailChangePurpose,
            token,
            tokenOptions.Value.TokenLifespan,
            cancellationToken).ConfigureAwait(false);

        var confirmationLink = BuildCallbackUrl(command.ConfirmationUrlBase, user.Id, token, newEmail.Value);
        await emailSender.SendConfirmationLinkAsync(user, newEmail.Value, confirmationLink).ConfigureAwait(false);

        await WriteAuditAsync(
            user.UserProfileId,
            "Account.EmailChangeRequested",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            command.RequestContext,
            new Dictionary<string, object?>
            {
                ["previousEmailHash"] = HashForAudit(user.Email),
                ["newEmailHash"] = HashForAudit(newEmail.Value)
            },
            cancellationToken).ConfigureAwait(false);

        return AccountOperationResult.Success(user.Id);
    }

    public async Task<AccountOperationResult> RequestAccountDeletionAsync(
        RequestAccountDeletionCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return AccountOperationResult.NotFound();
        }

        if (!await userManager.CheckPasswordAsync(user, command.CurrentPassword).ConfigureAwait(false))
        {
            return AccountOperationResult.Failed("Le mot de passe actuel est invalide.");
        }

        await WriteAuditAsync(
            user.UserProfileId,
            "Account.DeletionRequested",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            command.RequestContext,
            null,
            cancellationToken).ConfigureAwait(false);

        return AccountOperationResult.Success(user.Id);
    }

    private async Task<AccountOperationResult?> CheckRateLimitAsync(
        AccountRateLimitAction action,
        string? identifier,
        AccountRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var lease = await rateLimiter.TryAcquireAsync(
            BuildRateLimitRequest(action, identifier, requestContext),
            cancellationToken).ConfigureAwait(false);

        return lease.IsAllowed ? null : AccountOperationResult.RateLimited(lease.RetryAfter);
    }

    private static AccountRateLimitRequest BuildRateLimitRequest(
        AccountRateLimitAction action,
        string? identifier,
        AccountRequestContext requestContext)
        => new(action, identifier, requestContext.RemoteIpAddress);

    private async Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await roleManager.CreateAsync(new PrivateListingsRole(roleName)).ConfigureAwait(false);
    }

    private async Task WriteAuditAsync(
        UserProfileId? actorUserProfileId,
        string action,
        AuditLogSubject subject,
        AccountRequestContext requestContext,
        IDictionary<string, object?>? metadata,
        CancellationToken cancellationToken)
    {
        var auditMetadata = new Dictionary<string, object?>
        {
            ["remoteIpHash"] = HashForAudit(requestContext.RemoteIpAddress),
            ["userAgentHash"] = HashForAudit(requestContext.UserAgent)
        };

        if (metadata is not null)
        {
            foreach (var item in metadata)
            {
                auditMetadata[item.Key] = item.Value;
            }
        }

        dbContext.AuditLogs.Add(AuditLog.Create(
            actorUserProfileId,
            action,
            subject,
            timeProvider.GetUtcNow(),
            JsonSerializer.Serialize(auditMetadata, MetadataJsonOptions)));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildCallbackUrl(
        string callbackUrlBase,
        Guid userId,
        string token,
        string? newEmail = null)
    {
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var query = new Dictionary<string, string?>
        {
            ["userId"] = userId.ToString("D"),
            ["code"] = encodedToken
        };

        if (!string.IsNullOrWhiteSpace(newEmail))
        {
            query["email"] = newEmail;
        }

        return QueryHelpers.AddQueryString(callbackUrlBase, query);
    }

    private static bool TryDecodeToken(string encodedToken, out string token)
    {
        token = string.Empty;

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            return !string.IsNullOrWhiteSpace(token);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryCreateEmail(string value, out EmailAddress emailAddress)
    {
        try
        {
            emailAddress = EmailAddress.Create(value);
            return true;
        }
        catch (ArgumentException)
        {
            emailAddress = default;
            return false;
        }
    }

    private static string NormalizeEmail(string value)
        => TryCreateEmail(value, out var emailAddress) ? emailAddress.Value : string.Empty;

    private static string NormalizeDisplayName(string value, string email)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        return atIndex > 0 ? email[..atIndex] : "Membre";
    }

    private static IEnumerable<string> ToPublicErrors(IdentityResult result)
        => result.Errors.Select(error => error.Description);

    private static string HashForAudit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash);
    }
}
