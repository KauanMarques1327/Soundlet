namespace Soundlet;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(ConnectedPage), typeof(ConnectedPage));

		Routing.RegisterRoute(
			nameof(BluetoothPage),
			typeof(BluetoothPage)
		);
	}
}
