namespace Analyze;

public record ClassAndField(string ClassName, string FieldName) : IComparable<ClassAndField>
{
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

        var classNameComparison = string.Compare(ClassName, other.ClassName, StringComparison.Ordinal);
        if (classNameComparison != 0)
        {
            return classNameComparison;
        }

        return string.Compare(FieldName, other.FieldName, StringComparison.Ordinal);
    }
}