namespace Soundlet;

public partial class MainPage : ContentPage
{
    private readonly ConnectionOptionsDrawable _options = new();
    private bool _isNavigating;

    public MainPage()
    {
        InitializeComponent();
        ConnectionArtwork.Drawable = _options;
        UpdateSelection();
    }

    private void OnArtworkSizeChanged(object? sender, EventArgs e)
    {
        var width = ArtworkLayout.Width;
        var height = ArtworkLayout.Height;
        if (width <= 0 || height <= 0) return;
        var scale = Math.Max(width / 850, height / 1850);
        var x = (width - 850 * scale) / 2;
        var y = (height - 1850 * scale) / 2;
        Rect Map(Rect bounds) => new(x + bounds.X * scale, y + bounds.Y * scale,
            bounds.Width * scale, bounds.Height * scale);
        AbsoluteLayout.SetLayoutBounds(BluetoothButton, Map(ConnectionOptionsDrawable.BluetoothBounds));
        AbsoluteLayout.SetLayoutBounds(WifiButton, Map(ConnectionOptionsDrawable.WifiBounds));
        ConnectionArtwork.Invalidate();
    }

    private void UpdateSelection()
    {
        SemanticProperties.SetDescription(BluetoothButton,
            "Conectar via Bluetooth, " + (_options.WifiSelected ? "não selecionado" : "selecionado"));
        SemanticProperties.SetDescription(WifiButton,
            "Conectar via Wi-Fi, " + (_options.WifiSelected ? "selecionado" : "não selecionado"));
        ConnectionArtwork.Invalidate();
    }

    private void OnBluetoothClicked(object? sender, EventArgs e) => SelectOrNavigate(false);
    private void OnWifiClicked(object? sender, EventArgs e) => SelectOrNavigate(true);

    private async void SelectOrNavigate(bool wifi)
    {
        if (_isNavigating) return;
        if (_options.WifiSelected != wifi)
        {
            _options.WifiSelected = wifi;
            UpdateSelection();
            return;
        }
        _isNavigating = true;
        try { await Shell.Current.GoToAsync(wifi ? nameof(WifiPage) : nameof(BluetoothPage)); }
        finally { _isNavigating = false; }
    }
}
