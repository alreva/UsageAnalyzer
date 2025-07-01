// <copyright file="DeviceInfo.cs" company="alreva">
// Copyright (c) alreva. All rights reserved.
// </copyright>

namespace Dto;

/// <summary>
/// Information about the user's device.
/// </summary>
public class DeviceInfo
{
  public required string DeviceType { get; set; }

  public required string Os { get; set; }

  public required string Browser { get; set; }

  public required string IpAddress { get; set; }
}
