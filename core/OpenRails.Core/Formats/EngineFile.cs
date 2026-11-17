using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Reader for MSTS/Open Rails locomotive files (<c>*.eng</c>).
/// </summary>
public sealed class EngineFile
{
    public EngineFile(MstsBlock root) => Root = root ?? throw new ArgumentNullException(nameof(root));

    public MstsBlock Root { get; }

    public string? Name => Root.GetString("Name");

    public string? Shape => Root.GetString("WagonShape") ?? Root.GetString("Shape");

    public double? Mass => Root.GetDouble("Mass");

    public double? MaxPower => Root.GetDouble("MaxPower");

    public double? MaxForce => Root.GetDouble("MaxForce");

    public double? MaxVelocity => Root.GetDouble("MaxVelocity");

    public static EngineFile Parse(string text) => new(MstsParser.ParseSingle(text, "Engine"));

    public static EngineFile Load(string path) => Parse(File.ReadAllText(path));
}
