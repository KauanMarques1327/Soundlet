namespace Soundlet;

public partial class BluetoothPage : ContentPage
{
    private const string SearchAnimation = "BluetoothSearchRotation";

    // Pixels in the original 850 x 1850 artwork, before AspectFill scaling/cropping.
    // Measured glyph bounds: (369, 456) to (481, 660), center (425, 558).
    // Heading bounds: (59, 934) to (411, 963), vertical center y=948.5.
    private const double ArtworkWidth = 850;
    private const double ArtworkHeight = 1850;
    private static readonly Rect CentralArcBounds = new(288, 421, 274, 274);
    private static readonly Rect SearchArcBounds = new(425, 933.5, 30, 30);
    private static readonly Rect BackButtonBounds = new(38, 114, 94, 94);
    // Outer card border measured in soundlet_search.png (850 x 1850).
    private static readonly Rect DeviceCardBounds = new(57, 985, 736, 149);

    private readonly LoadingArcDrawable _centralArc = new(274, 5);
    private readonly LoadingArcDrawable _searchArc = new(30, 3);
    private bool _isPageVisible;
    private bool _isNavigatingBack;
    private Window? _animationWindow;

    public BluetoothPage()
    {
        InitializeComponent();
        CentralLoading.Drawable = _centralArc;
        SearchLoading.Drawable = _searchArc;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isPageVisible = true;
        if (_animationWindow is null && Window is { } window)
        {
            _animationWindow = window;
            window.Stopped += OnWindowStopped;
            window.Resumed += OnWindowResumed;
        }

        PositionIndicators();
        StartSearchAnimation();
    }

    protected override void OnDisappearing()
    {
        _isPageVisible = false;
        StopSearchAnimation();
        if (_animationWindow is { } window)
        {
            window.Stopped -= OnWindowStopped;
            window.Resumed -= OnWindowResumed;
            _animationWindow = null;
        }

        base.OnDisappearing();
    }

    private void OnArtworkSizeChanged(object? sender, EventArgs e) => PositionIndicators();

    private void PositionIndicators()
    {
        var width = ArtworkLayout.Width;
        var height = ArtworkLayout.Height;
        if (width <= 0 || height <= 0)
            return;

        // AspectFill uses the larger scale and crops equally on opposite sides.
        var scale = Math.Max(width / ArtworkWidth, height / ArtworkHeight);
        var offsetX = (width - ArtworkWidth * scale) / 2;
        var offsetY = (height - ArtworkHeight * scale) / 2;

        Rect MapArtworkBounds(Rect bounds) => new(
            offsetX + bounds.X * scale,
            offsetY + bounds.Y * scale,
            bounds.Width * scale,
            bounds.Height * scale);

        AbsoluteLayout.SetLayoutBounds(CentralLoading, MapArtworkBounds(CentralArcBounds));
        AbsoluteLayout.SetLayoutBounds(SearchLoading, MapArtworkBounds(SearchArcBounds));
        AbsoluteLayout.SetLayoutBounds(BackButton, MapArtworkBounds(BackButtonBounds));
        AbsoluteLayout.SetLayoutBounds(DeviceCardButton, MapArtworkBounds(DeviceCardBounds));
    }

    private void StartSearchAnimation()
    {
        if (!_isPageVisible || this.AnimationIsRunning(SearchAnimation))
            return;

        new Animation(angle =>
        {
            _centralArc.Rotation = (float)angle;
            _searchArc.Rotation = (float)angle;
            CentralLoading.Invalidate();
            SearchLoading.Invalidate();
        }, 0, 360).Commit(
            this,
            SearchAnimation,
            rate: 16,
            length: 1400,
            easing: Easing.Linear,
            repeat: () => _isPageVisible);
    }

    private void StopSearchAnimation() => this.AbortAnimation(SearchAnimation);

    private void OnWindowStopped(object? sender, EventArgs e) => StopSearchAnimation();

    private void OnWindowResumed(object? sender, EventArgs e) => StartSearchAnimation();

    private async void OnBackClicked(
        object? sender, EventArgs e)
    {
        if (_isNavigatingBack)
            return;

        _isNavigatingBack = true;
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            _isNavigatingBack = false;
        }
    }

    private async void OnPulseiraClicked(
        object? sender, EventArgs e)
    {
        if (_isNavigatingBack) return;
        _isNavigatingBack = true;
        try { await Shell.Current.GoToAsync(nameof(ConnectedPage)); }
        finally { _isNavigatingBack = false; }
    }
}
