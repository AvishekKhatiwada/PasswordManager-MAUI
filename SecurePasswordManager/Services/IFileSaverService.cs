namespace SecurePasswordManager.Services;

/// <summary>
/// Saves a local file straight into the platform's Downloads location, rather than
/// via the share sheet. Returns false when the platform has no such native path
/// (e.g. iOS) or the user cancels, so callers can fall back to sharing instead.
/// </summary>
public interface IFileSaverService
{
    Task<bool> SaveToDownloadsAsync(string sourceFilePath, string suggestedFileName, CancellationToken cancellationToken = default);
}
