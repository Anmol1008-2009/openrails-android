using OpenRails.Core.Msts;
using Xunit;

namespace OpenRails.Core.Tests;

public class MstsParserTests
{
    [Fact]
    public void Parses_nested_blocks_and_ignores_comments()
    {
        var roots = MstsParser.Parse(
            """
            SIMISA@@@@@@@@@@JINX0T1t______
            Tr_RouteFile (
            	RouteID ( USA1 )
            	Name ( "North American Route" )
            	comment ( ignored )
            	TrackNode ( 1 2 3 )
            )
            """);

        var root = Assert.Single(roots);
        Assert.Equal("Tr_RouteFile", root.Name);
        Assert.Equal("USA1", root.GetString("RouteID"));
        Assert.Equal("North American Route", root.GetString("Name"));
        Assert.Null(root.Child("comment"));
        Assert.Equal(3, root.Child("TrackNode")!.Values.Count);
    }

    [Fact]
    public void Converts_numbers_and_flags()
    {
        var root = MstsParser.ParseSingle(
            """
            Thing (
            	Speed ( 40 )
            	Ratio ( 0.25 )
            	Enabled ( 1 )
            	Disabled ( 0 )
            )
            """,
            "Thing");

        Assert.Equal(40, root.GetInt("Speed"));
        Assert.Equal(0.25, root.GetDouble("Ratio"));
        Assert.True(root.GetFlag("Enabled"));
        Assert.False(root.GetFlag("Disabled"));
        Assert.Null(root.GetInt("Missing"));
    }

    [Fact]
    public void Throws_when_requested_block_is_missing()
    {
        Assert.Throws<MstsFormatException>(() => MstsParser.ParseSingle("nothing to see here", "Engine"));
    }
}
