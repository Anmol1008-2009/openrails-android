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
                FileName ( USA1 )
                Description ( "A test route" )
                LoadingScreen ( Load.ace )
                SpeedLimit ( 22.352 )
                RouteStart ( -5306 14961 240 120 )
            )
            """);

        Assert.Equal("USA1", route.RouteId);
        Assert.Equal("North American Route", route.Name);
        Assert.Equal("USA1", route.FileName);
        Assert.Equal("A test route", route.Description);
        Assert.Equal("Load.ace", route.LoadingScreen);
        Assert.Equal(22.352f, route.SpeedLimit!.Value, 3);
        Assert.Equal(new RouteStart(-5306f, 14961f, 240f, 120f), route.RouteStart);
    }

    [Fact]
    public void Route_without_route_start_has_null_start()
    {
        var route = RouteFile.Parse("Tr_RouteFile ( Name ( X ) )");
        Assert.Null(route.RouteStart);
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
