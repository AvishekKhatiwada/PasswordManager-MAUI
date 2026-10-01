using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class SecureNoteEditPage : ContentPage
{
    public SecureNoteEditPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<SecureNoteEditViewModel>();
    }
}
