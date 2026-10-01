using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace SecurePasswordManager.Services;

public sealed class ToastService : IToastService
{
    public async Task ShowAsync(string message, CancellationToken cancellationToken = default)
    {
        var toast = Toast.Make(message, ToastDuration.Short, 14);
        await toast.Show(cancellationToken);
    }
}
