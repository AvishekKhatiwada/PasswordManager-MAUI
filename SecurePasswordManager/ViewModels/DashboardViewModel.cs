using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly ICredentialRepository _credentials;
    private readonly ICategoryRepository _categories;
    private readonly ISettingsService _settings;
    private readonly ISecureClipboardService _clipboard;
    private readonly INavigationService _navigation;
    private readonly IAuthenticationService _authentication;
    private readonly IToastService _toast;
    private CancellationTokenSource? _searchDebounce;
    private int _clipboardSeconds = 30;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private bool isGrid;

    [ObservableProperty]
    private string layoutToggleText = "Grid";

    [ObservableProperty]
    private bool isRefreshing;

    public ObservableCollection<CredentialItem> Credentials { get; } = [];

    public ObservableCollection<Category> Categories { get; } = [];

    public IReadOnlyList<QuickAddShortcut> QuickAddShortcuts { get; } =
    [
        new("Login", "\U0001F464", VaultItemKind.WebsiteCredential),
        new("Secure Note", "\U0001F4DD", VaultItemKind.SecureNote),
        new("Email", "\U0001F4E7", VaultItemKind.EmailCredential),
        new("API Key", "\U0001F511", VaultItemKind.ApiKey),
        new("Custom", "\U0001F9E9", VaultItemKind.CustomLogin)
    ];

    public DashboardViewModel(
        ICredentialRepository credentials,
        ICategoryRepository categories,
        ISettingsService settings,
        ISecureClipboardService clipboard,
        INavigationService navigation,
        IAuthenticationService authentication,
        IToastService toast)
    {
        _credentials = credentials;
        _categories = categories;
        _settings = settings;
        _clipboard = clipboard;
        _navigation = navigation;
        _authentication = authentication;
        _toast = toast;
        Title = "Vault";
    }

    partial void OnSearchTextChanged(string value) => DebounceSearch();

    partial void OnSelectedCategoryChanged(Category? value) => DebounceSearch();

    partial void OnIsGridChanged(bool value) => LayoutToggleText = value ? "List" : "Grid";

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            _authentication.Touch();
            var settings = await _settings.GetSettingsAsync();
            _clipboardSeconds = settings.ClipboardClearSeconds;
            await _categories.EnsureDefaultsAsync();

            Categories.Clear();
            Categories.Add(new Category { Id = string.Empty, Name = "All", IconGlyph = "\u25CF", ColorHex = "#64748B" });
            foreach (var category in await _categories.GetAllAsync())
            {
                Categories.Add(category);
            }

            SelectedCategory ??= Categories.FirstOrDefault();
            await SearchAsync();
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await SearchAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task AddAsync() => _navigation.GoToAsync(nameof(Views.CredentialEditPage));

    [RelayCommand]
    private Task QuickAddAsync(QuickAddShortcut? shortcut)
    {
        if (shortcut is null)
        {
            return Task.CompletedTask;
        }

        return _navigation.GoToAsync(nameof(Views.CredentialEditPage), new Dictionary<string, object>
        {
            ["Kind"] = shortcut.Kind
        });
    }

    [RelayCommand]
    private Task EditAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return Task.CompletedTask;
        }

        return _navigation.GoToAsync(nameof(Views.CredentialEditPage), new Dictionary<string, object>
        {
            ["CredentialId"] = item.Id
        });
    }

    [RelayCommand]
    private async Task CopyPasswordAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _clipboard.CopySecretAsync(item.Password, _clipboardSeconds);
    }

    [RelayCommand]
    private async Task CopyUsernameAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _clipboard.CopySecretAsync(item.Username, _clipboardSeconds);
    }

    [RelayCommand]
    private async Task CopyEmailAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _clipboard.CopySecretAsync(item.Email, _clipboardSeconds);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _credentials.SetFavoriteAsync(item.Id, !item.IsFavorite);
        await SearchAsync();
    }

    [RelayCommand]
    private async Task DuplicateAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _credentials.DuplicateAsync(item.Id);
        await _toast.ShowAsync("Credential duplicated.");
        await SearchAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _credentials.SoftDeleteAsync(item.Id);
        await _toast.ShowAsync("Credential moved to trash.");
        await SearchAsync();
    }

    [RelayCommand]
    private Task OpenSettingsAsync() => _navigation.GoToAsync("//SettingsPage");

    [RelayCommand]
    private void ToggleLayout() => IsGrid = !IsGrid;

    [RelayCommand]
    private void SetGrid() => IsGrid = true;

    [RelayCommand]
    private void SetList() => IsGrid = false;

    private async Task SearchAsync()
    {
        _authentication.Touch();
        var items = await _credentials.SearchAsync(new CredentialSearchQuery
        {
            SearchText = SearchText,
            CategoryId = SelectedCategory?.Id ?? string.Empty,
            Limit = 150
        });

        Credentials.Clear();
        foreach (var item in items)
        {
            Credentials.Add(item);
        }
    }

    private void DebounceSearch()
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                await MainThread.InvokeOnMainThreadAsync(SearchAsync);
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }
}
