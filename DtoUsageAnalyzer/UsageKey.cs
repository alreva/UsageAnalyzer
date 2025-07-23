namespace DtoUsageAnalyzer;

public record UsageKey(string FilePath, ClassAndField Attribute) : IComparable<UsageKey>
{
  /// <inheritdoc/>
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

    var filePathComparison = string.Compare(this.FilePath, other.FilePath, StringComparison.Ordinal);
    if (filePathComparison != 0)
    {
      return filePathComparison;
    }

    return this.Attribute.CompareTo(other.Attribute);
  }
}
