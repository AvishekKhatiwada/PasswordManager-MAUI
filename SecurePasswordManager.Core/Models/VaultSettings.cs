namespace SecurePasswordManager.Core.Models;

/// <summary>
/// User preferences stored in the encrypted database.
/// </summary>
public sealed class VaultSettings
{
    public string Theme { get; set; } = "System";

    public int AutoLockMinutes { get; set; } = 5;

    public int ClipboardClearSeconds { get; set; } = 30;

    public bool BiometricsEnabled { get; set; }

    public bool HighContrast { get; set; }
}
