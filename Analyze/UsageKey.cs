namespace Analyze;

public record UsageKey(string FilePath, ClassAndField Attribute) : IComparable<UsageKey>
{
    public int CompareTo(UsageKey? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (other is null)
        {
            return 1;
        }

        var filePathComparison = string.Compare(FilePath, other.FilePath, StringComparison.Ordinal);
        if (filePathComparison != 0)
        {
            return filePathComparison;
        }

        return Attribute.CompareTo(other.Attribute);
    }
}