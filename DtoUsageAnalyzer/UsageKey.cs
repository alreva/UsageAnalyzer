// <copyright file="UsageKey.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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

        var filePathComparison = string.Compare(this.FilePath, other.FilePath, StringComparison.Ordinal);
        if (filePathComparison != 0)
        {
            return filePathComparison;
        }

        return this.Attribute.CompareTo(other.Attribute);
    }
}
