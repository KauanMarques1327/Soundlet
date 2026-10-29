
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Soundlet;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Android.OS.Build.VERSION.SdkInt >=
            Android.OS.BuildVersionCodes.R)
        {
            Window?.InsetsController?.Hide(Android.Views.WindowInsets.Type.StatusBars());
        }
        else
        {
            Window?.SetFlags(
                Android.Views.WindowManagerFlags.Fullscreen,
                Android.Views.WindowManagerFlags.Fullscreen
            );
        }
    }

}
