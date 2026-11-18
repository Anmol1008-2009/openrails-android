using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Reader for the MSTS/Open Rails track database (<c>*.tdb</c>).
/// This is where real route track nodes and vector sections live, unlike the
/// route header (<c>*.trk</c>) which only has metadata and a starting point.
/// </summary>
public sealed class TrackDatabaseFile
{
    private const int VectorSectionValueCount = 16;

    public TrackDatabaseFile(MstsBlock root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));

        var trackNodes = root.Child("TrackNodes");
        DeclaredTrackNodeCount = trackNodes?.GetIntAt(0) ?? 0;

        var nodes = new List<TrackNode>();
        if (trackNodes is not null)
        {
            foreach (var block in trackNodes.ChildrenNamed("TrackNode"))
            {
                nodes.Add(ParseNode(block));
            }
        }

        TrackNodes = nodes;
        TrackNodePositions = nodes
            .Where(node => node.Position is not null)
            .Select(node => node.Position!.Value)
            .ToList();
        TrackSectionPositions = nodes
            .SelectMany(node => node.VectorSections)
            .ToList();
    }

    public MstsBlock Root { get; }

    /// <summary>Count declared in <c>TrackNodes ( N ... )</c>; may differ from <see cref="TrackNodes"/>.</summary>
    public int DeclaredTrackNodeCount { get; }

    public IReadOnlyList<TrackNode> TrackNodes { get; }

    /// <summary>One position per node: from <c>UiD</c>, or the first vector section.</summary>
    public IReadOnlyList<TrackNodePosition> TrackNodePositions { get; }

    /// <summary>Every section position of all vector nodes, in file order.</summary>
    public IReadOnlyList<TrackNodePosition> TrackSectionPositions { get; }

    public static TrackDatabaseFile Parse(string text) =>
        new(MstsParser.ParseSingle(text, "TrackDB"));

    public static TrackDatabaseFile Load(string path) => Parse(File.ReadAllText(path));

    private static TrackNode ParseNode(MstsBlock block)
    {
        var index = block.GetIntAt(0);

        var kind = block.Has("TrEndNode")
            ? TrackNodeKind.End
            : block.Has("TrJunctionNode")
                ? TrackNodeKind.Junction
                : block.Has("TrVectorNode")
                    ? TrackNodeKind.Vector
                    : TrackNodeKind.Unknown;

        var sections = ParseVectorSections(block.Child("TrVectorNode"));

        TrackNodePosition? position = null;
        if (block.Child("UiD") is { } uid && uid.Values.Count >= 9)
        {
            position = new TrackNodePosition(
                uid.GetIntAt(4),
                uid.GetIntAt(5),
                uid.GetFloatAt(6),
                uid.GetFloatAt(7),
                uid.GetFloatAt(8));
        }
        else if (sections.Count > 0)
        {
            position = sections[0];
        }

        return new TrackNode(index, kind, position, sections);
    }

    private static List<TrackNodePosition> ParseVectorSections(MstsBlock? vectorNode)
    {
        var sections = new List<TrackNodePosition>();
        if (vectorNode?.Child("TrVectorSections") is not { } block)
        {
            return sections;
        }

        var count = block.GetIntAt(0);
        for (var i = 0; i < count; i++)
        {
            var offset = 1 + (i * VectorSectionValueCount);
            if (offset + VectorSectionValueCount > block.Values.Count)
            {
                break;
            }

            sections.Add(new TrackNodePosition(
                block.GetIntAt(offset + 8),
                block.GetIntAt(offset + 9),
                block.GetFloatAt(offset + 10),
                block.GetFloatAt(offset + 11),
                block.GetFloatAt(offset + 12)));
        }

        return sections;
    }
}

public enum TrackNodeKind
{
    Unknown,
    End,
    Junction,
    Vector,
}

public sealed class TrackNode
{
    public TrackNode(
        int index,
        TrackNodeKind kind,
        TrackNodePosition? position,
        IReadOnlyList<TrackNodePosition> vectorSections)
    {
        Index = index;
        Kind = kind;
        Position = position;
        VectorSections = vectorSections;
    }

    public int Index { get; }

    public TrackNodeKind Kind { get; }

    public TrackNodePosition? Position { get; }

    public IReadOnlyList<TrackNodePosition> VectorSections { get; }
}

/// <summary>
/// A tile-local MSTS position. World coordinates use 2048 m tiles.
/// </summary>
public readonly record struct TrackNodePosition(int TileX, int TileZ, float X, float Y, float Z)
{
    public const float TileSizeM = 2048f;

    public float WorldX => (TileX * TileSizeM) + X;

    public float WorldZ => (TileZ * TileSizeM) + Z;
}
