namespace PrivateListings.Application.Authentication;

public interface IUserAccountService
{
    Task<AccountOperationResult> RegisterAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResult> LoginAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResult> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResult> RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AccountSecurityOverview?> GetSecurityOverviewAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResult> ChangeEmailAsync(
        ChangeEmailCommand command,
        CancellationToken cancellationToken = default);

    Task<AccountOperationResult> RequestAccountDeletionAsync(
        RequestAccountDeletionCommand command,
        CancellationToken cancellationToken = default);
}
