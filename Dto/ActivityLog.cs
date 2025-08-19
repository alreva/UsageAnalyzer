namespace Dto;

/// <summary>
/// A record of user activity.
/// </summary>
public class ActivityLog
{
  public required string Action { get; set; }

  public required DateTime Timestamp { get; set; }

#pragma warning disable S1104
#pragma warning disable SA1401
#pragma warning disable SA1201
  public string? ProductId;
  public int ViewCount;
#pragma warning restore S1104
#pragma warning restore SA1401
#pragma warning restore SA1201
}
