namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Supported vault entry types.
/// </summary>
public enum VaultItemKind
{
    WebsiteCredential = 0,
    EmailCredential = 1,
    SecureNote = 2,
    ApiKey = 3,
    CustomLogin = 4
}
