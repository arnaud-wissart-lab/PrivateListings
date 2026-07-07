namespace PrivateListings.Application.Authentication;

public sealed record RegisterAccountCommand(
    string Email,
    string DisplayName,
    string Password,
    string ConfirmationUrlBase,
    AccountRequestContext RequestContext);

public sealed record LoginAccountCommand(
    string Email,
    string Password,
    bool RememberMe,
    AccountRequestContext RequestContext);

public sealed record ConfirmEmailCommand(
    Guid UserId,
    string EncodedToken,
    AccountRequestContext RequestContext,
    string? NewEmail = null);

public sealed record RequestPasswordResetCommand(
    string Email,
    string ResetPasswordUrlBase,
    AccountRequestContext RequestContext);

public sealed record ResetPasswordCommand(
    Guid UserId,
    string EncodedToken,
    string Password,
    AccountRequestContext RequestContext);

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    AccountRequestContext RequestContext);

public sealed record ChangeEmailCommand(
    Guid UserId,
    string NewEmail,
    string CurrentPassword,
    AccountRequestContext RequestContext,
    string? ConfirmationUrlBase = null);

public sealed record RequestAccountDeletionCommand(
    Guid UserId,
    string CurrentPassword,
    AccountRequestContext RequestContext);
