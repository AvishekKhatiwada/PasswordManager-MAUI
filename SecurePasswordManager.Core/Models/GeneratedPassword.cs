namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Generated secret plus strength metadata.
/// </summary>
public sealed class GeneratedPassword
{
    public string Value { get; init; } = string.Empty;

    public double EntropyBits { get; init; }

    public string StrengthLabel { get; init; } = "Weak";

    public double StrengthScore { get; init; }
}
