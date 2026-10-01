using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SecurePasswordManager.Core.Services;
using SecurePasswordManager.Helpers;
using SecurePasswordManager.Services;
using SecurePasswordManager.ViewModels;
using SecurePasswordManager.Views;

namespace SecurePasswordManager;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
			Debug.WriteLine($"Unhandled exception: {args.ExceptionObject.GetType().Name}");
		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			Debug.WriteLine($"Unobserved task exception: {args.Exception.GetType().Name}");
			args.SetObserved();
		};

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<AppVaultFileSystem>();
		builder.Services.AddSingleton<IVaultFileSystem>(provider => provider.GetRequiredService<AppVaultFileSystem>());
		builder.Services.AddSecurePasswordManagerCore();

		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<IToastService, ToastService>();
		builder.Services.AddSingleton<IBiometricService, BiometricService>();
		builder.Services.AddSingleton<IBiometricCredentialStore, BiometricCredentialStore>();
		builder.Services.AddSingleton<ISecureClipboardService, SecureClipboardService>();
		builder.Services.AddSingleton<IFileSaverService, FileSaverService>();
		builder.Services.AddSingleton<IScreenshotProtectionService, ScreenshotProtectionService>();

		builder.Services.AddTransient<OnboardingViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<CredentialEditViewModel>();
		builder.Services.AddTransient<PasswordGeneratorViewModel>();
		builder.Services.AddTransient<SecureNotesViewModel>();
		builder.Services.AddTransient<SecureNoteEditViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<TrashViewModel>();
		builder.Services.AddTransient<CategoriesViewModel>();
		builder.Services.AddTransient<ChangeMasterPasswordViewModel>();

		builder.Services.AddTransient<OnboardingPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<CredentialEditPage>();
		builder.Services.AddTransient<PasswordGeneratorPage>();
		builder.Services.AddTransient<SecureNotesPage>();
		builder.Services.AddTransient<SecureNoteEditPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<TrashPage>();
		builder.Services.AddTransient<CategoriesPage>();
		builder.Services.AddTransient<ChangeMasterPasswordPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		ServiceHelper.Services = app.Services;
		return app;
	}
}
