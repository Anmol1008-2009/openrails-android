using Android.Content;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace OpenRails.Android;

/// <summary>
/// Read-only view of a folder selected through the Storage Access Framework.
/// Files are addressed by their path relative to the selected tree so MSTS
/// references such as <c>../common/Library/foo.dds</c> can be resolved.
/// </summary>
public sealed class SafFolder
{
    private const string ColumnDocumentId = "document_id";
    private const string ColumnDisplayName = "_display_name";
    private const string ColumnMimeType = "mime_type";
    private const string MimeTypeDirectory = "vnd.android.document/directory";
    private const int MaxFiles = 50000;
    private const int MaxDepth = 12;

    private readonly AndroidUri _treeUri;
    private readonly Dictionary<string, SafFile> _files;

    private SafFolder(AndroidUri treeUri, Dictionary<string, SafFile> files)
    {
        _treeUri = treeUri;
        _files = files;
    }

    public IReadOnlyCollection<SafFile> Files => _files.Values;

    public static SafFolder Scan(Context context, AndroidUri treeUri)
    {
        var resolver = context.ContentResolver
            ?? throw new InvalidOperationException("No content resolver.");
        var rootId = DocumentsContract.GetTreeDocumentId(treeUri);
        if (string.IsNullOrEmpty(rootId))
        {
            throw new InvalidOperationException("The selected folder has no document id.");
        }

        var files = new Dictionary<string, SafFile>(StringComparer.OrdinalIgnoreCase);
        ScanDirectory(resolver, treeUri, rootId, string.Empty, files, 0);
        return new SafFolder(treeUri, files);
    }

    public SafFile? Find(string relativePath)
    {
        var normalized = Normalize(relativePath);
        return _files.TryGetValue(normalized, out var file) ? file : null;
    }

    public SafFile? FindByFileName(string fileName)
    {
        foreach (var file in _files.Values)
        {
            if (string.Equals(file.Name, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }

    /// <summary>Resolves an MSTS path relative to <paramref name="baseDirectory"/>.</summary>
    public SafFile? Resolve(string baseDirectory, string reference)
    {
        var combined = string.IsNullOrEmpty(baseDirectory)
            ? reference
            : baseDirectory + "/" + reference;
        var resolved = Normalize(combined);
        if (resolved.StartsWith("../", StringComparison.Ordinal))
        {
            return null;
        }

        return Find(resolved);
    }

    public Stream OpenRead(Context context, SafFile file)
    {
        var uri = DocumentsContract.BuildDocumentUriUsingTree(_treeUri, file.DocumentId);
        return context.ContentResolver?.OpenInputStream(uri)
            ?? throw new IOException("Could not open " + file.RelativePath);
    }

    public static string Normalize(string path)
    {
        var parts = path.Replace('\\', '/').Split('/');
        var stack = new List<string>();
        foreach (var part in parts)
        {
            if (part.Length == 0 || part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (stack.Count == 0 || stack[^1] == "..")
                {
                    stack.Add("..");
                }
                else
                {
                    stack.RemoveAt(stack.Count - 1);
                }

                continue;
            }

            stack.Add(part);
        }

        return string.Join('/', stack);
    }

    private static void ScanDirectory(
        ContentResolver resolver,
        AndroidUri treeUri,
        string documentId,
        string relativePath,
        Dictionary<string, SafFile> files,
        int depth)
    {
        if (depth > MaxDepth || files.Count >= MaxFiles)
        {
            return;
        }

        var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, documentId);
        using var cursor = resolver.Query(
            childrenUri,
            new[] { ColumnDocumentId, ColumnDisplayName, ColumnMimeType },
            null,
            null,
            null);
        if (cursor is null)
        {
            return;
        }

        var idIndex = cursor.GetColumnIndexOrThrow(ColumnDocumentId);
        var nameIndex = cursor.GetColumnIndexOrThrow(ColumnDisplayName);
        var mimeIndex = cursor.GetColumnIndexOrThrow(ColumnMimeType);

        while (cursor.MoveToNext() && files.Count < MaxFiles)
        {
            var childId = cursor.GetString(idIndex);
            var name = cursor.GetString(nameIndex);
            var mime = cursor.GetString(mimeIndex);
            if (string.IsNullOrEmpty(childId) || string.IsNullOrEmpty(name))
            {
                continue;
            }

            var childPath = string.IsNullOrEmpty(relativePath)
                ? name
                : relativePath + "/" + name;

            if (string.Equals(mime, MimeTypeDirectory, StringComparison.Ordinal))
            {
                ScanDirectory(resolver, treeUri, childId, childPath, files, depth + 1);
                continue;
            }

            var uri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, childId);
            files[childPath] = new SafFile(childPath, name, childId, uri);
        }
    }
}

public sealed class SafFile
{
    public SafFile(string relativePath, string name, string documentId, AndroidUri uri)
    {
        RelativePath = relativePath;
        Name = name;
        DocumentId = documentId;
        Uri = uri;
    }

    public string RelativePath { get; }

    public string Name { get; }

    public string DocumentId { get; }

    public AndroidUri Uri { get; }

    public string Directory
    {
        get
        {
            var index = RelativePath.LastIndexOf('/');
            return index < 0 ? string.Empty : RelativePath[..index];
        }
    }
}
