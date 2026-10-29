namespace Soundlet;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}



	private async void OnBluetoothClicked(
		object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(
			nameof(BluetoothPage)
		);
	}

}
