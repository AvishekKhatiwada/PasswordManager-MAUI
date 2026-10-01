using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager;

public partial class App : Application
{
	private readonly IAuthenticationService _authentication;
	private readonly ISettingsService _settings;
	private IDispatcherTimer? _lockTimer;

	public App(IAuthenticationService authentication, ISettingsService settings)
	{
		_authentication = authentication;
		_settings = settings;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = new AppShell();
		var window = new Window(shell);

		window.Stopped += async (_, _) => await LockAndNavigateAsync();
		window.Resumed += async (_, _) =>
		{
			if (await _authentication.IsVaultCreatedAsync() && !_authentication.IsUnlocked)
			{
				await shell.GoToAsync("//LoginPage");
			}
		};

		_lockTimer = Dispatcher.CreateTimer();
		_lockTimer.Interval = TimeSpan.FromSeconds(20);
		_lockTimer.Tick += async (_, _) => await AutoLockIfNeededAsync();
		_lockTimer.Start();

		Dispatcher.Dispatch(async () =>
		{
			var route = await _authentication.IsVaultCreatedAsync() ? "//LoginPage" : "//OnboardingPage";
			await shell.GoToAsync(route);
		});

		return window;
	}

	private async Task AutoLockIfNeededAsync()
	{
		if (!_authentication.IsUnlocked)
		{
			return;
		}

		var settings = await _settings.GetSettingsAsync();
		if (_authentication.ShouldAutoLock(TimeSpan.FromMinutes(settings.AutoLockMinutes)))
		{
			await LockAndNavigateAsync();
		}
	}

	private async Task LockAndNavigateAsync()
	{
		if (!_authentication.IsUnlocked)
		{
			return;
		}

		await _authentication.LockAsync();
		if (Shell.Current is not null)
		{
			await Shell.Current.GoToAsync("//LoginPage");
		}
	}
}
