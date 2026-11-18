using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace OpenRails.Android;

/// <summary>
/// Full-screen Android view that proves the portable MSTS parsers against the
/// bundled sample content. It intentionally does not use the 3D renderer.
/// </summary>
public static class ContentScreen
{
    private static readonly Color Background = Color.ParseColor("#0E1620");
    private static readonly Color Accent = Color.ParseColor("#8FD3FF");
    private static readonly Color Muted = Color.ParseColor("#7F93A6");
    private static readonly Color Body = Color.ParseColor("#DCE6EF");

    public static View Build(Context context, SampleContent content, Action onContinue)
    {
        var root = new LinearLayout(context) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(Background);
        root.SetPadding(Dp(context, 24), Dp(context, 20), Dp(context, 24), Dp(context, 20));

        var title = new TextView(context) { Text = "Open Rails - sample content" };
        title.SetTextSize(ComplexUnitType.Sp, 22f);
        title.SetTypeface(Typeface.Default, TypefaceStyle.Bold);
        title.SetTextColor(Accent);
        root.AddView(title);

        var subtitle = new TextView(context)
        {
            Text = "MSTS route, track database and consist parsed on this device.",
        };
        subtitle.SetTextSize(ComplexUnitType.Sp, 12f);
        subtitle.SetTextColor(Muted);
        subtitle.SetPadding(0, Dp(context, 2), 0, Dp(context, 10));
        root.AddView(subtitle);

        var body = new TextView(context) { Text = content.BuildSummary() };
        body.SetTextSize(ComplexUnitType.Sp, 12f);
        body.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        body.SetTextColor(Body);
        body.SetTextIsSelectable(true);
        body.SetLineSpacing(0f, 1.15f);

        var scroll = new ScrollView(context) { FillViewport = true };
        scroll.AddView(body);
        root.AddView(scroll, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

        var continueButton = new Button(context) { Text = "Start 3D demo" };
        continueButton.Click += (_, _) => onContinue();
        root.AddView(continueButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        return root;
    }

    private static int Dp(Context context, int value) =>
        (int)((value * context.Resources!.DisplayMetrics!.Density) + 0.5f);
}
