namespace PrivateListings.Application.Authentication;

public interface IAccountPasswordPolicy
{
    AccountPasswordValidationResult Validate(string? password);
}
