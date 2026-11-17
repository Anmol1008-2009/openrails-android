using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Reader for the MSTS/Open Rails route header file (<c>*.trk</c>).
/// </summary>
public sealed class RouteFile
{
    public RouteFile(MstsBlock root) => Root = root ?? throw new ArgumentNullException(nameof(root));

    public MstsBlock Root { get; }

    public string? RouteId => Root.GetString("RouteID") ?? Root.GetString("RouteId");

    public string? Name => Root.GetString("Name");

    public string? Description => Root.GetString("Description");

    public static RouteFile Parse(string text) => new(MstsParser.ParseSingle(text, "Tr_RouteFile"));

    public static RouteFile Load(string path) => Parse(File.ReadAllText(path));
}
