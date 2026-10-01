using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IAuthenticationService _authentication;
    private readonly IBackupService _backup;
    private readonly IBiometricService _biometrics;
    private readonly IBiometricCredentialStore _credentialStore;
    private readonly INavigationService _navigation;
    private readonly IToastService _toast;
    private readonly IFileSaverService _fileSaver;
    private readonly AppVaultFileSystem _fileSystem;

    [ObservableProperty]
    private string theme = "System";

    [ObservableProperty]
    private int autoLockMinutes = 5;

    [ObservableProperty]
    private int clipboardClearSeconds = 30;

    [ObservableProperty]
    private bool biometricsEnabled;

    [ObservableProperty]
    private bool highContrast;

    [ObservableProperty]
    private string backupPassword = string.Empty;

    public IReadOnlyList<string> Themes { get; } = ["System", "Light", "Dark"];

    public SettingsViewModel(
        ISettingsService settingsService,
        IAuthenticationService authentication,
        IBackupService backup,
        IBiometricService biometrics,
        IBiometricCredentialStore credentialStore,
        INavigationService navigation,
        IToastService toast,
        IFileSaverService fileSaver,
        AppVaultFileSystem fileSystem)
    {
        _settingsService = settingsService;
        _authentication = authentication;
        _backup = backup;
        _biometrics = biometrics;
        _credentialStore = credentialStore;
        _navigation = navigation;
        _toast = toast;
        _fileSaver = fileSaver;
        _fileSystem = fileSystem;
        Title = "Settings";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        Theme = settings.Theme;
        AutoLockMinutes = settings.AutoLockMinutes;
        ClipboardClearSeconds = settings.ClipboardClearSeconds;
        BiometricsEnabled = settings.BiometricsEnabled;
        HighContrast = settings.HighContrast;
        ApplyTheme();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunBusyAsync(async () =>
        {
            var settings = new VaultSettings
            {
                Theme = Theme,
                AutoLockMinutes = AutoLockMinutes,
                ClipboardClearSeconds = ClipboardClearSeconds,
                BiometricsEnabled = BiometricsEnabled,
                HighContrast = HighContrast
            };

            if (BiometricsEnabled)
            {
                if (!await _biometrics.IsAvailableAsync())
                {
                    await _toast.ShowAsync("Biometrics are not available.");
                    BiometricsEnabled = false;
                    settings.BiometricsEnabled = false;
                }
                else if (_authentication.CurrentSession is not null)
                {
                    var key = _authentication.CurrentSession.GetDatabaseKeyCopy();
                    try
                    {
                        await _credentialStore.SaveDatabaseKeyAsync(key);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(key);
                    }
                }
            }
            else
            {
                _credentialStore.Clear();
            }

            await _settingsService.SaveSettingsAsync(settings);
            ApplyTheme();
            await _toast.ShowAsync("Settings saved.");
        });
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(BackupPassword))
            {
                await _toast.ShowAsync("Enter a backup password first.");
                return;
            }

            var fileName = $"vault-{DateTime.UtcNow:yyyyMMdd-HHmmss}.spmbackup";
            var path = Path.Combine(_fileSystem.BackupDirectory, fileName);
            await _backup.ExportEncryptedAsync(path, BackupPassword);

            if (await _fileSaver.SaveToDownloadsAsync(path, fileName))
            {
                await _toast.ShowAsync("Encrypted backup downloaded.");
                return;
            }

            await Share.Default.RequestAsync(new ShareFileRequest("Encrypted vault backup", new ShareFile(path)));
            await _toast.ShowAsync("Encrypted backup exported.");
        });
    }

    [RelayCommand]
    private async Task ImportBackupAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(BackupPassword))
            {
                await _toast.ShowAsync("Enter the backup password first.");
                return;
            }

            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose encrypted backup"
            });

            if (file is null)
            {
                return;
            }

            await _backup.ImportEncryptedAsync(file.FullPath, BackupPassword, merge: true);
            await _toast.ShowAsync("Encrypted backup imported.");
        });
    }

    [RelayCommand]
    private Task OpenTrashAsync() => _navigation.GoToAsync(nameof(Views.TrashPage));

    [RelayCommand]
    private Task OpenCategoriesAsync() => _navigation.GoToAsync(nameof(Views.CategoriesPage));

    [RelayCommand]
    private Task OpenChangeMasterPasswordAsync() => _navigation.GoToAsync(nameof(Views.ChangeMasterPasswordPage));

    [RelayCommand]
    private async Task LockAsync()
    {
        await _authentication.LockAsync();
        await _navigation.GoToAsync("//LoginPage");
    }

    [RelayCommand]
    private async Task ResetVaultAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
        {
            return;
        }

        var confirm = await page.DisplayAlert(
            "Reset vault",
            "This permanently deletes the local encrypted vault and biometric key.",
            "Reset",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        _credentialStore.Clear();
        await _authentication.ResetVaultAsync();
        await _navigation.GoToAsync("//OnboardingPage");
    }

    private void ApplyTheme()
    {
        Application.Current!.UserAppTheme = Theme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
