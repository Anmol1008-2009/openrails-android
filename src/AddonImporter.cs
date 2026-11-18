using System.Text;
using Android.Content;
using AndroidUri = Android.Net.Uri;
using OpenRails.Core.Formats;

namespace OpenRails.Android;

/// <summary>
/// Resolves a selected MSTS/Open Rails add-on folder into the data needed by
/// the renderer: a text shape plus its referenced DDS textures.
/// </summary>
public static class AddonImporter
{
    public static ImportedAddon Load(Context context, AndroidUri treeUri, Action<string>? log = null)
    {
        var folder = SafFolder.Scan(context, treeUri);
        log?.Invoke("SAF scan found " + folder.Files.Count + " files");

        SafFile? shapeFile = null;
        string? name = null;

        foreach (var wagonFile in folder.Files.Where(file =>
                     file.Name.EndsWith(".wag", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var wagon = WagonFile.Parse(ReadText(context, folder, wagonFile));
                if (string.IsNullOrEmpty(wagon.Shape))
                {
                    continue;
                }

                shapeFile = ResolveShape(folder, wagonFile.Directory, wagon.Shape);
                if (shapeFile is not null)
                {
                    name = wagon.Name;
                    log?.Invoke("Using wagon shape " + shapeFile.RelativePath);
                    break;
                }
            }
            catch (Exception ex)
            {
                log?.Invoke("Could not read " + wagonFile.RelativePath + ": " + ex.Message);
            }
        }

        if (shapeFile is null)
        {
            shapeFile = folder.Files.FirstOrDefault(file =>
                file.Name.EndsWith(".s", StringComparison.OrdinalIgnoreCase));
            log?.Invoke("No usable .wag shape reference; falling back to " +
                        (shapeFile?.RelativePath ?? "(none)"));
        }

        if (shapeFile is null)
        {
            throw new IOException("No .s shape found in the selected folder.");
        }

        var shape = ShapeFile.Parse(ReadText(context, folder, shapeFile));
        var textures = new DdsImage?[shape.ImageNames.Count];

        for (var i = 0; i < shape.ImageNames.Count; i++)
        {
            var imageName = shape.ImageNames[i];
            var textureFile = ResolveImage(folder, shapeFile.Directory, imageName);
            if (textureFile is null)
            {
                log?.Invoke("Texture not found: " + imageName);
                continue;
            }

            try
            {
                var bytes = ReadBytes(context, folder, textureFile);
                if (!LooksLikeDds(bytes))
                {
                    log?.Invoke("Skipping non-DDS texture: " + textureFile.RelativePath);
                    continue;
                }

                var maxDimension = imageName.Contains("shadow", StringComparison.OrdinalIgnoreCase)
                    ? 512
                    : 1024;
                textures[i] = DdsImage.Decode(bytes, maxDimension);
                log?.Invoke("Loaded texture " + textureFile.RelativePath + " (" +
                            textures[i]!.Width + "x" + textures[i]!.Height + ")");
            }
            catch (Exception ex)
            {
                log?.Invoke("Could not decode " + textureFile.RelativePath + ": " + ex.Message);
            }
        }

        return new ImportedAddon(
            string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(shapeFile.Name) : name,
            shapeFile.RelativePath,
            shape,
            textures);
    }

    private static SafFile? ResolveShape(SafFolder folder, string baseDirectory, string shapeName)
    {
        var direct = folder.Resolve(baseDirectory, shapeName);
        if (direct is not null && direct.Name.EndsWith(".s", StringComparison.OrdinalIgnoreCase))
        {
            return direct;
        }

        var fileName = Path.GetFileName(shapeName.Replace('\\', '/'));
        var byName = folder.FindByFileName(fileName);
        return byName is not null && byName.Name.EndsWith(".s", StringComparison.OrdinalIgnoreCase)
            ? byName
            : null;
    }

    private static SafFile? ResolveImage(SafFolder folder, string baseDirectory, string imageName)
    {
        var normalized = imageName.Replace('\\', '/');
        var candidates = new List<string> { normalized };
        if (normalized.EndsWith(".ace", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(normalized[..^4] + ".dds");
        }
        else if (normalized.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(normalized[..^4] + ".ace");
        }

        var fileName = Path.GetFileName(normalized);
        var stem = Path.GetFileNameWithoutExtension(normalized);
        candidates.Add(fileName);
        if (fileName.EndsWith(".ace", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(stem + ".dds");
        }
        else if (fileName.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(stem + ".ace");
        }

        foreach (var candidate in candidates)
        {
            var file = folder.Resolve(baseDirectory, candidate);
            if (file is not null)
            {
                return file;
            }

            file = folder.FindByFileName(Path.GetFileName(candidate));
            if (file is not null)
            {
                return file;
            }
        }

        return null;
    }

    private static bool LooksLikeDds(byte[] bytes) =>
        bytes.Length >= 4 && bytes[0] == 'D' && bytes[1] == 'D' && bytes[2] == 'S' && bytes[3] == ' ';

    private static string ReadText(Context context, SafFolder folder, SafFile file)
    {
        using var stream = folder.OpenRead(context, file);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return reader.ReadToEnd();
    }

    private static byte[] ReadBytes(Context context, SafFolder folder, SafFile file)
    {
        using var stream = folder.OpenRead(context, file);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

public sealed class ImportedAddon
{
    public ImportedAddon(
        string name,
        string shapePath,
        ShapeFile shape,
        IReadOnlyList<DdsImage?> textures)
    {
        Name = name;
        ShapePath = shapePath;
        Shape = shape;
        Textures = textures;
    }

    public string Name { get; }

    public string ShapePath { get; }

    public ShapeFile Shape { get; }

    public IReadOnlyList<DdsImage?> Textures { get; }
}
