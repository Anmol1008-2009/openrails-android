using OpenRails.Core.Formats;
using OpenRails.Core.Msts;
using Xunit;

namespace OpenRails.Core.Tests;

public class FormatTests
{
    [Fact]
    public void Reads_route_file()
    {
        var route = RouteFile.Parse(
            """
            SIMISA@@@@@@@@@@JINX0T1t______
            Tr_RouteFile (
            	RouteID ( USA1 )
            	Name ( "North American Route" )
            	Description ( "A test route" )
            )
            """);

        Assert.Equal("USA1", route.RouteId);
        Assert.Equal("North American Route", route.Name);
        Assert.Equal("A test route", route.Description);
    }

    [Fact]
    public void Reads_route_track_nodes()
    {
        var route = RouteFile.Parse(
            """
            SIMISA@@@@@@@@@@JINX0T1t______
            Tr_RouteFile (
                RouteID ( USA1 )
                TrackNode (
                    Position ( 100.5 -20.25 300 )
                )
                TrackNode (
                    Position ( 200 0 400 )
                )
                TrackNode (
                    Position ( abc 5 6 )
                )
            )
            """);

        var nodes = route.TrackNodePositions;
        Assert.Equal(3, nodes.Count);
        Assert.Equal((100.5f, -20.25f, 300f), nodes[0]);
        Assert.Equal((200f, 0f, 400f), nodes[1]);
        Assert.Equal((0f, 5f, 6f), nodes[2]);
    }

    [Fact]
    public void Route_without_nodes_has_empty_positions()
    {
        var route = RouteFile.Parse("Tr_RouteFile ( Name ( X ) )");
        Assert.Empty(route.TrackNodePositions);
    }

    [Fact]
    public void Reads_engine_file()
    {
        var engine = EngineFile.Parse(
            """
            SIMISA@@@@@@@@@@JINX0E1t______
            Engine (
            	WagonShape ( GP38.s )
            	Name ( "GP38-2" )
            	Mass ( 108.862 )
            	MaxPower ( 1491.4 )
            	MaxForce ( 244.7 )
            	MaxVelocity ( 44.7 )
            )
            """);

        Assert.Equal("GP38-2", engine.Name);
        Assert.Equal("GP38.s", engine.Shape);
        Assert.Equal(108.862, engine.Mass!.Value, 3);
        Assert.Equal(1491.4, engine.MaxPower!.Value, 1);
        Assert.Equal(44.7, engine.MaxVelocity!.Value, 1);
    }

    [Fact]
    public void Reads_wagon_file()
    {
        var wagon = WagonFile.Parse(
            """
            SIMISA@@@@@@@@@@JINX0W1t______
            Wagon (
            	WagonShape ( US2Freight1.s )
            	Name ( "US2 Freight 1" )
            	Mass ( 30.0 )
            )
            """);

        Assert.Equal("US2 Freight 1", wagon.Name);
        Assert.Equal("US2Freight1.s", wagon.Shape);
        Assert.Equal(30.0, wagon.Mass!.Value, 3);
    }

    [Fact]
    public void Reads_consist_file()
    {
        var consist = ConsistFile.Parse(
            """
            SIMISA@@@@@@@@@@JINX0D1t______
            Train (
            	TrainCfg (
            		Name ( "Default" )
            		Engine ( UiD ( 0 ) EngineData ( GP38 "GP38-2" ) )
            		Wagon ( UiD ( 1 ) WagonData ( US2Freight1 "US2 Freight 1" ) )
            	)
            )
            """);

        Assert.Equal("Default", consist.Name);
        Assert.Equal(2, consist.Vehicles.Count);

        Assert.Equal("Engine", consist.Vehicles[0].Kind);
        Assert.Equal("GP38", consist.Vehicles[0].Folder);
        Assert.Equal("GP38-2", consist.Vehicles[0].Name);

        Assert.Equal("Wagon", consist.Vehicles[1].Kind);
        Assert.Equal("US2Freight1", consist.Vehicles[1].Folder);
        Assert.Equal("US2 Freight 1", consist.Vehicles[1].Name);
    }

    [Fact]
    public void Rejects_wrong_root_block()
    {
        Assert.Throws<MstsFormatException>(() => RouteFile.Parse("Engine ( Name ( X ) )"));
    }
}
