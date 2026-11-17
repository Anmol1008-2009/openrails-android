namespace OpenRails.Core.Msts;

public sealed class MstsBlock
{
    public MstsBlock(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

    public string Name { get; }

    public List<string> Values { get; } = new();

    public List<MstsBlock> Children { get; } = new();

    public string? FirstValue => Values.Count > 0 ? Values[0] : null;

    public string? GetValue(int index) => index >= 0 && index < Values.Count ? Values[index] : null;

    public MstsBlock? Child(string name) =>
        Children.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<MstsBlock> ChildrenNamed(string name) =>
        Children.Where(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool Has(string name) => Child(name) is not null;

    public override string ToString() => $"{Name} ({Values.Count} values, {Children.Count} children)";
}
