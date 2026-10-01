namespace SecurePasswordManager.Services;

public interface IBiometricService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<bool> AuthenticateAsync(string title, string reason, CancellationToken cancellationToken = default);
}
