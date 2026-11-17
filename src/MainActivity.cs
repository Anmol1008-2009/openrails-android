using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace OpenRails.Android;

[Activity(
    Label = "Open Rails",
    MainLauncher = true,
    Exported = true,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
    Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
public class MainActivity : AndroidGameActivity
{
    private Game1 _game = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _game = new Game1();
        var view = (View)_game.Services.GetService(typeof(View))!;
        SetContentView(view);
        _game.Run();
    }
}
