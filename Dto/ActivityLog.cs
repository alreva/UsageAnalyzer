// <copyright file="ActivityLog.cs" company="alreva">
// Copyright (c) alreva. All rights reserved.
// </copyright>

namespace Dto;

/// <summary>
/// A record of user activity.
/// </summary>
public class ActivityLog
{
  public required string Action { get; set; }

  public required DateTime Timestamp { get; set; }

  public string? ProductId { get; set; }
}
