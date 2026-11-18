using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using OpenRails.Core.Simulation;

namespace OpenRails.Android;

/// <summary>
/// Bottom cab-control strip: throttle and brake sliders plus a direction
/// button. Writes directly into the portable <see cref="TrainControlState"/>.
/// </summary>
public sealed class ControlOverlay : LinearLayout
{
    private readonly Activity _activity;
    private readonly TrainControlState _state;
    private readonly Handler _handler = new(Looper.MainLooper!);
    private readonly Java.Lang.Runnable _tick;
    private readonly TextView _status;
    private readonly Button _direction;
    private bool _running;

    public ControlOverlay(Activity activity, TrainControlState state) : base(activity)
    {
        _activity = activity;
        _state = state;
        _tick = new Java.Lang.Runnable(Refresh);

        Orientation = Orientation.Vertical;
        var pad = Dp(activity, 10);
        SetPadding(pad, pad, pad, pad);
        SetBackgroundColor(Color.Argb(190, 8, 14, 18));

        _status = new TextView(activity) { TextSize = 13f };
        _status.SetTextColor(Color.White);
        AddView(_status);

        var row = new LinearLayout(activity) { Orientation = Orientation.Horizontal };
        AddView(row, new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        AddSlider(activity, row, "Throttle", value => _state.Throttle = value);
        AddSlider(activity, row, "Brake", value => _state.Brake = value);

        _direction = new Button(activity) { Text = "Dir: Fwd" };
        _direction.Click += (_, _) => _state.ToggleDirection();
        row.AddView(_direction, new LayoutParams(Dp(activity, 96), ViewGroup.LayoutParams.WrapContent));

        _state.Changed += OnStateChanged;
        Refresh();
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        _running = true;
        _handler.RemoveCallbacks(_tick);
        _handler.Post(_tick);
    }

    protected override void OnDetachedFromWindow()
    {
        _running = false;
        _handler.RemoveCallbacks(_tick);
        _state.Changed -= OnStateChanged;
        base.OnDetachedFromWindow();
    }

    private void OnStateChanged(object? sender, EventArgs e) =>
        _activity.RunOnUiThread(Refresh);

    private void Refresh()
    {
        var direction = _state.Direction == TrainDirection.Forward ? "Fwd" : "Rev";
        _status.Text =
            $"Throttle {_state.Throttle * 100f:0}%   Brake {_state.Brake * 100f:0}%   " +
            $"{direction}   {_state.SpeedKph:0} km/h";
        _direction.Text = "Dir: " + direction;

        if (_running)
        {
            _handler.PostDelayed(_tick, 250);
        }
    }

    private static void AddSlider(
        Activity activity,
        LinearLayout row,
        string title,
        Action<float> changed)
    {
        var group = new LinearLayout(activity) { Orientation = Orientation.Vertical };
        var label = new TextView(activity) { Text = title, TextSize = 12f };
        label.SetTextColor(Color.LightGray);

        var bar = new SeekBar(activity) { Max = 100, Progress = 0 };
        bar.ProgressChanged += (_, e) => changed(e.Progress / 100f);

        group.AddView(label);
        group.AddView(bar);
        row.AddView(group, new LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
    }

    private static int Dp(Context context, float dp) =>
        (int)((dp * context.Resources!.DisplayMetrics!.Density) + 0.5f);
}
