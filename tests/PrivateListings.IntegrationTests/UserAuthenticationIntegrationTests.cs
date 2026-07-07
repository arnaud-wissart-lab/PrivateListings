using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Application.Authentication;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Persistence.Seeding;
using PrivateListings.Web.Components.Account.Pages;

namespace PrivateListings.IntegrationTests;

[Collection(PostgreSqlTestCollectionDefinition.Name)]
public sealed class UserAuthenticationIntegrationTests(PostgreSqlFixture fixture)
{
    private const string ValidPassword = "phrase de passe privee 2026";
    private const string NewPassword = "nouvelle phrase de passe 2026";

    [Fact]
    public async Task InscriptionValideCreeUtilisateurProfilJetonEtAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();

        var result = await accountService.RegisterAsync(new RegisterAccountCommand(
            "membre@example.test",
            "Membre Test",
            ValidPassword,
            "https://localhost/confirm-email",
            CreateRequestContext()));

        Assert.Equal(AccountOperationStatus.RequiresEmailConfirmation, result.Status);
        Assert.NotNull(emailSender.ConfirmationLink);
        Assert.False(emailSender.PasswordWasSent);
        Assert.Equal(1, await dbContext.Users.CountAsync());
        Assert.Equal(1, await dbContext.UserProfiles.CountAsync());
        Assert.Equal(1, await dbContext.AccountSecurityTokens.CountAsync(token => token.ConsumedAt == null));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.Registered"));
    }

    [Fact]
    public async Task InscriptionInvalideRefuseMotDePasseCourt()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();

        var result = await accountService.RegisterAsync(new RegisterAccountCommand(
            "membre@example.test",
            "Membre Test",
            "court",
            "https://localhost/confirm-email",
            CreateRequestContext()));

        Assert.Equal(AccountOperationStatus.Failed, result.Status);
        Assert.Null(emailSender.ConfirmationLink);
        Assert.False(await dbContext.Users.AnyAsync());
        Assert.False(await dbContext.UserProfiles.AnyAsync());
    }

    [Fact]
    public async Task ConfirmationEmailConsommeLeLienEtCreeAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PrivateListingsUser>>();
        var callback = await RegisterAndReadConfirmationLinkAsync(accountService, emailSender);

        var result = await accountService.ConfirmEmailAsync(new ConfirmEmailCommand(
            callback.UserId,
            callback.Code,
            CreateRequestContext()));
        var replay = await accountService.ConfirmEmailAsync(new ConfirmEmailCommand(
            callback.UserId,
            callback.Code,
            CreateRequestContext()));
        var user = await userManager.FindByIdAsync(callback.UserId.ToString("D"));

        Assert.True(result.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.NotNull(user);
        Assert.True(await userManager.IsEmailConfirmedAsync(user));
        Assert.Equal(1, await dbContext.AccountSecurityTokens.CountAsync(token => token.ConsumedAt != null));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.EmailConfirmed"));
    }

    [Fact]
    public async Task LoginValideCreeAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        await RegisterAndConfirmAsync(accountService, emailSender);
        ConfigureHttpContext(scope.ServiceProvider);

        var result = await accountService.LoginAsync(new LoginAccountCommand(
            "membre@example.test",
            ValidPassword,
            RememberMe: false,
            CreateRequestContext()));

        Assert.True(result.Succeeded);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.LoginSucceeded"));
    }

    [Fact]
    public async Task LoginInvalideCreeAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        await RegisterAndConfirmAsync(accountService, emailSender);

        var result = await accountService.LoginAsync(new LoginAccountCommand(
            "membre@example.test",
            "mauvais mot de passe",
            RememberMe: false,
            CreateRequestContext()));

        Assert.False(result.Succeeded);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.LoginFailed"));
    }

    [Fact]
    public async Task ResetPasswordConsommeLeLienEtPermetNouveauLogin()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        await RegisterAndConfirmAsync(accountService, emailSender);

        var requestResult = await accountService.RequestPasswordResetAsync(new RequestPasswordResetCommand(
            "membre@example.test",
            "https://localhost/reset-password",
            CreateRequestContext()));
        var callback = ReadCallback(emailSender.ResetLink);
        var resetResult = await accountService.ResetPasswordAsync(new ResetPasswordCommand(
            callback.UserId,
            callback.Code,
            NewPassword,
            CreateRequestContext()));
        ConfigureHttpContext(scope.ServiceProvider);
        var loginResult = await accountService.LoginAsync(new LoginAccountCommand(
            "membre@example.test",
            NewPassword,
            RememberMe: false,
            CreateRequestContext()));

        Assert.True(requestResult.Succeeded);
        Assert.True(resetResult.Succeeded);
        Assert.True(loginResult.Succeeded);
        Assert.Equal(2, await dbContext.AccountSecurityTokens.CountAsync(token => token.ConsumedAt != null));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.PasswordResetRequested"));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.PasswordResetSucceeded"));
    }

    [Fact]
    public async Task ChangeEmailAttendConfirmationDeLaNouvelleAdresse()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PrivateListingsUser>>();
        await RegisterAndConfirmAsync(accountService, emailSender);
        var user = await userManager.FindByEmailAsync("membre@example.test");
        Assert.NotNull(user);

        var requestResult = await accountService.ChangeEmailAsync(new ChangeEmailCommand(
            user.Id,
            "nouveau@example.test",
            ValidPassword,
            CreateRequestContext(),
            "https://localhost/confirm-email"));
        var userBeforeConfirmation = await userManager.FindByIdAsync(user.Id.ToString("D"));
        var profileBeforeConfirmation = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleAsync(profile => profile.Id == user.UserProfileId);

        Assert.True(requestResult.Succeeded);
        Assert.Equal("nouveau@example.test", emailSender.ConfirmationRecipient);
        Assert.Equal("membre@example.test", userBeforeConfirmation!.Email);
        Assert.True(userBeforeConfirmation.EmailConfirmed);
        Assert.Equal("membre@example.test", profileBeforeConfirmation.EmailAddress?.Value);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.EmailChangeRequested"));

        var callback = ReadCallback(emailSender.ConfirmationLink);
        Assert.Equal("nouveau@example.test", callback.Email);
        var confirmResult = await accountService.ConfirmEmailAsync(new ConfirmEmailCommand(
            callback.UserId,
            callback.Code,
            CreateRequestContext(),
            callback.Email));
        var userAfterConfirmation = await userManager.FindByIdAsync(user.Id.ToString("D"));
        var profileAfterConfirmation = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleAsync(profile => profile.Id == user.UserProfileId);

        Assert.True(confirmResult.Succeeded);
        Assert.Equal("nouveau@example.test", userAfterConfirmation!.Email);
        Assert.Equal("nouveau@example.test", userAfterConfirmation.UserName);
        Assert.True(userAfterConfirmation.EmailConfirmed);
        Assert.Equal("nouveau@example.test", profileAfterConfirmation.EmailAddress?.Value);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.EmailChangeConfirmed"));
    }

    [Fact]
    public async Task PurgeExpiredAsyncSupprimeUniquementLesJetonsExpires()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var tokenStore = scope.ServiceProvider.GetRequiredService<AccountSecurityTokenStore>();
        await RegisterAndConfirmAsync(accountService, emailSender);
        var user = await dbContext.Users.SingleAsync();
        var now = DateTimeOffset.UtcNow;

        dbContext.AccountSecurityTokens.Add(AccountSecurityToken.Create(
            user.Id,
            "TestExpired",
            "jeton-expire",
            now.AddHours(-3),
            now.AddHours(-2)));
        dbContext.AccountSecurityTokens.Add(AccountSecurityToken.Create(
            user.Id,
            "TestActive",
            "jeton-actif",
            now.AddMinutes(-5),
            now.AddHours(1)));
        await dbContext.SaveChangesAsync();

        var deletedCount = await tokenStore.PurgeExpiredAsync();

        Assert.Equal(1, deletedCount);
        Assert.False(await dbContext.AccountSecurityTokens.AnyAsync(token => token.Purpose == "TestExpired"));
        Assert.True(await dbContext.AccountSecurityTokens.AnyAsync(token => token.Purpose == "TestActive"));
    }

    [Fact]
    public void AccesNonAutoriseEstRefuseSurLaPageSecuriteCompte()
    {
        var attributes = typeof(AccountSecurity).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

        Assert.NotEmpty(attributes);
    }

    private ServiceProvider CreateServiceProvider(CapturingEmailSender emailSender)
        => fixture.CreateServiceProvider(configureServices: services =>
        {
            services.AddSingleton<IEmailSender<PrivateListingsUser>>(emailSender);
        });

    private static async Task InitializeIdentityAsync(IServiceProvider serviceProvider)
    {
        ConfigureHttpContext(serviceProvider);
        await serviceProvider.GetRequiredService<PrivateListingsIdentitySeeder>().SeedAsync();
    }

    private static void ConfigureHttpContext(IServiceProvider serviceProvider)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Headers["User-Agent"] = "PrivateListings.Tests";

        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
    }

    private static async Task RegisterAndConfirmAsync(
        IUserAccountService accountService,
        CapturingEmailSender emailSender)
    {
        var callback = await RegisterAndReadConfirmationLinkAsync(accountService, emailSender);
        var result = await accountService.ConfirmEmailAsync(new ConfirmEmailCommand(
            callback.UserId,
            callback.Code,
            CreateRequestContext()));

        Assert.True(result.Succeeded);
    }

    private static async Task<CallbackValues> RegisterAndReadConfirmationLinkAsync(
        IUserAccountService accountService,
        CapturingEmailSender emailSender)
    {
        var registerResult = await accountService.RegisterAsync(new RegisterAccountCommand(
            "membre@example.test",
            "Membre Test",
            ValidPassword,
            "https://localhost/confirm-email",
            CreateRequestContext()));

        Assert.Equal(AccountOperationStatus.RequiresEmailConfirmation, registerResult.Status);
        return ReadCallback(emailSender.ConfirmationLink);
    }

    private static CallbackValues ReadCallback(string? link)
    {
        Assert.False(string.IsNullOrWhiteSpace(link));

        var uri = new Uri(link);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var userId = Guid.Parse(query["userId"].ToString());
        var code = query["code"].ToString();
        var email = query.TryGetValue("email", out var emailValues)
            ? emailValues.ToString()
            : null;

        Assert.False(string.IsNullOrWhiteSpace(code));
        return new CallbackValues(userId, code, string.IsNullOrWhiteSpace(email) ? null : email);
    }

    private static AccountRequestContext CreateRequestContext()
        => new("127.0.0.1", "PrivateListings.Tests");

    private sealed record CallbackValues(Guid UserId, string Code, string? Email);

    private sealed class CapturingEmailSender : IEmailSender<PrivateListingsUser>
    {
        public string? ConfirmationLink { get; private set; }

        public string? ConfirmationRecipient { get; private set; }

        public string? ResetLink { get; private set; }

        public bool PasswordWasSent { get; private set; }

        public Task SendConfirmationLinkAsync(
            PrivateListingsUser user,
            string email,
            string confirmationLink)
        {
            ConfirmationRecipient = email;
            ConfirmationLink = confirmationLink;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(
            PrivateListingsUser user,
            string email,
            string resetLink)
        {
            ResetLink = resetLink;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(
            PrivateListingsUser user,
            string email,
            string resetCode)
        {
            PasswordWasSent = true;
            return Task.CompletedTask;
        }
    }
}
