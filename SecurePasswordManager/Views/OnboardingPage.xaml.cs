using SecurePasswordManager.Helpers;
using SecurePasswordManager.ViewModels;

namespace SecurePasswordManager.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<OnboardingViewModel>();
    }
}
