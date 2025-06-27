// <copyright file="Preferences.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Dto;

/// <summary>
/// User preference settings.
/// </summary>
public class Preferences
{
  public required string Theme { get; set; }

  public required string Language { get; set; }

  public required bool Notifications { get; set; }

  public required bool Newsletter { get; set; }

  public required string Timezone { get; set; }
}
