using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Security;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class ChangeMasterPasswordViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authentication;
    private readonly IEncryptionService _encryption;
    private readonly IBiometricCredentialStore _credentialStore;
    private readonly ISettingsService _settings;
    private readonly INavigationService _navigation;
    private readonly IToastService _toast;

    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private double strengthScore;

    [ObservableProperty]
    private string strengthLabel = "Weak";

    [ObservableProperty]
    private string validationText = "Use at least 12 characters with upper, lower, number, and symbol.";

    [ObservableProperty]
    private bool hasValidationError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ChangeMasterPasswordViewModel(
        IAuthenticationService authentication,
        IEncryptionService encryption,
        IBiometricCredentialStore credentialStore,
        ISettingsService settings,
        INavigationService navigation,
        IToastService toast)
    {
        _authentication = authentication;
        _encryption = encryption;
        _credentialStore = credentialStore;
        _settings = settings;
        _navigation = navigation;
        _toast = toast;
        Title = "Change master password";
    }

    partial void OnNewPasswordChanged(string value) => UpdateValidation();

    partial void OnConfirmPasswordChanged(string value) => UpdateValidation();

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        await RunBusyAsync(async () =>
        {
            ErrorMessage = string.Empty;
            UpdateValidation();
            if (HasValidationError || string.IsNullOrEmpty(CurrentPassword))
            {
                return;
            }

            var result = await _authentication.ChangeMasterPasswordAsync(CurrentPassword, NewPassword);
            if (!result.Succeeded || result.Session is null)
            {
                ErrorMessage = result.Message;
                return;
            }

            var settings = await _settings.GetSettingsAsync();
            if (settings.BiometricsEnabled)
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

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            await _toast.ShowAsync("Master password changed.");
            await _navigation.GoBackAsync();
        });
    }

    private void UpdateValidation()
    {
        var validation = _encryption.ValidateMasterPassword(NewPassword);
        StrengthScore = validation.Score;
        StrengthLabel = validation.Label;

        if (!string.IsNullOrEmpty(ConfirmPassword) && NewPassword != ConfirmPassword)
        {
            ValidationText = "Passwords do not match.";
            HasValidationError = true;
            return;
        }

        if (validation.Errors.Count > 0)
        {
            ValidationText = string.Join(Environment.NewLine, validation.Errors);
            HasValidationError = true;
            return;
        }

        ValidationText = "New master password meets the vault policy.";
        HasValidationError = false;
    }
}
