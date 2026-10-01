using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class ChangeMasterPasswordPage : ContentPage
{
    public ChangeMasterPasswordPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<ChangeMasterPasswordViewModel>();
    }
}
