using Soundlet.Services;

namespace Soundlet;

public partial class SplashPage : ContentPage
{
    private const string PresentationAnimation = "SoundletPresentation";
    private readonly SplashProgressDrawable _bar = new();
    private bool _started;

    public SplashPage()
    {
        InitializeComponent();
        PresentationBar.Drawable = _bar;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        // Begin after attaching the page, leaving the UI free to render the empty track.
        Dispatcher.Dispatch(PresentAsync);
    }

    private async void PresentAsync()
    {
        var window = Window;
        if (window is null) { _started = false; return; }

        _bar.Progress = 0;
        PresentationBar.Invalidate();
        // This is a visual introduction, not a percentage of storage/network work.
        var presentation = AnimateBarAsync(0, 0.95, 1350);
        var startup = PrepareApplicationAsync();
        if (!await presentation) return;
        var shell = await startup;
        if (window.Page != this) return;
        // The final part only runs after initialization has actually finished.
        if (!await AnimateBarAsync(0.95, 1, 150)) return;
        await Composition.FadeToAsync(0, 180, Easing.CubicIn);
        // Replace the window root: the splash never enters the Shell back stack.
        window.Page = shell;
    }

    private Task<bool> AnimateBarAsync(double from, double to, uint duration)
    {
        var completion = new TaskCompletionSource<bool>();
        new Animation(value =>
        {
            _bar.Progress = (float)value;
            PresentationBar.Invalidate();
        }, from, to).Commit(this, PresentationAnimation, 16, duration, Easing.SinInOut,
            (_, cancelled) => completion.TrySetResult(!cancelled));
        return completion.Task;
    }

    private static async Task<AppShell> PrepareApplicationAsync()
    {
        // Existing initialization only. Failed storage reads retain the existing
        // retry/error handling on ConnectedPage instead of stranding users here.
        try { await AlertHistory.Current.LoadAsync(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine($"Soundlet history initialization: {ex.Message}");
        }
        return new AppShell();
    }

    protected override bool OnBackButtonPressed() => true;

    protected override void OnDisappearing()
    {
        this.AbortAnimation(PresentationAnimation);
        base.OnDisappearing();
    }
}
