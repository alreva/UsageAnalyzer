namespace DtoUsageAnalyzer;

/// <summary>
/// Represents a specific property within a class, used to identify DTO property usage patterns.
/// </summary>
/// <param name="ClassName">
/// The name of the class containing the property (e.g., "User", "Address", "DeviceInfo").
/// </param>
/// <param name="FieldName">
/// The name of the property/field being accessed (e.g., "Name", "ZipCode", "IpAddress").
/// </param>
/// <example>
/// <code>
/// // Represents accessing the Name property on a User class
/// var userNameField = new ClassAndField("User", "Name");
///
/// // Represents accessing the ZipCode property on an Address class
/// var addressZipField = new ClassAndField("Address", "ZipCode");
/// </code>
/// </example>
/// <remarks>
/// This record is used as a key for aggregating property usage across different files and contexts.
/// Two instances are considered equal if both ClassName and FieldName match exactly.
/// </remarks>
public record ClassAndField(string ClassName, string FieldName) : IComparable<ClassAndField>
{
  /// <inheritdoc/>
  public int CompareTo(ClassAndField? other)
  {
    if (ReferenceEquals(this, other))
    {
      return 0;
    }

    if (other is null)
    {
      return 1;
    }

    var classNameComparison = string.Compare(this.ClassName, other.ClassName, StringComparison.Ordinal);
    if (classNameComparison != 0)
    {
      return classNameComparison;
    }

    return string.Compare(this.FieldName, other.FieldName, StringComparison.Ordinal);
  }
}
