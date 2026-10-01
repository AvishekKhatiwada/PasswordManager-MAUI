using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Services;

namespace SecurePasswordManager.ViewModels;

public partial class TrashViewModel : BaseViewModel
{
    private readonly ICredentialRepository _credentials;
    private readonly ISecureNoteRepository _notes;
    private readonly IToastService _toast;

    [ObservableProperty]
    private bool showArchived;

    public ObservableCollection<CredentialItem> TrashedCredentials { get; } = [];

    public ObservableCollection<SecureNote> TrashedNotes { get; } = [];

    public ObservableCollection<CredentialItem> ArchivedCredentials { get; } = [];

    public TrashViewModel(ICredentialRepository credentials, ISecureNoteRepository notes, IToastService toast)
    {
        _credentials = credentials;
        _notes = notes;
        _toast = toast;
        Title = "Trash & Archive";
    }

    [RelayCommand]
    private void SetTrash() => ShowArchived = false;

    [RelayCommand]
    private void SetArchived() => ShowArchived = true;

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            TrashedCredentials.Clear();
            foreach (var item in await _credentials.GetTrashedAsync())
            {
                TrashedCredentials.Add(item);
            }

            TrashedNotes.Clear();
            foreach (var note in await _notes.GetTrashedAsync())
            {
                TrashedNotes.Add(note);
            }

            ArchivedCredentials.Clear();
            foreach (var item in await _credentials.GetArchivedAsync())
            {
                ArchivedCredentials.Add(item);
            }
        });
    }

    [RelayCommand]
    private async Task RestoreCredentialAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _credentials.RestoreAsync(item.Id);
        await _toast.ShowAsync("Credential restored.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RestoreNoteAsync(SecureNote? note)
    {
        if (note is null)
        {
            return;
        }

        await _notes.RestoreAsync(note.Id);
        await _toast.ShowAsync("Note restored.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task UnarchiveCredentialAsync(CredentialItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _credentials.ArchiveAsync(item.Id, archive: false);
        await _toast.ShowAsync("Credential unarchived.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteCredentialForeverAsync(CredentialItem? item)
    {
        if (item is null || !await ConfirmAsync("Delete forever", $"Permanently delete \"{item.Title}\"? This cannot be undone."))
        {
            return;
        }

        await _credentials.PermanentDeleteAsync(item.Id);
        await _toast.ShowAsync("Credential permanently deleted.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteNoteForeverAsync(SecureNote? note)
    {
        if (note is null || !await ConfirmAsync("Delete forever", $"Permanently delete \"{note.Title}\"? This cannot be undone."))
        {
            return;
        }

        await _notes.PermanentDeleteAsync(note.Id);
        await _toast.ShowAsync("Note permanently deleted.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EmptyTrashAsync()
    {
        if (TrashedCredentials.Count == 0 && TrashedNotes.Count == 0)
        {
            return;
        }

        if (!await ConfirmAsync("Empty trash", "Permanently delete everything in the trash? This cannot be undone."))
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            foreach (var item in TrashedCredentials.ToList())
            {
                await _credentials.PermanentDeleteAsync(item.Id);
            }

            foreach (var note in TrashedNotes.ToList())
            {
                await _notes.PermanentDeleteAsync(note.Id);
            }
        });

        await _toast.ShowAsync("Trash emptied.");
        await LoadAsync();
    }

    private static Task<bool> ConfirmAsync(string title, string message)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page is null ? Task.FromResult(false) : page.DisplayAlert(title, message, "Delete", "Cancel");
    }
}
