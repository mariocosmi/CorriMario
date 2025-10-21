namespace CorriMario;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new MainPage();
	}

	protected override void OnStart()
	{
		// Code to execute when the application is launching (eg, from Start)
		// This code will not execute when the application is reactivated
	}

	protected override void OnSleep()
	{
		// Code to execute when the application is deactivated (sent to background)
		// This code will not execute when the application is closing
	}

	protected override void OnResume()
	{
		// Code to execute when the application is activated (brought to foreground)
		// This code will not execute when the application is first launched
	}
}
