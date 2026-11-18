using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenRails.Core.Formats;
using OpenRails.Core.Simulation;

namespace OpenRails.Android;

/// <summary>
/// Minimal data-driven track viewer: geometry comes from the route's
/// <c>.tdb</c> vector sections, not from hard-coded shapes.
/// </summary>
public sealed class Game1 : Game
{
    private static readonly Color ClearColor = new(14, 20, 26);
    private static readonly Color GroundColor = new(20, 30, 32);
    private static readonly Color GridMinor = new(27, 41, 49);
    private static readonly Color GridMajor = new(40, 62, 74);
    private static readonly Color BallastColor = new(72, 68, 60);
    private static readonly Color RailColor = new(205, 205, 195);
    private static readonly Color CenterLineColor = new(180, 145, 60);
    private static readonly Color EndColor = new(255, 130, 70);
    private static readonly Color JunctionColor = new(90, 215, 255);

    private readonly GraphicsDeviceManager _graphics;
    private readonly TrackDatabaseFile _trackDatabase;
    private readonly TrainControlState _controls;
    private readonly List<Vector3> _trainPath = new();
    private readonly List<float> _trainPathLengths = new();
    private readonly VertexPositionColor[] _trainVertices = new VertexPositionColor[36];
    private float _trainDistance;
    private float _trainSpeed;
    private float _totalPathLength;

    private BasicEffect _effect = null!;
    private VertexBuffer _surfaceBuffer = null!;
    private VertexBuffer _lineBuffer = null!;
    private int _surfacePrimitiveCount;
    private int _linePrimitiveCount;

    public Game1(TrackDatabaseFile trackDatabase, TrainControlState controls)
    {
        _trackDatabase = trackDatabase ?? throw new ArgumentNullException(nameof(trackDatabase));
        _controls = controls ?? throw new ArgumentNullException(nameof(controls));

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
        };

        Content.RootDirectory = "Content";
    }

    protected override void LoadContent()
    {
        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
        };

        BuildScene();
    }

    protected override void UnloadContent()
    {
        _surfaceBuffer?.Dispose();
        _lineBuffer?.Dispose();
        _effect?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);

        if (_totalPathLength > 0f)
        {
            var target = _controls.Throttle * 45f * _controls.DirectionSign;
            var rate = MathF.Max(30f * _controls.Throttle, 45f * _controls.Brake);
            if (rate <= 0f)
            {
                rate = 4f;
            }

            _trainSpeed = MoveTowards(_trainSpeed, target, rate * dt);
            _trainDistance += _trainSpeed * dt;

            if (_trainDistance < 0f)
            {
                _trainDistance = 0f;
                _trainSpeed = 0f;
            }
            else if (_trainDistance > _totalPathLength)
            {
                _trainDistance = _totalPathLength;
                _trainSpeed = 0f;
            }
        }
        else
        {
            _trainSpeed = 0f;
        }

        _controls.SpeedMetersPerSecond = _trainSpeed;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(ClearColor);
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        var aspect = GraphicsDevice.Viewport.AspectRatio;
        _effect.World = Matrix.Identity;
        _effect.View = Matrix.CreateLookAt(new Vector3(105f, 95f, 130f), Vector3.Zero, Vector3.Up);
        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 2000f);

        GraphicsDevice.SetVertexBuffer(_surfaceBuffer);
        if (_surfacePrimitiveCount > 0)
        {
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _surfacePrimitiveCount);
            }
        }

        GraphicsDevice.SetVertexBuffer(_lineBuffer);
        if (_linePrimitiveCount > 0)
        {
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, _linePrimitiveCount);
            }
        }

        if (_trainPath.Count >= 2)
        {
            BuildTrainBox();
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList, _trainVertices, 0, 12);
            }
        }

        base.Draw(gameTime);
    }

    private void BuildScene()
    {
        var worldPoints = new List<(float X, float Y, float Z)>();
        foreach (var node in _trackDatabase.TrackNodes)
        {
            if (node.Position is { } position)
            {
                worldPoints.Add((position.WorldX, position.Y, position.WorldZ));
            }

            foreach (var section in node.VectorSections)
            {
                worldPoints.Add((section.WorldX, section.Y, section.WorldZ));
            }
        }

        var centerX = 0f;
        var centerY = 0f;
        var centerZ = 0f;
        var scale = 1f;
        var groundY = -2f;

        if (worldPoints.Count > 0)
        {
            var minX = worldPoints.Min(p => p.X);
            var maxX = worldPoints.Max(p => p.X);
            var minY = worldPoints.Min(p => p.Y);
            var maxY = worldPoints.Max(p => p.Y);
            var minZ = worldPoints.Min(p => p.Z);
            var maxZ = worldPoints.Max(p => p.Z);

            centerX = (minX + maxX) * 0.5f;
            centerY = (minY + maxY) * 0.5f;
            centerZ = (minZ + maxZ) * 0.5f;

            var extent = MathF.Max(maxX - minX, MathF.Max(maxZ - minZ, maxY - minY));
            scale = extent > 0.5f ? 120f / extent : 1f;
            groundY = ((minY - centerY) * scale) - 2f;
        }

        var surfaces = new List<VertexPositionColor>();
        var lines = new List<VertexPositionColor>();

        AddGround(surfaces, lines, groundY);
        AddTrack(surfaces, lines, centerX, centerY, centerZ, scale);
        AddNodeMarkers(lines, centerX, centerY, centerZ, scale);
        BuildTrainPath(centerX, centerY, centerZ, scale);

        _surfaceBuffer = new VertexBuffer(
            GraphicsDevice,
            typeof(VertexPositionColor),
            Math.Max(surfaces.Count, 1),
            BufferUsage.WriteOnly);
        _surfaceBuffer.SetData(surfaces.Count > 0
            ? surfaces.ToArray()
            : new[] { default(VertexPositionColor) });
        _surfacePrimitiveCount = surfaces.Count / 3;

        _lineBuffer = new VertexBuffer(
            GraphicsDevice,
            typeof(VertexPositionColor),
            Math.Max(lines.Count, 2),
            BufferUsage.WriteOnly);
        _lineBuffer.SetData(lines.Count > 0
            ? lines.ToArray()
            : new[] { default(VertexPositionColor), default(VertexPositionColor) });
        _linePrimitiveCount = lines.Count / 2;
    }

    private static void AddGround(
        List<VertexPositionColor> surfaces,
        List<VertexPositionColor> lines,
        float y)
    {
        const float half = 170f;

        AddQuad(
            surfaces,
            new Vector3(-half, y, -half),
            new Vector3(-half, y, half),
            new Vector3(half, y, half),
            new Vector3(half, y, -half),
            GroundColor);

        for (var i = -160; i <= 160; i += 10)
        {
            var color = i % 50 == 0 ? GridMajor : GridMinor;
            AddLine(lines, new Vector3(i, y, -half), new Vector3(i, y, half), color);
            AddLine(lines, new Vector3(-half, y, i), new Vector3(half, y, i), color);
        }
    }

    private void AddTrack(
        List<VertexPositionColor> surfaces,
        List<VertexPositionColor> lines,
        float centerX,
        float centerY,
        float centerZ,
        float scale)
    {
        var bedHalf = MathF.Max(0.8f, 2.5f * scale);
        var railOffset = MathF.Max(0.25f, 0.75f * scale);

        foreach (var node in _trackDatabase.TrackNodes)
        {
            var points = node.VectorSections
                .Select(section => ToLocal(section.WorldX, section.Y, section.WorldZ,
                    centerX, centerY, centerZ, scale))
                .ToList();

            for (var i = 0; i < points.Count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var dx = b.X - a.X;
                var dz = b.Z - a.Z;
                var length = MathF.Sqrt((dx * dx) + (dz * dz));
                if (length < 0.0001f)
                {
                    continue;
                }

                var px = -dz / length;
                var pz = dx / length;

                AddQuad(
                    surfaces,
                    new Vector3(a.X + (px * bedHalf), a.Y + 0.05f, a.Z + (pz * bedHalf)),
                    new Vector3(a.X - (px * bedHalf), a.Y + 0.05f, a.Z - (pz * bedHalf)),
                    new Vector3(b.X - (px * bedHalf), b.Y + 0.05f, b.Z - (pz * bedHalf)),
                    new Vector3(b.X + (px * bedHalf), b.Y + 0.05f, b.Z + (pz * bedHalf)),
                    BallastColor);

                AddLine(
                    lines,
                    new Vector3(a.X + (px * railOffset), a.Y + 0.15f, a.Z + (pz * railOffset)),
                    new Vector3(b.X + (px * railOffset), b.Y + 0.15f, b.Z + (pz * railOffset)),
                    RailColor);
                AddLine(
                    lines,
                    new Vector3(a.X - (px * railOffset), a.Y + 0.15f, a.Z - (pz * railOffset)),
                    new Vector3(b.X - (px * railOffset), b.Y + 0.15f, b.Z - (pz * railOffset)),
                    RailColor);
                AddLine(
                    lines,
                    new Vector3(a.X, a.Y + 0.12f, a.Z),
                    new Vector3(b.X, b.Y + 0.12f, b.Z),
                    CenterLineColor);
            }
        }
    }

    private void AddNodeMarkers(
        List<VertexPositionColor> lines,
        float centerX,
        float centerY,
        float centerZ,
        float scale)
    {
        foreach (var node in _trackDatabase.TrackNodes)
        {
            if (node.Kind is not (TrackNodeKind.End or TrackNodeKind.Junction) ||
                node.Position is null)
            {
                continue;
            }

            var position = node.Position.Value;

            var point = ToLocal(position.WorldX, position.Y, position.WorldZ,
                centerX, centerY, centerZ, scale);
            var color = node.Kind == TrackNodeKind.Junction ? JunctionColor : EndColor;
            var size = node.Kind == TrackNodeKind.Junction ? 2.5f : 1.5f;
            var y = point.Y + 0.4f;

            AddLine(lines,
                new Vector3(point.X - size, y, point.Z),
                new Vector3(point.X + size, y, point.Z), color);
            AddLine(lines,
                new Vector3(point.X, y, point.Z - size),
                new Vector3(point.X, y, point.Z + size), color);
        }
    }

    private void BuildTrainPath(
        float centerX,
        float centerY,
        float centerZ,
        float scale)
    {
        var node = _trackDatabase.TrackNodes
            .OrderByDescending(n => n.VectorSections.Count)
            .FirstOrDefault(n => n.VectorSections.Count >= 2);
        if (node is null)
        {
            return;
        }

        _trainPath.Clear();
        _trainPathLengths.Clear();
        _trainPathLengths.Add(0f);

        foreach (var section in node.VectorSections)
        {
            _trainPath.Add(ToLocal(
                section.WorldX, section.Y, section.WorldZ,
                centerX, centerY, centerZ, scale));
        }

        for (var i = 1; i < _trainPath.Count; i++)
        {
            var length = Vector3.Distance(_trainPath[i - 1], _trainPath[i]);
            _trainPathLengths.Add(_trainPathLengths[i - 1] + length);
        }

        _totalPathLength = _trainPathLengths[^1];
    }

    private Vector3 SamplePath(float distance, out Vector3 tangent)
    {
        distance = Math.Clamp(distance, 0f, _totalPathLength);

        var index = 0;
        while (index < _trainPath.Count - 2 && _trainPathLengths[index + 1] < distance)
        {
            index++;
        }

        var a = _trainPath[index];
        var b = _trainPath[index + 1];
        var segmentLength = _trainPathLengths[index + 1] - _trainPathLengths[index];
        var t = segmentLength > 0.0001f
            ? Math.Clamp((distance - _trainPathLengths[index]) / segmentLength, 0f, 1f)
            : 0f;

        tangent = b - a;
        return Vector3.Lerp(a, b, t);
    }

    private void BuildTrainBox()
    {
        var position = SamplePath(_trainDistance, out var tangent);
        var forward = tangent.LengthSquared() > 0.0001f
            ? Vector3.Normalize(tangent)
            : Vector3.UnitZ;
        var right = Vector3.Cross(Vector3.UnitY, forward);
        right = right.LengthSquared() > 0.0001f
            ? Vector3.Normalize(right)
            : Vector3.UnitX;

        var center = position + (Vector3.UnitY * 2.2f);
        const float halfWidth = 1.8f;
        const float halfHeight = 1.5f;
        const float halfLength = 5f;

        Vector3 Corner(float x, float y, float z) =>
            center + (right * (x * halfWidth)) + (Vector3.UnitY * (y * halfHeight)) +
            (forward * (z * halfLength));

        var b0 = Corner(-1f, -1f, -1f);
        var b1 = Corner(1f, -1f, -1f);
        var b2 = Corner(1f, -1f, 1f);
        var b3 = Corner(-1f, -1f, 1f);
        var t0 = Corner(-1f, 1f, -1f);
        var t1 = Corner(1f, 1f, -1f);
        var t2 = Corner(1f, 1f, 1f);
        var t3 = Corner(-1f, 1f, 1f);

        var body = new Color(190, 75, 45);
        var roof = new Color(235, 170, 70);
        var cab = new Color(245, 220, 120);

        var index = 0;
        void Tri(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            _trainVertices[index++] = new VertexPositionColor(a, color);
            _trainVertices[index++] = new VertexPositionColor(b, color);
            _trainVertices[index++] = new VertexPositionColor(c, color);
        }

        Tri(t0, t1, t2, roof);
        Tri(t0, t2, t3, roof);
        Tri(b0, b2, b1, body);
        Tri(b0, b3, b2, body);
        Tri(b0, b1, t1, body);
        Tri(b0, t1, t0, body);
        Tri(b3, t3, t2, cab);
        Tri(b3, t2, b2, cab);
        Tri(b0, t0, t3, body);
        Tri(b0, t3, b3, body);
        Tri(b1, b2, t2, body);
        Tri(b1, t2, t1, body);
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + (MathF.Sign(target - current) * maxDelta);
    }

    private static Vector3 ToLocal(
        float x,
        float y,
        float z,
        float centerX,
        float centerY,
        float centerZ,
        float scale) =>
        new((x - centerX) * scale, (y - centerY) * scale, (z - centerZ) * scale);

    private static void AddQuad(
        List<VertexPositionColor> vertices,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Color color)
    {
        vertices.Add(new VertexPositionColor(a, color));
        vertices.Add(new VertexPositionColor(b, color));
        vertices.Add(new VertexPositionColor(c, color));
        vertices.Add(new VertexPositionColor(a, color));
        vertices.Add(new VertexPositionColor(c, color));
        vertices.Add(new VertexPositionColor(d, color));
    }

    private static void AddLine(
        List<VertexPositionColor> vertices,
        Vector3 a,
        Vector3 b,
        Color color)
    {
        vertices.Add(new VertexPositionColor(a, color));
        vertices.Add(new VertexPositionColor(b, color));
    }
}
