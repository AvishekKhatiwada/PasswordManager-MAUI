using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class SecureNotesViewModel : BaseViewModel
{
    private const string AllCategory = "All";

    private readonly ISecureNoteRepository _notes;
    private readonly INavigationService _navigation;
    private readonly IToastService _toast;
    private IReadOnlyList<SecureNote> _allNotes = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int noteCount;

    [ObservableProperty]
    private string selectedCategory = AllCategory;

    public ObservableCollection<SecureNote> Notes { get; } = [];

    public ObservableCollection<string> Categories { get; } = [AllCategory];

    public SecureNotesViewModel(ISecureNoteRepository notes, INavigationService navigation, IToastService toast)
    {
        _notes = notes;
        _navigation = navigation;
        _toast = toast;
        Title = "Secure notes";
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        _allNotes = await _notes.SearchAsync(SearchText);
        NoteCount = _allNotes.Count;

        var current = SelectedCategory;
        Categories.Clear();
        Categories.Add(AllCategory);
        foreach (var tag in _allNotes
                     .Select(note => PrimaryTag(note.Tags))
                     .Where(tag => !string.IsNullOrWhiteSpace(tag))
                     .Distinct()
                     .OrderBy(tag => tag))
        {
            Categories.Add(tag);
        }

        SelectedCategory = Categories.Contains(current) ? current : AllCategory;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = SelectedCategory == AllCategory
            ? _allNotes
            : _allNotes.Where(note => PrimaryTag(note.Tags) == SelectedCategory);

        Notes.Clear();
        foreach (var note in filtered)
        {
            Notes.Add(note);
        }
    }

    private static string PrimaryTag(string tags) =>
        tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;

    [RelayCommand]
    private Task AddAsync() => _navigation.GoToAsync(nameof(Views.SecureNoteEditPage));

    [RelayCommand]
    private Task EditAsync(SecureNote note) => _navigation.GoToAsync(nameof(Views.SecureNoteEditPage), new Dictionary<string, object>
    {
        ["NoteId"] = note.Id
    });

    [RelayCommand]
    private async Task DeleteAsync(SecureNote note)
    {
        await _notes.SoftDeleteAsync(note.Id);
        await _toast.ShowAsync("Secure note moved to trash.");
        await LoadAsync();
    }
}
