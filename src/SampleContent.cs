using System.Globalization;
using System.Text;
using Android.Content.Res;
using OpenRails.Core.Formats;

namespace OpenRails.Android;

/// <summary>
/// Loads the sample MSTS content shipped in <c>assets/samples</c> and formats it
/// for the on-device content screen.
/// </summary>
public sealed class SampleContent
{
    public const string RouteAsset = "samples/SampleRoute/SampleRoute.trk";
    public const string TrackDatabaseAsset = "samples/SampleRoute/SampleRoute.tdb";
    public const string ConsistAsset = "samples/SampleConsist.con";

    private SampleContent(RouteFile route, TrackDatabaseFile trackDatabase, ConsistFile consist)
    {
        Route = route;
        TrackDatabase = trackDatabase;
        Consist = consist;
    }

    public RouteFile Route { get; }

    public TrackDatabaseFile TrackDatabase { get; }

    public ConsistFile Consist { get; }

    public static SampleContent Load(AssetManager assets)
    {
        var route = RouteFile.Parse(ReadAsset(assets, RouteAsset));
        var trackDatabase = TrackDatabaseFile.Parse(ReadAsset(assets, TrackDatabaseAsset));
        var consist = ConsistFile.Parse(ReadAsset(assets, ConsistAsset));
        return new SampleContent(route, trackDatabase, consist);
    }

    public string BuildSummary()
    {
        var text = new StringBuilder();
        text.AppendLine("Parsed on device from assets:");
        text.AppendLine("  " + RouteAsset);
        text.AppendLine("  " + TrackDatabaseAsset);
        text.AppendLine("  " + ConsistAsset);
        text.AppendLine();

        text.AppendLine("ROUTE / .trk");
        text.AppendLine("  ID            " + Display(Route.RouteId));
        text.AppendLine("  Name          " + Display(Route.Name));
        text.AppendLine("  File base     " + Display(Route.FileName));
        text.AppendLine("  Loading screen " + Display(Route.LoadingScreen));
        text.AppendLine("  Description   " + Display(Route.Description));
        if (Route.SpeedLimit is { } speed)
        {
            text.AppendLine("  Speed limit   " + Num(speed) + " m/s (" + Num(speed * 3.6f) + " km/h)");
        }

        if (Route.RouteStart is { } start)
        {
            text.AppendLine("  Route start   tile (" + Num(start.TileX) + ", " + Num(start.TileZ) +
                            ") offset (" + Num(start.X) + ", " + Num(start.Z) + ")");
        }

        text.AppendLine();

        var nodes = TrackDatabase.TrackNodes;
        text.AppendLine("TRACK DATABASE / .tdb");
        text.AppendLine("  Nodes         " + nodes.Count + " (declared " +
                        TrackDatabase.DeclaredTrackNodeCount + ")");
        text.AppendLine("  Sections      " + TrackDatabase.TrackSectionPositions.Count);
        text.AppendLine("  Ends          " + Count(TrackNodeKind.End));
        text.AppendLine("  Junctions     " + Count(TrackNodeKind.Junction));
        text.AppendLine("  Vectors       " + Count(TrackNodeKind.Vector));
        text.AppendLine();

        foreach (var node in nodes.Take(8))
        {
            var kind = node.Kind.ToString().ToUpperInvariant().PadRight(8);
            var position = node.Position is { } p
                ? "(" + Num(p.X) + ", " + Num(p.Y) + ", " + Num(p.Z) + ")"
                : "(no position)";
            text.AppendLine("  Node " + node.Index.ToString(CultureInfo.InvariantCulture).PadLeft(3) +
                            "     " + kind + " " + position);
        }

        if (TrackDatabase.TrackSectionPositions.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("  First vector sections:");
            foreach (var section in TrackDatabase.TrackSectionPositions.Take(6))
            {
                text.AppendLine("    tile (" + section.TileX + ", " + section.TileZ + ") " +
                                "(" + Num(section.X) + ", " + Num(section.Y) + ", " + Num(section.Z) + ")");
            }
        }

        text.AppendLine();
        text.AppendLine("CONSIST / .con");
        text.AppendLine("  Name          " + Display(Consist.Name));
        text.AppendLine("  Vehicles      " + Consist.Vehicles.Count);
        for (var i = 0; i < Consist.Vehicles.Count; i++)
        {
            var vehicle = Consist.Vehicles[i];
            text.AppendLine("    " + (i + 1) + ". " + vehicle.Kind.PadRight(7) + " " +
                            Display(vehicle.Name) + "  [" + Display(vehicle.Folder) + "]");
        }

        return text.ToString();
    }

    private int Count(TrackNodeKind kind) =>
        TrackDatabase.TrackNodes.Count(node => node.Kind == kind);

    private static string ReadAsset(AssetManager assets, string name)
    {
        using var stream = assets.Open(name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string Display(string? value) =>
        string.IsNullOrEmpty(value) ? "(none)" : value;

    private static string Num(float value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}
