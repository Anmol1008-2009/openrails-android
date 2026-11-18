using System.Text;

namespace OpenRails.Core.Msts;

public sealed class MstsTokenizer
{
    private readonly string _text;
    private int _index;
    private int _line = 1;

    public MstsTokenizer(string text) => _text = text ?? throw new ArgumentNullException(nameof(text));

    public static IReadOnlyList<MstsToken> Tokenize(string text) => new MstsTokenizer(text).Tokenize();

    public IReadOnlyList<MstsToken> Tokenize()
    {
        var tokens = new List<MstsToken>();
        while (true)
        {
            var token = NextToken();
            if (token is null)
            {
                return tokens;
            }

            tokens.Add(token.Value);
        }
    }

    /// <summary>Reads the next token without materialising the full token list.</summary>
    public MstsToken? ReadToken() => NextToken();

    private MstsToken? NextToken()
    {
        SkipTrivia();
        if (_index >= _text.Length)
        {
            return null;
        }

        var c = _text[_index];
        switch (c)
        {
            case '(':
                _index++;
                return new MstsToken(MstsTokenKind.OpenParen, "(", _line);
            case ')':
                _index++;
                return new MstsToken(MstsTokenKind.CloseParen, ")", _line);
            case '"':
                return ReadQuoted();
            default:
                return ReadWord();
        }
    }

    private void SkipTrivia()
    {
        while (_index < _text.Length)
        {
            var c = _text[_index];

            if (c == '\uFEFF')
            {
                _index++;
                continue;
            }

            if (c == '\n')
            {
                _line++;
                _index++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                _index++;
                continue;
            }

            if (c == '#' || (c == '/' && _index + 1 < _text.Length && _text[_index + 1] == '/'))
            {
                while (_index < _text.Length && _text[_index] != '\n')
                {
                    _index++;
                }

                continue;
            }

            return;
        }
    }

    private MstsToken ReadQuoted()
    {
        var startLine = _line;
        _index++;
        var builder = new StringBuilder();

        while (_index < _text.Length)
        {
            var c = _text[_index++];

            if (c == '"')
            {
                break;
            }

            if (c == '\\' && _index < _text.Length)
            {
                var escaped = _text[_index++];
                builder.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '0' => '\0',
                    _ => escaped,
                });
                continue;
            }

            if (c == '\n')
            {
                _line++;
            }

            builder.Append(c);
        }

        return new MstsToken(MstsTokenKind.QuotedString, builder.ToString(), startLine);
    }

    private MstsToken ReadWord()
    {
        var start = _index;
        var startLine = _line;

        while (_index < _text.Length)
        {
            var c = _text[_index];

            if (char.IsWhiteSpace(c) || c == '(' || c == ')' || c == '"')
            {
                break;
            }

            if (c == '#' || (c == '/' && _index + 1 < _text.Length && _text[_index + 1] == '/'))
            {
                break;
            }

            _index++;
        }

        return new MstsToken(MstsTokenKind.Word, _text[start.._index], startLine);
    }
}
