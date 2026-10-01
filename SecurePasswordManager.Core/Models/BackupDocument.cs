namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Decrypted backup payload shape. The serialized wrapper is encrypted with AES-GCM.
/// </summary>
public sealed class BackupDocument
{
    public int Version { get; set; } = 1;

    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    public List<Category> Categories { get; set; } = [];

    public List<CredentialItem> Credentials { get; set; } = [];

    public List<SecureNote> SecureNotes { get; set; } = [];

    public VaultSettings Settings { get; set; } = new();
}
