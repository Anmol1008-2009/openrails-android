using System.IO;
using OpenRails.Core.Formats;
using Xunit;

namespace OpenRails.Core.Tests;

public class SampleContentTests
{
    private static string SamplePath(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "samples", Path.Combine(parts));

    [Fact]
    public void Sample_route_header_parses()
    {
        var route = RouteFile.Load(SamplePath("SampleRoute", "SampleRoute.trk"));

        Assert.Equal("SampleRoute", route.RouteId);
        Assert.Equal("SampleRoute", route.FileName);
        Assert.Equal("Open Rails Android Sample Route", route.Name);
        Assert.StartsWith("Synthetic route header", route.Description);
        Assert.Equal(new RouteStart(0f, 0f, 240f, 120f), route.RouteStart);
        Assert.Equal(22.352f, route.SpeedLimit!.Value, 3);
    }

    [Fact]
    public void Sample_track_database_parses()
    {
        var tdb = TrackDatabaseFile.Load(SamplePath("SampleRoute", "SampleRoute.tdb"));

        Assert.Equal(6, tdb.DeclaredTrackNodeCount);
        Assert.Equal(6, tdb.TrackNodes.Count);
        Assert.Equal(TrackNodeKind.End, tdb.TrackNodes[0].Kind);
        Assert.Equal(TrackNodeKind.Junction, tdb.TrackNodes[1].Kind);
        Assert.Equal(TrackNodeKind.Vector, tdb.TrackNodes[2].Kind);
        Assert.Equal(4, tdb.TrackNodes[2].VectorSections.Count);
        Assert.Equal(7, tdb.TrackSectionPositions.Count);
        Assert.All(tdb.TrackNodes, node => Assert.NotNull(node.Position));
    }

    [Fact]
    public void Sample_consist_parses()
    {
        var consist = ConsistFile.Load(SamplePath("SampleConsist.con"));

        Assert.Equal("Sample Consist", consist.Name);
        Assert.Equal(3, consist.Vehicles.Count);
        Assert.Equal("Engine", consist.Vehicles[0].Kind);
        Assert.Equal("GP38-2", consist.Vehicles[0].Name);
        Assert.Equal("US2 Freight 1", consist.Vehicles[2].Name);
    }
}
