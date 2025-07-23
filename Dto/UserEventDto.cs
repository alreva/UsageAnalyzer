namespace Dto;

/// <summary>
/// Root event DTO.
/// </summary>
public class UserEventDto
{
  public required string EventId { get; set; }

  public required DateTime Timestamp { get; set; }

  public required string Source { get; set; }

  public required string Message { get; set; }

  public required User User { get; set; }
}
