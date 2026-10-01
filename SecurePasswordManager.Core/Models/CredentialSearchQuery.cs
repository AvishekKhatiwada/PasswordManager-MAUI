namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Search and paging parameters for vault entries.
/// </summary>
public sealed class CredentialSearchQuery
{
    public string SearchText { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    public bool IncludeArchived { get; set; }

    public bool IncludeDeleted { get; set; }

    public bool FavoritesOnly { get; set; }

    public int Limit { get; set; } = 80;

    public int Offset { get; set; }
}
