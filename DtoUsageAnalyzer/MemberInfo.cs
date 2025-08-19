namespace DtoUsageAnalyzer;

using System.Reflection;

/// <summary>
/// Represents a member (property or field) discovered during analysis, containing member information and metadata.
/// </summary>
/// <param name="Member">The reflection member information (PropertyInfo or FieldInfo).</param>
/// <param name="DeclaringType">The type that declares this member.</param>
/// <param name="FullPath">The full dotted path to the member (e.g., "Address.City", "SocialMedia.Twitter").</param>
/// <param name="MemberType">The type of the member value.</param>
/// <param name="Name">The name of the member.</param>
/// <example>
/// For a User type with nested Address, this represents paths like:
/// - "Name" (primitive property)
/// - "Address.City" (nested object property)
/// - "deviceId" (field in nested object).
/// </example>
public record AnalyzedMember(
    MemberInfo Member,
    Type DeclaringType,
    string FullPath,
    Type MemberType,
    string Name);
