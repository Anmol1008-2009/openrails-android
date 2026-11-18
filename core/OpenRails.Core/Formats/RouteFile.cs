using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Reader for the MSTS/Open Rails route header file (<c>*.trk</c>).
/// Track geometry is not stored here; it lives in the route's <c>*.tdb</c>
/// file (see <see cref="TrackDatabaseFile"/>).
/// </summary>
public sealed class RouteFile
{
    public RouteFile(MstsBlock root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));

        if (root.Child("RouteStart") is { } start && start.Values.Count >= 4)
        {
            RouteStart = new RouteStart(
                start.GetFloatAt(0),
                start.GetFloatAt(1),
                start.GetFloatAt(2),
                start.GetFloatAt(3));
        }
    }

    public MstsBlock Root { get; }

    public string? RouteId => Root.GetString("RouteID") ?? Root.GetString("RouteId");

    public string? Name => Root.GetString("Name");

    public string? Description => Root.GetString("Description");

    /// <summary>Base name shared by the route's <c>.tdb</c>, <c>.rdb</c>, <c>.ref</c>, etc.</summary>
    public string? FileName => Root.GetString("FileName");

    public string? LoadingScreen => Root.GetString("LoadingScreen");

    public string? Graphic => Root.GetString("Graphic");

    /// <summary>Global route speed limit as stored by MSTS, in metres per second.</summary>
    public float? SpeedLimit => Root.GetFloat("SpeedLimit");

    public RouteStart? RouteStart { get; }

    public static RouteFile Parse(string text) => new(MstsParser.ParseSingle(text, "Tr_RouteFile"));

    public static RouteFile Load(string path) => Parse(File.ReadAllText(path));
}

/// <summary>
/// Starting tile and offset of a route, from <c>Tr_RouteFile/RouteStart</c>.
/// </summary>
public sealed record RouteStart(float TileX, float TileZ, float X, float Z);
