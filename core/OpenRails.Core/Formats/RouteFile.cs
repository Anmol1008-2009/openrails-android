using System.Globalization;
using System.Linq;
using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Reader for the MSTS/Open Rails route header file (<c>*.trk</c>).
/// </summary>
public sealed class RouteFile
{
    public RouteFile(MstsBlock root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        TrackNodePositions = root.ChildrenNamed("TrackNode")
            .SelectMany(n => n.ChildrenNamed("Position"))
            .Where(p => p.Values.Count >= 3)
            .Select(p => ParseVec3(p.Values))
            .ToList();
    }

    public MstsBlock Root { get; }

    public string? RouteId => Root.GetString("RouteID") ?? Root.GetString("RouteId");

    public string? Name => Root.GetString("Name");

    public string? Description => Root.GetString("Description");

    public IReadOnlyList<(float X, float Y, float Z)> TrackNodePositions { get; }

    public static RouteFile Parse(string text) => new(MstsParser.ParseSingle(text, "Tr_RouteFile"));

    public static RouteFile Load(string path) => Parse(File.ReadAllText(path));

    private static (float X, float Y, float Z) ParseVec3(List<string> v)
    {
        float X(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : 0f;
        return (X(v[0]), X(v[1]), X(v[2]));
    }
}
