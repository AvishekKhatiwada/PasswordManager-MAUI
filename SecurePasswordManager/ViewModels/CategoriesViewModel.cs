using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class CategoriesViewModel : BaseViewModel
{
    private readonly ICategoryRepository _categories;
    private readonly IToastService _toast;
    private string _editingId = string.Empty;

    [ObservableProperty]
    private bool isFormVisible;

    [ObservableProperty]
    private string formTitle = "New category";

    [ObservableProperty]
    private string formName = string.Empty;

    [ObservableProperty]
    private string formIconGlyph = IconPalette[0];

    [ObservableProperty]
    private string formColorHex = ColorPalette[0];

    public ObservableCollection<Category> Categories { get; } = [];

    public static IReadOnlyList<string> IconPalette { get; } =
        ["★", "G", "", "$", "▣", "●", "▶", "◆", "₿", "▦", "+"];

    public static IReadOnlyList<string> ColorPalette { get; } =
        ["#2563EB", "#DB4437", "#24292F", "#047857", "#7C3AED", "#0F766E", "#E11D48", "#EA580C", "#B45309", "#334155", "#64748B"];

    public CategoriesViewModel(ICategoryRepository categories, IToastService toast)
    {
        _categories = categories;
        _toast = toast;
        Title = "Categories";
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
        });
    }

    [RelayCommand]
    private void StartCreate()
    {
        _editingId = string.Empty;
        FormTitle = "New category";
        FormName = string.Empty;
        FormIconGlyph = IconPalette[0];
        FormColorHex = ColorPalette[0];
        StatusMessage = string.Empty;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void StartEdit(Category? category)
    {
        if (category is null)
        {
            return;
        }

        _editingId = category.Id;
        FormTitle = "Edit category";
        FormName = category.Name;
        FormIconGlyph = category.IconGlyph;
        FormColorHex = category.ColorHex;
        StatusMessage = string.Empty;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void SelectIcon(string glyph) => FormIconGlyph = glyph;

    [RelayCommand]
    private void SelectColor(string hex) => FormColorHex = hex;

    [RelayCommand]
    private void CancelForm() => IsFormVisible = false;

    [RelayCommand]
    private async Task SaveFormAsync()
    {
        var name = FormName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Category name is required.";
            return;
        }

        if (Categories.Any(category => category.Id != _editingId && string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "A category with that name already exists.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var category = string.IsNullOrWhiteSpace(_editingId)
                ? new Category()
                : await _categories.GetByIdAsync(_editingId) ?? new Category { Id = _editingId };

            category.Name = name;
            category.IconGlyph = FormIconGlyph;
            category.ColorHex = FormColorHex;

            await _categories.SaveAsync(category);
            IsFormVisible = false;
        });

        await _toast.ShowAsync("Category saved.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Category? category)
    {
        if (category is null || category.IsDefault)
        {
            return;
        }

        if (!await ConfirmAsync("Delete category", $"Delete \"{category.Name}\"? Credentials keep their assignment but will show no category."))
        {
            return;
        }

        await _categories.DeleteAsync(category.Id);
        await _toast.ShowAsync("Category deleted.");
        await LoadAsync();
    }

    private static Task<bool> ConfirmAsync(string title, string message)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page is null ? Task.FromResult(false) : page.DisplayAlert(title, message, "Delete", "Cancel");
    }
}
