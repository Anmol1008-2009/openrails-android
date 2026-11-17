using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

public sealed record ConsistVehicle(string Kind, string? Folder, string? Name);

/// <summary>
/// Reader for MSTS/Open Rails consist files (<c>*.con</c>).
/// </summary>
public sealed class ConsistFile
{
    private ConsistFile(MstsBlock root)
    {
        Root = root;
        Config = root.Child("TrainCfg") ?? root;
        Vehicles = BuildVehicles(Config);
    }

    public MstsBlock Root { get; }

    public MstsBlock Config { get; }

    public string? Name => Config.GetString("Name");

    public IReadOnlyList<ConsistVehicle> Vehicles { get; }

    public static ConsistFile Parse(string text) => new(MstsParser.ParseSingle(text, "Train"));

    public static ConsistFile Load(string path) => Parse(File.ReadAllText(path));

    private static List<ConsistVehicle> BuildVehicles(MstsBlock config)
    {
        var vehicles = new List<ConsistVehicle>();

        foreach (var entry in config.Children)
        {
            if (entry.Name.Equals("Engine", StringComparison.OrdinalIgnoreCase))
            {
                if (entry.Child("EngineData") is { } data)
                {
                    vehicles.Add(new ConsistVehicle("Engine", data.GetValue(0), data.GetValue(1) ?? data.GetValue(0)));
                }
            }
            else if (entry.Name.Equals("Wagon", StringComparison.OrdinalIgnoreCase))
            {
                if (entry.Child("WagonData") is { } data)
                {
                    vehicles.Add(new ConsistVehicle("Wagon", data.GetValue(0), data.GetValue(1) ?? data.GetValue(0)));
                }
            }
        }

        return vehicles;
    }
}
