// <copyright file="Address.cs" company="alreva">
// Copyright (c) alreva. All rights reserved.
// </copyright>

namespace Dto;

/// <summary>
/// Mailing address information.
/// </summary>
public class Address
{
  public required string Street { get; set; }

  public required string City { get; set; }

  public required string State { get; set; }

  public required string ZipCode { get; set; }

  public required string Country { get; set; }
}
