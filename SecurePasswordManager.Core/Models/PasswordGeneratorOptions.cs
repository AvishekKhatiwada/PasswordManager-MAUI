namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Options for cryptographically secure password and passphrase generation.
/// </summary>
public sealed class PasswordGeneratorOptions
{
    public int Length { get; set; } = 24;

    public bool UseUppercase { get; set; } = true;

    public bool UseLowercase { get; set; } = true;

    public bool UseNumbers { get; set; } = true;

    public bool UseSymbols { get; set; } = true;

    public bool ExcludeAmbiguous { get; set; } = true;

    public bool PassphraseMode { get; set; }

    public int PassphraseWords { get; set; } = 5;
}
