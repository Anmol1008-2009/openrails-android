using System;
using System.IO;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;

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
    private string _logPath = string.Empty;

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
            var game = new Game1();
            Trace("Game1 created");

            if (game.Services.GetService(typeof(View)) is not View view)
            {
                Trace("ERROR: MonoGame returned no View");
                ShowError("MonoGame returned no View");
                return;
            }

            SetContentView(view);
            Trace("SetContentView done");

            game.Run();
            Trace("game.Run() returned");
        }
        catch (Exception ex)
        {
            Trace("FATAL: " + ex);
            ShowError(ex.ToString());
        }
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
        var tv = new TextView(this)
        {
            Text = text + "\n\n--- startup.log ---\n" + SafeReadLog(),
            TextSize = 11f,
        };
        tv.SetTextIsSelectable(true);
        tv.SetTextColor(global::Android.Graphics.Color.White);

        var scroll = new ScrollView(this);
        scroll.AddView(tv);
        scroll.SetBackgroundColor(global::Android.Graphics.Color.Black);
        SetContentView(scroll);
    }

    private string SafeReadLog()
    {
        try { return File.Exists(_logPath) ? File.ReadAllText(_logPath) : "(no log)"; }
        catch (Exception ex) { return "(log read failed: " + ex.Message + ")"; }
    }
}
