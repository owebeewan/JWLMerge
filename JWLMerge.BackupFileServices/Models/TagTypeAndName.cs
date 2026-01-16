namespace JWLMerge.BackupFileServices.Models;

internal sealed class TagTypeAndName(int type, string name)
{
    public int TagType { get; } = type;

    public string Name { get; } = name;

    public override int GetHashCode() => new { TagType, Name }.GetHashCode();

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            null => false,
            TagTypeAndName o => TagType == o.TagType &&
                                   Name == o.Name,
            _ => false,
        };
    }
}