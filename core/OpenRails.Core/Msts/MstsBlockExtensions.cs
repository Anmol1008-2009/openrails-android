using System.Globalization;

namespace OpenRails.Core.Msts;

public static class MstsBlockExtensions
{
    public static string? GetString(this MstsBlock block, string childName) =>
        block.Child(childName)?.FirstValue;

    public static int? GetInt(this MstsBlock block, string childName)
    {
        var raw = block.GetString(childName);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    public static double? GetDouble(this MstsBlock block, string childName)
    {
        var raw = block.GetString(childName);
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    public static float? GetFloat(this MstsBlock block, string childName) =>
        block.GetDouble(childName) is { } value ? (float)value : null;

    public static bool GetFlag(this MstsBlock block, string childName)
    {
        var raw = block.GetString(childName);
        return raw is not null && !string.Equals(raw, "0", StringComparison.Ordinal);
    }
}
