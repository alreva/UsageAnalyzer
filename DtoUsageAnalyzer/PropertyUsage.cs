namespace DtoUsageAnalyzer;

/// <summary>
/// Represents the usage information for a specific DTO property.
/// Combines a property identifier with its usage count.
/// </summary>
/// <param name="Property">The property identifier including file location and field information.</param>
/// <param name="UsageCount">The number of times this property was accessed in the analyzed code.</param>
public record PropertyUsage(UsageKey Property, int UsageCount);
