namespace OpenRails.Core.Msts;

public enum MstsTokenKind
{
    Word,
    QuotedString,
    OpenParen,
    CloseParen,
}

public readonly record struct MstsToken(MstsTokenKind Kind, string Text, int Line)
{
    public override string ToString() => $"{Kind}:{Text}@{Line}";
}
