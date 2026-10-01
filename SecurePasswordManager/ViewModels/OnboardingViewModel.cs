using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Security.Cryptography;
using SecurePasswordManager.Core.Security;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class OnboardingViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authentication;
    private readonly IEncryptionService _encryption;
    private readonly INavigationService _navigation;
    private readonly IBiometricService _biometrics;
    private readonly IBiometricCredentialStore _credentialStore;
    private readonly ISettingsService _settings;
    private readonly IToastService _toast;

    [ObservableProperty]
    private string masterPassword = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool enableBiometrics;

    [ObservableProperty]
    private double strengthScore;

    [ObservableProperty]
    private string strengthLabel = "Weak";

    [ObservableProperty]
    private string validationText = "Use at least 12 characters with upper, lower, number, and symbol.";

    [ObservableProperty]
    private bool hasValidationError;

    public OnboardingViewModel(
        IAuthenticationService authentication,
        IEncryptionService encryption,
        INavigationService navigation,
        IBiometricService biometrics,
        IBiometricCredentialStore credentialStore,
        ISettingsService settings,
        IToastService toast)
    {
        _authentication = authentication;
        _encryption = encryption;
        _navigation = navigation;
        _biometrics = biometrics;
        _credentialStore = credentialStore;
        _settings = settings;
        _toast = toast;
        Title = "Create vault";
    }

    partial void OnMasterPasswordChanged(string value) => UpdateValidation();

    partial void OnConfirmPasswordChanged(string value) => UpdateValidation();

    [RelayCommand]
    private async Task CreateVaultAsync()
    {
        await RunBusyAsync(async () =>
        {
            UpdateValidation();
            if (HasValidationError)
            {
                return;
            }

            var session = await _authentication.CreateVaultAsync(MasterPassword);

            if (EnableBiometrics)
            {
                if (await _biometrics.IsAvailableAsync())
                {
                    var key = session.GetDatabaseKeyCopy();
                    try
                    {
                        await _credentialStore.SaveDatabaseKeyAsync(key);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(key);
                    }

                    var settings = await _settings.GetSettingsAsync();
                    settings.BiometricsEnabled = true;
                    await _settings.SaveSettingsAsync(settings);
                }
                else
                {
                    await _toast.ShowAsync("Biometrics are not available on this device.");
                }
            }

            MasterPassword = string.Empty;
            ConfirmPassword = string.Empty;
            await _navigation.GoToAsync("//DashboardPage");
        });
    }

    private void UpdateValidation()
    {
        var validation = _encryption.ValidateMasterPassword(MasterPassword);
        StrengthScore = validation.Score;
        StrengthLabel = validation.Label;

        if (!string.IsNullOrEmpty(ConfirmPassword) && MasterPassword != ConfirmPassword)
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

        ValidationText = "Master password meets the vault policy.";
        HasValidationError = false;
    }
}
