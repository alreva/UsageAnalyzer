namespace DtoUsageAnalyzer;

/// <summary>
/// Represents a unique location where a DTO property is accessed, combining file path and property information.
/// </summary>
/// <param name="FilePath">
/// The file path where the property usage occurs.
/// For unused properties, this will be "N/A".
/// </param>
/// <param name="Attribute">
/// The class and field information identifying which property is being accessed.
/// </param>
/// <example>
/// <code>
/// // Property usage in a specific file
/// var key1 = new UsageKey("UserProcessor.cs", new ClassAndField("User", "Name"));
///
/// // Unused property
/// var key2 = new UsageKey("N/A", new ClassAndField("User", "CreatedAt"));
/// </code>
/// </example>
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

/// <summary>
/// Represents property usage data, combining the property location with its usage count.
/// </summary>
/// <param name="Property">The unique key identifying where and which property is accessed.</param>
/// <param name="UsageCount">The number of times this property is used in the specified location.</param>
/// <example>
/// <code>
/// // Property used 5 times in UserProcessor.cs
/// var usage1 = new PropertyUsage(
///     new UsageKey("UserProcessor.cs", new ClassAndField("User", "Name")),
///     5);
///
/// // Unused property
/// var usage2 = new PropertyUsage(
///     new UsageKey("N/A", new ClassAndField("User", "CreatedAt")),
///     0);
/// </code>
/// </example>
public record PropertyUsage(UsageKey Property, int UsageCount);
