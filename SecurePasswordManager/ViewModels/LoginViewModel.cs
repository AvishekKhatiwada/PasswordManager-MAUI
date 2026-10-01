using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authentication;
    private readonly INavigationService _navigation;
    private readonly IBiometricService _biometrics;
    private readonly IBiometricCredentialStore _credentialStore;
    private readonly IToastService _toast;

    [ObservableProperty]
    private string masterPassword = string.Empty;

    [ObservableProperty]
    private bool canUseBiometrics;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public LoginViewModel(
        IAuthenticationService authentication,
        INavigationService navigation,
        IBiometricService biometrics,
        IBiometricCredentialStore credentialStore,
        IToastService toast)
    {
        _authentication = authentication;
        _navigation = navigation;
        _biometrics = biometrics;
        _credentialStore = credentialStore;
        _toast = toast;
        Title = "Unlock vault";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var key = await _credentialStore.TryGetDatabaseKeyAsync();
        if (key is not null)
        {
            CryptographicOperations.ZeroMemory(key);
        }

        CanUseBiometrics = key is not null && await _biometrics.IsAvailableAsync();
    }

    [RelayCommand]
    private async Task UnlockAsync()
    {
        await RunBusyAsync(async () =>
        {
            ErrorMessage = string.Empty;
            var result = await _authentication.UnlockAsync(MasterPassword);
            if (!result.Succeeded || result.Session is null)
            {
                ErrorMessage = result.RetryAfter is { } retry
                    ? $"{result.Message} Retry in {Math.Ceiling(retry.TotalSeconds)} seconds."
                    : result.Message;
                return;
            }

            if (CanUseBiometrics)
            {
                var key = result.Session.GetDatabaseKeyCopy();
                try
                {
                    await _credentialStore.SaveDatabaseKeyAsync(key);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key);
                }
            }

            MasterPassword = string.Empty;
            await _navigation.GoToAsync("//DashboardPage");
        });
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
        {
            return;
        }

        var confirm = await page.DisplayAlert(
            "Reset vault",
            "There is no way to recover a forgotten master password. Resetting permanently deletes the local encrypted vault and biometric key.",
            "Reset",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            _credentialStore.Clear();
            await _authentication.ResetVaultAsync();
            await _navigation.GoToAsync("//OnboardingPage");
        });
    }

    [RelayCommand]
    private async Task BiometricUnlockAsync()
    {
        await RunBusyAsync(async () =>
        {
            ErrorMessage = string.Empty;
            if (!await _biometrics.AuthenticateAsync("Unlock vault", "Confirm your identity to unlock the local vault."))
            {
                ErrorMessage = "Biometric authentication was cancelled or failed.";
                return;
            }

            var key = await _credentialStore.TryGetDatabaseKeyAsync();
            if (key is null)
            {
                ErrorMessage = "No biometric vault key is enrolled. Use the master password.";
                return;
            }

            try
            {
                var result = await _authentication.UnlockWithDatabaseKeyAsync(key);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.Message;
                    return;
                }

                await _navigation.GoToAsync("//DashboardPage");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        });
    }
}
