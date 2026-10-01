using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class PasswordGeneratorViewModel : BaseViewModel
{
    private readonly IPasswordGeneratorService _generator;
    private readonly ISecureClipboardService _clipboard;
    private readonly ISettingsService _settings;

    [ObservableProperty]
    private int length = 24;

    [ObservableProperty]
    private bool useUppercase = true;

    [ObservableProperty]
    private bool useLowercase = true;

    [ObservableProperty]
    private bool useNumbers = true;

    [ObservableProperty]
    private bool useSymbols = true;

    [ObservableProperty]
    private bool excludeAmbiguous = true;

    [ObservableProperty]
    private bool passphraseMode;

    [ObservableProperty]
    private int passphraseWords = 5;

    [ObservableProperty]
    private string generatedPassword = string.Empty;

    [ObservableProperty]
    private string entropyText = "0 bits";

    [ObservableProperty]
    private double strengthScore;

    [ObservableProperty]
    private string strengthLabel = "Weak";

    public PasswordGeneratorViewModel(
        IPasswordGeneratorService generator,
        ISecureClipboardService clipboard,
        ISettingsService settings)
    {
        _generator = generator;
        _clipboard = clipboard;
        _settings = settings;
        Title = "Generator";
    }

    partial void OnLengthChanged(int value) => Generate();
    partial void OnUseUppercaseChanged(bool value) => Generate();
    partial void OnUseLowercaseChanged(bool value) => Generate();
    partial void OnUseNumbersChanged(bool value) => Generate();
    partial void OnUseSymbolsChanged(bool value) => Generate();
    partial void OnExcludeAmbiguousChanged(bool value) => Generate();
    partial void OnPassphraseModeChanged(bool value) => Generate();
    partial void OnPassphraseWordsChanged(int value) => Generate();

    [RelayCommand]
    private void Load() => Generate();

    [RelayCommand]
    private void Generate()
    {
        var result = _generator.Generate(new PasswordGeneratorOptions
        {
            Length = Length,
            UseUppercase = UseUppercase,
            UseLowercase = UseLowercase,
            UseNumbers = UseNumbers,
            UseSymbols = UseSymbols,
            ExcludeAmbiguous = ExcludeAmbiguous,
            PassphraseMode = PassphraseMode,
            PassphraseWords = PassphraseWords
        });

        GeneratedPassword = result.Value;
        EntropyText = $"{result.EntropyBits:N1} bits";
        StrengthScore = result.StrengthScore;
        StrengthLabel = result.StrengthLabel;
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        var settings = await _settings.GetSettingsAsync();
        await _clipboard.CopySecretAsync(GeneratedPassword, settings.ClipboardClearSeconds);
    }
}
