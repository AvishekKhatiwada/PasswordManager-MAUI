namespace SecurePasswordManager.Core.Security;

/// <summary>
/// Password strength validation outcome used by onboarding and tests.
/// </summary>
public sealed class PasswordValidationResult
{
    public bool IsValid => Errors.Count == 0 && Score >= 0.65;

    public double Score { get; init; }

    public string Label { get; init; } = "Weak";

    public List<string> Errors { get; init; } = [];
}
