using Microsoft.Extensions.Options;

namespace PrivateListings.Application.Authentication;

public sealed class AccountPasswordPolicy(IOptions<AccountPasswordPolicyOptions> options) : IAccountPasswordPolicy
{
    public AccountPasswordValidationResult Validate(string? password)
    {
        var policy = options.Value;

        if (string.IsNullOrEmpty(password))
        {
            return AccountPasswordValidationResult.Failed("Le mot de passe est obligatoire.");
        }

        if (password.Length < policy.RequiredLength)
        {
            return AccountPasswordValidationResult.Failed(
                $"Le mot de passe doit contenir au moins {policy.RequiredLength} caractères.");
        }

        if (password.Length > policy.MaximumLength)
        {
            return AccountPasswordValidationResult.Failed(
                $"Le mot de passe ne doit pas dépasser {policy.MaximumLength} caractères.");
        }

        if (password.Any(char.IsControl))
        {
            return AccountPasswordValidationResult.Failed(
                "Le mot de passe ne doit pas contenir de caractères de contrôle.");
        }

        if (password.All(char.IsWhiteSpace))
        {
            return AccountPasswordValidationResult.Failed(
                "Le mot de passe ne peut pas contenir uniquement des espaces.");
        }

        return AccountPasswordValidationResult.Success;
    }
}
