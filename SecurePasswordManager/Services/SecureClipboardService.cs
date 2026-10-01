namespace SecurePasswordManager.Services;

public sealed class SecureClipboardService : ISecureClipboardService
{
    private readonly IToastService _toastService;
    private CancellationTokenSource? _clearTokenSource;

    public SecureClipboardService(IToastService toastService)
    {
        _toastService = toastService;
    }

    public async Task CopySecretAsync(string secret, int clearAfterSeconds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secret))
        {
            await _toastService.ShowAsync("Nothing to copy.", cancellationToken);
            return;
        }

        _clearTokenSource?.Cancel();
        _clearTokenSource?.Dispose();
        _clearTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await Clipboard.Default.SetTextAsync(secret);
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
        }
        await _toastService.ShowAsync($"Copied. Clipboard clears in {clearAfterSeconds}s.", cancellationToken);

        _ = ClearLaterAsync(secret, Math.Clamp(clearAfterSeconds, 5, 300), _clearTokenSource.Token);
    }

    private static async Task ClearLaterAsync(string expectedSecret, int seconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
            var current = await Clipboard.Default.GetTextAsync();
            if (current == expectedSecret)
            {
                await Clipboard.Default.SetTextAsync(string.Empty);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
