using OpenRails.Core.Formats;
using Xunit;

namespace OpenRails.Core.Tests;

public class TrackDatabaseTests
{
    [Fact]
    public void Reads_track_nodes_and_vector_sections()
    {
        var tdb = TrackDatabaseFile.Parse(
            """
            SIMISA@@@@@@@@@@JINX0T1t______
            TrackDB (
                Serial ( 1 )
                TrackNodes ( 3
                    TrackNode ( 1
                        TrEndNode ( )
                        UiD ( 0 0 1 0 0 0 100 2 300 0 0 0 )
                        TrPins ( 0 1 TrPin ( 2 0 ) )
                    )
                    TrackNode ( 2
                        TrVectorNode (
                            TrVectorSections ( 2
                                1 1 0 0 0 0 1 00 0 0 500 0 0 1.5 0 0
                                1 1 0 0 0 0 1 00 0 0 600 0 0 1.5 0 0
                            )
                        )
                        TrPins ( 1 1 TrPin ( 1 0 ) TrPin ( 3 1 ) )
                    )
                    TrackNode ( 3
                        TrJunctionNode ( 0 5 0 )
                        UiD ( -1 2 3 0 -1 2 700 0 900 0 0 0 )
                        TrPins ( 1 2 TrPin ( 2 0 ) TrPin ( 4 0 ) TrPin ( 5 0 ) )
                    )
                )
                TrItemTable ( 0 )
            )
            """);

        Assert.Equal(3, tdb.DeclaredTrackNodeCount);
        Assert.Equal(3, tdb.TrackNodes.Count);

        Assert.Equal(1, tdb.TrackNodes[0].Index);
        Assert.Equal(TrackNodeKind.End, tdb.TrackNodes[0].Kind);
        Assert.Equal(new TrackNodePosition(0, 0, 100, 2, 300), tdb.TrackNodes[0].Position);
        Assert.Equal(100f, tdb.TrackNodes[0].Position!.Value.WorldX);

        Assert.Equal(TrackNodeKind.Vector, tdb.TrackNodes[1].Kind);
        Assert.Equal(2, tdb.TrackNodes[1].VectorSections.Count);
        Assert.Equal(new TrackNodePosition(0, 0, 500, 0, 0), tdb.TrackNodes[1].Position);
        Assert.Equal(600f, tdb.TrackNodes[1].VectorSections[1].X);

        Assert.Equal(TrackNodeKind.Junction, tdb.TrackNodes[2].Kind);
        Assert.Equal(-1, tdb.TrackNodes[2].Position!.Value.TileX);
        Assert.Equal(700f, tdb.TrackNodes[2].Position!.Value.X);
        Assert.Equal((-2048f) + 700f, tdb.TrackNodes[2].Position!.Value.WorldX);

        Assert.Equal(3, tdb.TrackNodePositions.Count);
        Assert.Equal(2, tdb.TrackSectionPositions.Count);
    }

    [Fact]
    public void Empty_track_database_is_supported()
    {
        var tdb = TrackDatabaseFile.Parse("TrackDB ( TrackNodes ( 0 ) TrItemTable ( 0 ) )");

        Assert.Equal(0, tdb.DeclaredTrackNodeCount);
        Assert.Empty(tdb.TrackNodes);
        Assert.Empty(tdb.TrackNodePositions);
        Assert.Empty(tdb.TrackSectionPositions);
    }
}
