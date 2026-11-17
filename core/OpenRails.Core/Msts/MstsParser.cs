namespace OpenRails.Core.Msts;

public static class MstsParser
{
    public static IReadOnlyList<MstsBlock> Parse(string text)
    {
        var tokens = MstsTokenizer.Tokenize(text);
        var blocks = new List<MstsBlock>();
        var index = 0;

        while (index < tokens.Count)
        {
            var block = ParseBlock(tokens, ref index);
            if (block is null)
            {
                break;
            }

            if (!IsComment(block.Name))
            {
                blocks.Add(block);
            }
        }

        return blocks;
    }

    public static MstsBlock ParseSingle(string text, string blockName)
    {
        var blocks = Parse(text);
        foreach (var block in blocks)
        {
            if (string.Equals(block.Name, blockName, StringComparison.OrdinalIgnoreCase))
            {
                return block;
            }
        }

        throw new MstsFormatException($"Expected block '{blockName}' was not found.");
    }

    private static bool IsComment(string name) =>
        string.Equals(name, "comment", StringComparison.OrdinalIgnoreCase);

    private static MstsBlock? ParseBlock(IReadOnlyList<MstsToken> tokens, ref int index)
    {
        while (index < tokens.Count)
        {
            if (tokens[index].Kind == MstsTokenKind.Word &&
                index + 1 < tokens.Count &&
                tokens[index + 1].Kind == MstsTokenKind.OpenParen)
            {
                break;
            }

            index++;
        }

        if (index >= tokens.Count)
        {
            return null;
        }

        var name = tokens[index].Text;
        index += 2;

        var block = new MstsBlock(name);

        while (index < tokens.Count && tokens[index].Kind != MstsTokenKind.CloseParen)
        {
            var token = tokens[index];

            if (token.Kind == MstsTokenKind.Word &&
                index + 1 < tokens.Count &&
                tokens[index + 1].Kind == MstsTokenKind.OpenParen)
            {
                var child = ParseBlock(tokens, ref index);
                if (child is null)
                {
                    break;
                }

                if (!IsComment(child.Name))
                {
                    block.Children.Add(child);
                }

                continue;
            }

            if (token.Kind is MstsTokenKind.Word or MstsTokenKind.QuotedString)
            {
                block.Values.Add(token.Text);
                index++;
                continue;
            }

            index++;
        }

        if (index < tokens.Count && tokens[index].Kind == MstsTokenKind.CloseParen)
        {
            index++;
        }

        return block;
    }
}
