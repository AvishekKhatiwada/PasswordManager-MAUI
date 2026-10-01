using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class SecureNotesPage : ContentPage
{
    public SecureNotesPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<SecureNotesViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SecureNotesViewModel viewModel && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    private async void OnNoteMenuClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: SecureNote note } || BindingContext is not SecureNotesViewModel viewModel)
        {
            return;
        }

        var choice = await DisplayActionSheet(note.Title, "Cancel", "Delete", "Edit");
        switch (choice)
        {
            case "Edit":
                await viewModel.EditCommand.ExecuteAsync(note);
                break;
            case "Delete":
                await viewModel.DeleteCommand.ExecuteAsync(note);
                break;
        }
    }
}
