namespace SecurePasswordManager;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.CredentialEditPage), typeof(Views.CredentialEditPage));
		Routing.RegisterRoute(nameof(Views.SecureNoteEditPage), typeof(Views.SecureNoteEditPage));
		Routing.RegisterRoute(nameof(Views.TrashPage), typeof(Views.TrashPage));
		Routing.RegisterRoute(nameof(Views.CategoriesPage), typeof(Views.CategoriesPage));
		Routing.RegisterRoute(nameof(Views.ChangeMasterPasswordPage), typeof(Views.ChangeMasterPasswordPage));
	}
}
