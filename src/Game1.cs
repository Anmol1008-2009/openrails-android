using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenRails.Android;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private BasicEffect _effect = null!;
    private VertexBuffer _cube = null!;

    private float _angle;

    public Game1()
    {
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

        _cube = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), 36, BufferUsage.WriteOnly);
        _cube.SetData(BuildCube());
    }

    protected override void UnloadContent()
    {
        _cube?.Dispose();
        _effect?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _angle += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.8f;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 30, 40));

        var aspect = GraphicsDevice.Viewport.AspectRatio;
        _effect.World = Matrix.CreateRotationY(_angle) * Matrix.CreateRotationX(_angle * 0.5f);
        _effect.View = Matrix.CreateLookAt(new Vector3(0f, 1.5f, 4f), Vector3.Zero, Vector3.Up);
        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);

        GraphicsDevice.SetVertexBuffer(_cube);
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
        }

        base.Draw(gameTime);
    }

    private static VertexPositionColor[] BuildCube()
    {
        var corners = new[]
        {
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1),
            new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1), new Vector3(1, -1, 1),
            new Vector3(1, 1, 1), new Vector3(-1, 1, 1),
        };

        int[][] faces =
        {
            new[] { 0, 1, 2, 3 },
            new[] { 5, 4, 7, 6 },
            new[] { 4, 0, 3, 7 },
            new[] { 1, 5, 6, 2 },
            new[] { 3, 2, 6, 7 },
            new[] { 4, 5, 1, 0 },
        };

        var colors = new[]
        {
            new Color(70, 130, 180), new Color(60, 179, 113), new Color(218, 165, 32),
            new Color(205, 92, 92), new Color(147, 112, 219), new Color(255, 140, 0),
        };

        var verts = new VertexPositionColor[36];
        var i = 0;
        for (var f = 0; f < faces.Length; f++)
        {
            var q = faces[f];
            AddTriangle(verts, ref i, corners[q[0]], corners[q[1]], corners[q[2]], colors[f]);
            AddTriangle(verts, ref i, corners[q[0]], corners[q[2]], corners[q[3]], colors[f]);
        }

        return verts;
    }

    private static void AddTriangle(VertexPositionColor[] verts, ref int i, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        verts[i++] = new VertexPositionColor(a, color);
        verts[i++] = new VertexPositionColor(b, color);
        verts[i++] = new VertexPositionColor(c, color);
    }
}
