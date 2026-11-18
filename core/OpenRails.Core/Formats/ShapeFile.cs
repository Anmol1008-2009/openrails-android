using System.Globalization;
using System.Numerics;
using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Minimal reader for the text MSTS/Open Rails <c>.s</c> shape format.
/// It extracts points, texture coordinates, images and the first LOD's
/// indexed triangle lists. Binary shapes and animation matrices are not
/// handled yet.
/// </summary>
public sealed class ShapeFile
{
    public IReadOnlyList<Vector3> Points { get; private set; } = Array.Empty<Vector3>();

    public IReadOnlyList<Vector2> TextureCoordinates { get; private set; } = Array.Empty<Vector2>();

    public IReadOnlyList<string> ImageNames { get; private set; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, Matrix4x4> Matrices { get; private set; } =
        new Dictionary<string, Matrix4x4>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ShapePrimitiveGroup> PrimitiveGroups { get; private set; } =
        Array.Empty<ShapePrimitiveGroup>();

    public Vector3 BoundsMin { get; private set; }

    public Vector3 BoundsMax { get; private set; }

    public int TriangleCount { get; private set; }

    public static ShapeFile Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var reader = new ShapeTokenReader(new MstsTokenizer(text));
        var shape = new ShapeFile();

        if (reader.Seek("points"))
        {
            shape.Points = reader.ReadPoints();
        }

        if (reader.Seek("uv_points"))
        {
            shape.TextureCoordinates = reader.ReadUvPoints();
        }

        var imagesIndex = text.IndexOf("images", StringComparison.OrdinalIgnoreCase);
        var matricesIndex = text.IndexOf("matrices", StringComparison.OrdinalIgnoreCase);
        if (matricesIndex >= 0 && (imagesIndex < 0 || matricesIndex < imagesIndex))
        {
            if (reader.Seek("matrices"))
            {
                shape.Matrices = reader.ReadMatrices();
            }

            if (imagesIndex >= 0 && reader.Seek("images"))
            {
                shape.ImageNames = reader.ReadImages();
            }
        }
        else
        {
            if (imagesIndex >= 0 && reader.Seek("images"))
            {
                shape.ImageNames = reader.ReadImages();
            }

            if (matricesIndex >= 0 && reader.Seek("matrices"))
            {
                shape.Matrices = reader.ReadMatrices();
            }
        }

        var primTextures = new List<int>();
        var primNames = new List<string>();
        if (reader.Seek("prim_states"))
        {
            primTextures = reader.ReadPrimStates(primNames);
        }

        var groups = new Dictionary<int, List<ShapeVertex>>();
        if (reader.Seek("lod_controls") && reader.Seek("sub_objects"))
        {
            var subObjectCount = reader.ReadInt();
            for (var i = 0; i < subObjectCount; i++)
            {
                if (!reader.Seek("sub_object"))
                {
                    break;
                }

                reader.ReadSubObject(shape, primTextures, primNames, groups);
            }
        }

        shape.PrimitiveGroups = groups
            .Where(pair => pair.Value.Count > 0)
            .Select(pair => new ShapePrimitiveGroup(
                pair.Key,
                pair.Key >= 0 && pair.Key < shape.ImageNames.Count ? shape.ImageNames[pair.Key] : null,
                pair.Value))
            .ToList();

        shape.TriangleCount = shape.PrimitiveGroups.Sum(group => group.Vertices.Count / 3);
        shape.ComputeBounds();
        return shape;
    }

    private static Matrix4x4 ResolveMatrix(
        string name,
        IReadOnlyDictionary<string, Matrix4x4> matrices)
    {
        if (!matrices.TryGetValue(name, out var matrix))
        {
            return Matrix4x4.Identity;
        }

        // MSTS stores wheel matrices relative to their bogie. The name
        // convention is WHEELS<bogie><wheel>, e.g. WHEELS21 under BOGIE2.
        if (name.StartsWith("WHEELS", StringComparison.OrdinalIgnoreCase) &&
            name.Length > "WHEELS".Length &&
            char.IsDigit(name["WHEELS".Length]))
        {
            var parentName = "BOGIE" + name["WHEELS".Length];
            if (matrices.TryGetValue(parentName, out var parent))
            {
                matrix = Matrix4x4.Multiply(matrix, parent);
            }
        }

        return matrix;
    }

    private void ComputeBounds()
    {
        if (Points.Count == 0)
        {
            BoundsMin = Vector3.Zero;
            BoundsMax = Vector3.Zero;
            return;
        }

        var min = Points[0];
        var max = Points[0];
        foreach (var point in Points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        BoundsMin = min;
        BoundsMax = max;
    }

    private sealed class ShapeTokenReader
    {
        private readonly MstsTokenizer _tokenizer;
        private MstsToken? _current;
        private bool _hasCurrent;

        public ShapeTokenReader(MstsTokenizer tokenizer) => _tokenizer = tokenizer;

        public bool Seek(string name)
        {
            while (true)
            {
                var token = Read();
                if (token is null)
                {
                    return false;
                }

                if (token.Value.Kind != MstsTokenKind.Word ||
                    !string.Equals(token.Value.Text, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Peek() is { Kind: MstsTokenKind.OpenParen })
                {
                    Read();
                    return true;
                }
            }
        }

        public Vector3[] ReadPoints()
        {
            var count = ReadInt();
            var points = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                Expect("point");
                Expect("(");
                points[i] = new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
                Expect(")");
            }

            Expect(")");
            return points;
        }

        public Vector2[] ReadUvPoints()
        {
            var count = ReadInt();
            var points = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                Expect("uv_point");
                Expect("(");
                points[i] = new Vector2(ReadFloat(), ReadFloat());
                Expect(")");
            }

            Expect(")");
            return points;
        }

        public List<string> ReadImages()
        {
            var count = ReadInt();
            var images = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                Expect("image");
                Expect("(");
                images.Add(ReadText());
                Expect(")");
            }

            Expect(")");
            return images;
        }

        public List<int> ReadPrimStates(List<string> names)
        {
            var count = ReadInt();
            var textures = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                Expect("prim_state");
                names.Add(ReadText());
                Expect("(");
                textures.Add(ReadPrimStateTexture());
            }

            Expect(")");
            return textures;
        }

        public Dictionary<string, Matrix4x4> ReadMatrices()
        {
            var count = ReadInt();
            var matrices = new Dictionary<string, Matrix4x4>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                Expect("matrix");
                var name = ReadText();
                Expect("(");
                var values = new float[12];
                for (var value = 0; value < values.Length; value++)
                {
                    values[value] = ReadFloat();
                }
                Expect(")");

                matrices[name] = new Matrix4x4(
                    values[0], values[1], values[2], 0f,
                    values[3], values[4], values[5], 0f,
                    values[6], values[7], values[8], 0f,
                    values[9], values[10], values[11], 1f);
            }

            Expect(")");
            return matrices;
        }

        private int ReadPrimStateTexture()
        {
            var texture = -1;
            var depth = 1;
            while (depth > 0)
            {
                var token = Read() ?? throw new MstsFormatException("Unexpected end of shape.");
                if (token.Kind == MstsTokenKind.OpenParen)
                {
                    depth++;
                    continue;
                }

                if (token.Kind == MstsTokenKind.CloseParen)
                {
                    depth--;
                    continue;
                }

                if (token.Kind == MstsTokenKind.Word &&
                    string.Equals(token.Text, "tex_idxs", StringComparison.OrdinalIgnoreCase))
                {
                    Expect("(");
                    var count = ReadInt();
                    if (count > 0)
                    {
                        texture = ReadInt();
                        for (var i = 1; i < count; i++)
                        {
                            ReadInt();
                        }
                    }

                    Expect(")");
                }
            }

            return texture;
        }

        public void ReadSubObject(
            ShapeFile shape,
            List<int> primTextures,
            List<string> primNames,
            Dictionary<int, List<ShapeVertex>> groups)
        {
            if (!Seek("vertices"))
            {
                return;
            }

            var vertexCount = ReadInt();
            var pointIndices = new int[vertexCount];
            var uvIndices = new int[vertexCount];

            for (var i = 0; i < vertexCount; i++)
            {
                Expect("vertex");
                Expect("(");
                ReadText();
                pointIndices[i] = ReadInt();
                ReadInt();
                ReadText();
                ReadText();

                Expect("vertex_uvs");
                Expect("(");
                var uvCount = ReadInt();
                uvIndices[i] = uvCount > 0 ? ReadInt() : -1;
                for (var uv = 1; uv < uvCount; uv++)
                {
                    ReadInt();
                }

                Expect(")");
                Expect(")");
            }

            Expect(")");

            if (!Seek("primitives"))
            {
                return;
            }

            var primitiveBlockCount = ReadInt();
            var primitiveCount = (primitiveBlockCount + 1) / 2;
            for (var primitive = 0; primitive < primitiveCount; primitive++)
            {
                if (!Seek("prim_state_idx"))
                {
                    return;
                }

                var primState = ReadInt();
                Expect(")");

                if (!Seek("indexed_trilist"))
                {
                    return;
                }

                if (!Seek("vertex_idxs"))
                {
                    SkipBlock();
                    continue;
                }

                var indexCount = ReadInt();
                var indices = new int[indexCount];
                for (var i = 0; i < indexCount; i++)
                {
                    indices[i] = ReadInt();
                }

                Expect(")");
                SkipBlock();

                var texture = primState >= 0 && primState < primTextures.Count
                    ? primTextures[primState]
                    : -1;
                var matrix = Matrix4x4.Identity;
                if (primState >= 0 && primState < primNames.Count)
                {
                    var stateName = primNames[primState];
                    var separator = stateName.IndexOf('_');
                    var matrixName = separator > 0 ? stateName[..separator] : stateName;
                    matrix = ResolveMatrix(matrixName, shape.Matrices);
                }

                if (!groups.TryGetValue(texture, out var vertices))
                {
                    vertices = new List<ShapeVertex>();
                    groups.Add(texture, vertices);
                }

                for (var i = 0; i + 2 < indices.Length; i += 3)
                {
                    AddVertex(shape, vertices, pointIndices, uvIndices, indices[i], matrix);
                    AddVertex(shape, vertices, pointIndices, uvIndices, indices[i + 1], matrix);
                    AddVertex(shape, vertices, pointIndices, uvIndices, indices[i + 2], matrix);
                }
            }

            Expect(")");
        }

        private static void AddVertex(
            ShapeFile shape,
            List<ShapeVertex> vertices,
            int[] pointIndices,
            int[] uvIndices,
            int vertexIndex,
            Matrix4x4 matrix)
        {
            if (vertexIndex < 0 || vertexIndex >= pointIndices.Length)
            {
                return;
            }

            var pointIndex = pointIndices[vertexIndex];
            if (pointIndex < 0 || pointIndex >= shape.Points.Count)
            {
                return;
            }

            var uvIndex = uvIndices[vertexIndex];
            var uv = uvIndex >= 0 && uvIndex < shape.TextureCoordinates.Count
                ? shape.TextureCoordinates[uvIndex]
                : Vector2.Zero;

            var position = matrix.IsIdentity
                ? shape.Points[pointIndex]
                : Vector3.Transform(shape.Points[pointIndex], matrix);
            vertices.Add(new ShapeVertex(position, uv));
        }

        private MstsToken? Peek()
        {
            if (!_hasCurrent)
            {
                _current = _tokenizer.ReadToken();
                _hasCurrent = true;
            }

            return _current;
        }

        private MstsToken? Read()
        {
            if (_hasCurrent)
            {
                _hasCurrent = false;
                var current = _current;
                _current = null;
                return current;
            }

            return _tokenizer.ReadToken();
        }

        private void Expect(string text)
        {
            var token = Read();
            if (token is null || !string.Equals(token.Value.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                throw new MstsFormatException(
                    $"Expected '{text}', found '{token?.Text ?? "end of file"}' on line {token?.Line ?? 0}.");
            }
        }

        private void SkipBlock()
        {
            var depth = 1;
            while (depth > 0)
            {
                var token = Read();
                if (token is null)
                {
                    return;
                }

                if (token.Value.Kind == MstsTokenKind.OpenParen)
                {
                    depth++;
                }
                else if (token.Value.Kind == MstsTokenKind.CloseParen)
                {
                    depth--;
                }
            }
        }

        public int ReadInt()
        {
            var token = Read() ?? throw new MstsFormatException("Unexpected end of shape reading an integer.");
            if (!int.TryParse(token.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                throw new MstsFormatException($"Expected an integer, found '{token.Text}' on line {token.Line}.");
            }

            return value;
        }

        private float ReadFloat()
        {
            var token = Read() ?? throw new MstsFormatException("Unexpected end of shape reading a number.");
            if (!float.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new MstsFormatException($"Expected a number, found '{token.Text}' on line {token.Line}.");
            }

            return value;
        }

        private string ReadText()
        {
            var token = Read() ?? throw new MstsFormatException("Unexpected end of shape reading text.");
            return token.Text;
        }
    }
}

public sealed class ShapePrimitiveGroup
{
    public ShapePrimitiveGroup(int textureIndex, string? textureName, IReadOnlyList<ShapeVertex> vertices)
    {
        TextureIndex = textureIndex;
        TextureName = textureName;
        Vertices = vertices;
    }

    public int TextureIndex { get; }

    public string? TextureName { get; }

    public IReadOnlyList<ShapeVertex> Vertices { get; }
}

public readonly record struct ShapeVertex(Vector3 Position, Vector2 TextureCoordinate);
