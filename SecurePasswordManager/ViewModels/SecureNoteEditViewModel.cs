using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class SecureNoteEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ISecureNoteRepository _notes;
    private readonly INavigationService _navigation;
    private readonly IToastService _toast;
    private string _noteId = string.Empty;

    [ObservableProperty]
    private string noteTitle = string.Empty;

    [ObservableProperty]
    private string markdown = string.Empty;

    [ObservableProperty]
    private string tags = string.Empty;

    [ObservableProperty]
    private bool isExistingNote;

    public SecureNoteEditViewModel(ISecureNoteRepository notes, INavigationService navigation, IToastService toast)
    {
        _notes = notes;
        _navigation = navigation;
        _toast = toast;
        Title = "Secure note";
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("NoteId", out var value) && value is string id)
        {
            _noteId = id;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            IsExistingNote = !string.IsNullOrWhiteSpace(_noteId);
            if (!IsExistingNote)
            {
                return;
            }

            var note = await _notes.GetByIdAsync(_noteId);
            if (note is not null)
            {
                NoteTitle = note.Title;
                Markdown = note.Markdown;
                Tags = note.Tags;
            }
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(NoteTitle))
            {
                StatusMessage = "Note title is required.";
                return;
            }

            var note = string.IsNullOrWhiteSpace(_noteId)
                ? new SecureNote()
                : await _notes.GetByIdAsync(_noteId) ?? new SecureNote { Id = _noteId };
            note.Title = NoteTitle.Trim();
            note.Markdown = Markdown;
            note.Tags = Tags;
            await _notes.SaveAsync(note);
            await _toast.ShowAsync("Secure note saved.");
            await _navigation.GoBackAsync();
        });
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (string.IsNullOrWhiteSpace(_noteId))
        {
            return;
        }

        await _notes.SoftDeleteAsync(_noteId);
        await _toast.ShowAsync("Secure note moved to trash.");
        await _navigation.GoBackAsync();
    }
}
