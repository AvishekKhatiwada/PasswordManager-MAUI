using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class TrashPage : ContentPage
{
    public TrashPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<TrashViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TrashViewModel viewModel && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
