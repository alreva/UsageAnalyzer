namespace Dto;

/// <summary>
/// Mailing address information.
/// </summary>
public class Address
{
  public required string Street { get; init; }

  public required string City { get; set; }

  public required string State { get; set; }

  public required string ZipCode { get; set; }

  public required string Country { get; init; }
}
