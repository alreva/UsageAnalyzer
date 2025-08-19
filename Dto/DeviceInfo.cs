namespace Dto;

/// <summary>
/// Information about the user's device.
/// </summary>
public class DeviceInfo
{
  public required string DeviceType { get; init; }

  public required string Os { get; set; }

  public required string Browser { get; init; }

  public required string IpAddress { get; set; }
}
