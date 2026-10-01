using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class CredentialEditPage : ContentPage
{
    public CredentialEditPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<CredentialEditViewModel>();
    }
}
