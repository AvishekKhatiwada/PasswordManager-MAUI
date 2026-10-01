using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace SecurePasswordManager.Services;

/// <summary>
/// Native biometric/device authentication wrapper with master-password fallback in the calling flow.
/// </summary>
public sealed class BiometricService : IBiometricService
{
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await CrossFingerprint.Current.IsAvailableAsync(allowAlternativeAuthentication: true);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string title, string reason, CancellationToken cancellationToken = default)
    {
        if (!await IsAvailableAsync(cancellationToken))
        {
            return false;
        }

        var request = new AuthenticationRequestConfiguration(title, reason)
        {
            AllowAlternativeAuthentication = true,
            ConfirmationRequired = true
        };

        try
        {
            var result = await CrossFingerprint.Current.AuthenticateAsync(request, cancellationToken);
            return result.Authenticated;
        }
        catch
        {
            return false;
        }
    }
}
