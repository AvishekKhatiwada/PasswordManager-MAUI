using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SecurePasswordManager.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    protected async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            // [RelayCommand]-generated commands run fire-and-forget from the UI thread;
            // an exception left uncaught here becomes an unhandled async-void exception
            // that terminates the whole app instead of just failing the current action.
            Debug.WriteLine($"Unhandled command error: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
