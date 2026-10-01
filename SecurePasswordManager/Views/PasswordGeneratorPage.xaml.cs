using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class PasswordGeneratorPage : ContentPage
{
    public PasswordGeneratorPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<PasswordGeneratorViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PasswordGeneratorViewModel viewModel && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
