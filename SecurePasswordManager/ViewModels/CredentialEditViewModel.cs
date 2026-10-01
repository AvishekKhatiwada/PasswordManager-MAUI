using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class CredentialEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ICredentialRepository _credentials;
    private readonly ICategoryRepository _categories;
    private readonly IPasswordGeneratorService _generator;
    private readonly INavigationService _navigation;
    private readonly ISecureClipboardService _clipboard;
    private readonly ISettingsService _settings;
    private readonly IScreenshotProtectionService _screenshotProtection;
    private string _credentialId = string.Empty;

    [ObservableProperty]
    private string titleText = string.Empty;

    [ObservableProperty]
    private string website = string.Empty;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string tags = string.Empty;

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private bool isFavorite;

    [ObservableProperty]
    private bool isPasswordVisible;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private string newCategoryName = string.Empty;

    [ObservableProperty]
    private VaultItemKind selectedKind = VaultItemKind.WebsiteCredential;

    public ObservableCollection<Category> Categories { get; } = [];

    public CredentialEditViewModel(
        ICredentialRepository credentials,
        ICategoryRepository categories,
        IPasswordGeneratorService generator,
        INavigationService navigation,
        ISecureClipboardService clipboard,
        ISettingsService settings,
        IScreenshotProtectionService screenshotProtection)
    {
        _credentials = credentials;
        _categories = categories;
        _generator = generator;
        _navigation = navigation;
        _clipboard = clipboard;
        _settings = settings;
        _screenshotProtection = screenshotProtection;
        Title = "Credential";
    }

    public Array EntryKinds => Enum.GetValues(typeof(VaultItemKind));

    partial void OnIsPasswordVisibleChanged(bool value) => _screenshotProtection.SetProtection(value);

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("CredentialId", out var value) && value is string id)
        {
            _credentialId = id;
        }

        if (query.TryGetValue("Kind", out var kindValue) && kindValue is VaultItemKind kind)
        {
            SelectedKind = kind;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _categories.EnsureDefaultsAsync();
            Categories.Clear();
            foreach (var category in await _categories.GetAllAsync())
            {
                Categories.Add(category);
            }

            if (!string.IsNullOrWhiteSpace(_credentialId))
            {
                var item = await _credentials.GetByIdAsync(_credentialId);
                if (item is not null)
                {
                    TitleText = item.Title;
                    Website = item.Website;
                    Username = item.Username;
                    Email = item.Email;
                    Password = item.Password;
                    Notes = item.Notes;
                    Tags = item.Tags;
                    Url = item.Url;
                    IsFavorite = item.IsFavorite;
                    SelectedKind = item.Kind;
                    SelectedCategory = Categories.FirstOrDefault(category => category.Id == item.CategoryId);
                }
            }

            SelectedCategory ??= Categories.FirstOrDefault();
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(TitleText))
            {
                StatusMessage = "Title is required.";
                return;
            }

            var item = string.IsNullOrWhiteSpace(_credentialId)
                ? new CredentialItem()
                : await _credentials.GetByIdAsync(_credentialId) ?? new CredentialItem { Id = _credentialId };

            item.Title = TitleText.Trim();
            item.Website = Website.Trim();
            item.Username = Username.Trim();
            item.Email = Email.Trim();
            item.Password = Password;
            item.Notes = Notes;
            item.Tags = Tags;
            item.Url = Url.Trim();
            item.IsFavorite = IsFavorite;
            item.Kind = SelectedKind;
            item.CategoryId = SelectedCategory?.Id ?? string.Empty;

            await _credentials.SaveAsync(item);
            _screenshotProtection.SetProtection(false);
            await _navigation.GoBackAsync();
        });
    }

    [RelayCommand]
    private async Task CreateCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            return;
        }

        var category = await _categories.GetOrCreateAsync(NewCategoryName.Trim());
        Categories.Add(category);
        SelectedCategory = category;
        NewCategoryName = string.Empty;
    }

    [RelayCommand]
    private void GeneratePassword()
    {
        Password = _generator.Generate(new PasswordGeneratorOptions { Length = 24 }).Value;
    }

    [RelayCommand]
    private async Task CopyPasswordAsync()
    {
        var settings = await _settings.GetSettingsAsync();
        await _clipboard.CopySecretAsync(Password, settings.ClipboardClearSeconds);
    }

    [RelayCommand]
    private async Task CopyUsernameAsync()
    {
        var settings = await _settings.GetSettingsAsync();
        await _clipboard.CopySecretAsync(Username, settings.ClipboardClearSeconds);
    }

    [RelayCommand]
    private async Task CopyEmailAsync()
    {
        var settings = await _settings.GetSettingsAsync();
        await _clipboard.CopySecretAsync(Email, settings.ClipboardClearSeconds);
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (!string.IsNullOrWhiteSpace(_credentialId))
        {
            await _credentials.ArchiveAsync(_credentialId, true);
            _screenshotProtection.SetProtection(false);
            await _navigation.GoBackAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!string.IsNullOrWhiteSpace(_credentialId))
        {
            await _credentials.SoftDeleteAsync(_credentialId);
            _screenshotProtection.SetProtection(false);
            await _navigation.GoBackAsync();
        }
    }
}
