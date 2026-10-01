#if ANDROID
using Android.Views;
#elif IOS || MACCATALYST
using Foundation;
using UIKit;
#endif

namespace SecurePasswordManager.Services;

public sealed class ScreenshotProtectionService : IScreenshotProtectionService
{
#if IOS || MACCATALYST
    private UIView? _privacyOverlay;
    private NSObject? _willResignObserver;
    private NSObject? _didBecomeActiveObserver;
#endif

    public void SetProtection(bool enabled)
    {
#if ANDROID
        var window = Platform.CurrentActivity?.Window;
        if (window is null)
        {
            return;
        }

        if (enabled)
        {
            window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
        }
        else
        {
            window.ClearFlags(WindowManagerFlags.Secure);
        }
#elif IOS || MACCATALYST
        var window = UIApplication.SharedApplication
            .ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(scene => scene.Windows)
            .FirstOrDefault(item => item.IsKeyWindow);

        if (window is null)
        {
            return;
        }

        if (enabled)
        {
            _privacyOverlay ??= new UIView(window.Bounds)
            {
                BackgroundColor = UIColor.SystemBackground,
                Hidden = true,
                AutoresizingMask = UIViewAutoresizing.FlexibleDimensions
            };

            if (_privacyOverlay.Superview is null)
            {
                window.AddSubview(_privacyOverlay);
            }

            _willResignObserver ??= UIApplication.Notifications.ObserveWillResignActive((_, _) =>
            {
                if (_privacyOverlay is not null)
                {
                    _privacyOverlay.Hidden = false;
                }
            });

            _didBecomeActiveObserver ??= UIApplication.Notifications.ObserveDidBecomeActive((_, _) =>
            {
                if (_privacyOverlay is not null)
                {
                    _privacyOverlay.Hidden = true;
                }
            });

            return;
        }

        _privacyOverlay?.RemoveFromSuperview();
        _privacyOverlay = null;
        _willResignObserver?.Dispose();
        _didBecomeActiveObserver?.Dispose();
        _willResignObserver = null;
        _didBecomeActiveObserver = null;
#endif
    }
}
