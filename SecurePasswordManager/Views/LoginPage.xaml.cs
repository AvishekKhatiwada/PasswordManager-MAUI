using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<LoginViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LoginViewModel viewModel && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    private void OnPasswordCompleted(object sender, EventArgs e)
    {
        if (BindingContext is LoginViewModel viewModel && viewModel.UnlockCommand.CanExecute(null))
        {
            viewModel.UnlockCommand.Execute(null);
        }
    }
}
