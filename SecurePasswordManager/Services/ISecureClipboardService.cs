namespace SecurePasswordManager.Services;

public interface ISecureClipboardService
{
    Task CopySecretAsync(string secret, int clearAfterSeconds, CancellationToken cancellationToken = default);
}
