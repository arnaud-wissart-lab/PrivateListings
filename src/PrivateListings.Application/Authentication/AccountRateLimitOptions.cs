namespace PrivateListings.Application.Authentication;

public sealed class AccountRateLimitOptions
{
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan BlockDuration { get; set; } = TimeSpan.FromMinutes(10);

    public int LoginPermitLimit { get; set; } = 5;

    public int RegisterPermitLimit { get; set; } = 3;

    public int ForgotPasswordPermitLimit { get; set; } = 3;

    public int ResetPasswordPermitLimit { get; set; } = 5;

    public int ConfirmEmailPermitLimit { get; set; } = 10;

    public int GetPermitLimit(AccountRateLimitAction action)
        => action switch
        {
            AccountRateLimitAction.Login => LoginPermitLimit,
            AccountRateLimitAction.Register => RegisterPermitLimit,
            AccountRateLimitAction.ForgotPassword => ForgotPasswordPermitLimit,
            AccountRateLimitAction.ResetPassword => ResetPasswordPermitLimit,
            AccountRateLimitAction.ConfirmEmail => ConfirmEmailPermitLimit,
            _ => LoginPermitLimit
        };
}
