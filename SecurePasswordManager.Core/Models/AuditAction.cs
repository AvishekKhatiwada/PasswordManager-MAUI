namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Non-secret audit events recorded inside the encrypted database.
/// </summary>
public enum AuditAction
{
    VaultCreated = 0,
    VaultUnlocked = 1,
    VaultLocked = 2,
    CredentialCreated = 3,
    CredentialUpdated = 4,
    CredentialDeleted = 5,
    CredentialRestored = 6,
    CredentialArchived = 7,
    SecureNoteCreated = 8,
    SecureNoteUpdated = 9,
    BackupExported = 10,
    BackupImported = 11,
    SettingsUpdated = 12,
    CredentialPurged = 13,
    SecureNotePurged = 14,
    MasterPasswordChanged = 15
}
