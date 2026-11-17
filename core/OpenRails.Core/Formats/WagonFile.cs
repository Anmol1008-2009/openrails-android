using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Reader for MSTS/Open Rails wagon files (<c>*.wag</c>).
/// </summary>
public sealed class WagonFile
{
    public WagonFile(MstsBlock root) => Root = root ?? throw new ArgumentNullException(nameof(root));

    public MstsBlock Root { get; }

    public string? Name => Root.GetString("Name");

    public string? Shape => Root.GetString("WagonShape") ?? Root.GetString("Shape");

    public double? Mass => Root.GetDouble("Mass");

    public double? MaxVelocity => Root.GetDouble("MaxVelocity");

    public static WagonFile Parse(string text) => new(MstsParser.ParseSingle(text, "Wagon"));

    public static WagonFile Load(string path) => Parse(File.ReadAllText(path));
}
