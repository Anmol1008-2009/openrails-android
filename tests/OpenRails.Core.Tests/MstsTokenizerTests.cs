using OpenRails.Core.Msts;
using Xunit;

namespace OpenRails.Core.Tests;

public class MstsTokenizerTests
{
    [Fact]
    public void Tokenizes_quoted_strings_parens_and_words()
    {
        var tokens = MstsTokenizer.Tokenize("\"North American Route\" \"a\\\"b\" 42 ( )");

        Assert.Equal(5, tokens.Count);
        Assert.Equal(MstsTokenKind.QuotedString, tokens[0].Kind);
        Assert.Equal("North American Route", tokens[0].Text);
        Assert.Equal("a\"b", tokens[1].Text);
        Assert.Equal("42", tokens[2].Text);
        Assert.Equal(MstsTokenKind.OpenParen, tokens[3].Kind);
        Assert.Equal(MstsTokenKind.CloseParen, tokens[4].Kind);
    }

    [Fact]
    public void Skips_hash_and_slash_comments()
    {
        var tokens = MstsTokenizer.Tokenize("# hash comment\n// slash comment\nfoo");

        Assert.Single(tokens);
        Assert.Equal("foo", tokens[0].Text);
    }

    [Fact]
    public void Tracks_line_numbers()
    {
        var tokens = MstsTokenizer.Tokenize("a\nb\n\nc");

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(2, tokens[1].Line);
        Assert.Equal(4, tokens[2].Line);
    }
}
