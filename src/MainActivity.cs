using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;
using OpenRails.Core.Simulation;

namespace OpenRails.Android;

[Activity(
    Label = "Open Rails",
    MainLauncher = true,
    Exported = true,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
        | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Density | ConfigChanges.ScreenLayout)]
public class MainActivity : AndroidGameActivity
{
    private const string Tag = "OpenRailsAndroid";
    private const int ImportFolderRequest = 2001;
    private string _logPath = string.Empty;
    private Game1? _game;
    private FrameLayout? _host;
    private TrainControlState? _controls;
    private View? _contentOverlay;
    private View? _controlOverlay;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var dir = GetExternalFilesDir(null)?.AbsolutePath ?? FilesDir!.AbsolutePath;
        _logPath = Path.Combine(dir, "startup.log");
        try { File.WriteAllText(_logPath, string.Empty); } catch { }

        AppDomain.CurrentDomain.UnhandledException += (_, e) => Trace("APPDOMAIN: " + e.ExceptionObject);
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => Trace("ANDROIDENV: " + e.Exception);

        Trace("OnCreate:begin sdk=" + (int)Build.VERSION.SdkInt + " device=" + Build.Model);

        try
        {
            var sample = SampleContent.Load(Assets!);
            Trace("Sample content parsed: route='" + sample.Route.Name + "' tdbNodes=" +
                  sample.TrackDatabase.TrackNodes.Count + " trackSections=" +
                  sample.TrackDatabase.TrackSectionPositions.Count + " consistVehicles=" +
                  sample.Consist.Vehicles.Count);

            var controls = new TrainControlState();
            var game = new Game1(sample.TrackDatabase, controls);
            _game = game;
            _controls = controls;
            Trace("Game1 created (track from .tdb)");

            if (game.Services.GetService(typeof(View)) is not View gameView)
            {
                Trace("ERROR: MonoGame returned no View");
                ShowError("MonoGame returned no View");
                return;
            }

            var host = new FrameLayout(this);
            _host = host;
            host.AddView(gameView, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

            View overlay = null!;
            overlay = ContentScreen.Build(this, sample, () =>
            {
                DismissContentScreen();
                ShowControls();
                Trace("Content screen dismissed; showing 3D track demo with cab controls");
            }, BeginImport);
            _contentOverlay = overlay;
            host.AddView(overlay, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

            SetContentView(host);
            Trace("SetContentView done (content screen)");

            game.Run();
            Trace("game.Run() returned");
        }
        catch (Exception ex)
        {
            Trace("FATAL: " + ex);
            ShowError(ex.ToString());
        }
    }

    private void BeginImport()
    {
        var intent = new Intent(Intent.ActionOpenDocumentTree);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPersistableUriPermission);
        StartActivityForResult(intent, ImportFolderRequest);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode != ImportFolderRequest || resultCode != Result.Ok)
        {
            return;
        }

        var uri = data?.Data;
        if (uri is null)
        {
            return;
        }

        try
        {
            ContentResolver?.TakePersistableUriPermission(uri, ActivityFlags.GrantReadUriPermission);
        }
        catch (Exception ex)
        {
            Trace("Could not persist folder permission: " + ex.Message);
        }

        Trace("Importing addon from " + uri);
        Toast.MakeText(this, "Importing add-on...", ToastLength.Short)?.Show();

        Task.Run(() =>
        {
            try
            {
                var addon = AddonImporter.Load(this, uri, message => Trace("IMPORT: " + message));
                RunOnUiThread(() => OnAddonLoaded(addon));
            }
            catch (Exception ex)
            {
                Trace("IMPORT FAILED: " + ex);
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Import failed: " + ex.Message, ToastLength.Long)?.Show());
            }
        });
    }

    private void OnAddonLoaded(ImportedAddon addon)
    {
        Trace("Addon loaded: '" + addon.Name + "' shape=" + addon.ShapePath +
              " triangles=" + addon.Shape.TriangleCount + " textures=" + addon.Textures.Count);
        _game?.SetImportedShape(addon);
        DismissContentScreen();
        ShowControls();
        Toast.MakeText(
            this,
            addon.Name + " loaded (" + addon.Shape.TriangleCount + " triangles)",
            ToastLength.Short)?.Show();
    }

    private void DismissContentScreen()
    {
        if (_contentOverlay is not null && _host is not null)
        {
            _host.RemoveView(_contentOverlay);
            _contentOverlay = null;
        }
    }

    private void ShowControls()
    {
        if (_controlOverlay is not null || _host is null || _controls is null)
        {
            return;
        }

        var cabParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Bottom,
        };
        _controlOverlay = new ControlOverlay(this, _controls);
        _host.AddView(_controlOverlay, cabParams);
    }

    private void Trace(string message)
    {
        Log.Info(Tag, message);
        try
        {
            File.AppendAllText(_logPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + "\n");
        }
        catch
        {
        }
    }

    private void ShowError(string text)
    {
        var full = text + "\n\n--- startup.log ---\n" + SafeReadLog();
        var savedPath = TrySaveToDownloads("openrails-startup.log", full);

        var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };

        var copy = new Button(this) { Text = "Copy log" };
        copy.Click += (_, _) =>
        {
            var clipboard = GetSystemService(ClipboardService) as ClipboardManager;
            if (clipboard is not null)
            {
                clipboard.PrimaryClip = ClipData.NewPlainText("Open Rails log", full);
                Toast.MakeText(this, "Log copied", ToastLength.Short)?.Show();
            }
        };
        layout.AddView(copy);

        if (savedPath is not null)
        {
            var saved = new TextView(this) { Text = "Saved: " + savedPath, TextSize = 10f };
            saved.SetTextColor(global::Android.Graphics.Color.LightGreen);
            layout.AddView(saved);
        }

        var tv = new TextView(this) { Text = full, TextSize = 11f };
        tv.SetTextIsSelectable(true);
        tv.SetTextColor(global::Android.Graphics.Color.White);

        var scroll = new ScrollView(this);
        scroll.AddView(tv);
        layout.AddView(scroll);

        layout.SetBackgroundColor(global::Android.Graphics.Color.Black);
        SetContentView(layout);
    }

    private string? TrySaveToDownloads(string name, string content)
    {
        try
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            {
                return null;
            }

            var values = new ContentValues();
            values.Put(MediaStore.MediaColumns.DisplayName, name);
            values.Put(MediaStore.MediaColumns.MimeType, "text/plain");
            values.Put(MediaStore.MediaColumns.RelativePath, global::Android.OS.Environment.DirectoryDownloads);

            var uri = ContentResolver?.Insert(MediaStore.Downloads.ExternalContentUri!, values);
            if (uri is null)
            {
                return null;
            }

            using var stream = ContentResolver!.OpenOutputStream(uri);
            if (stream is null)
            {
                return null;
            }

            using var writer = new StreamWriter(stream);
            writer.Write(content);
            return "/sdcard/Download/" + name;
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, "saving log to Downloads failed: " + ex.Message);
            return null;
        }
    }

    private string SafeReadLog()
    {
        try { return File.Exists(_logPath) ? File.ReadAllText(_logPath) : "(no log)"; }
        catch (Exception ex) { return "(log read failed: " + ex.Message + ")"; }
    }
}
