// <copyright file="UserEventProcessorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors.Tests;

using System.Text.Json;

public class UserEventProcessorTests
{
  [Fact]
  public void Process_ValidJson_WritesFormattedEventInfo()
  {
    // Arrange
    var json =
        """
            {
                "eventId": "12345",
                "timestamp": "2023-10-01T12:00:00Z",
                "source": "User Activity System",
                "message": "User has been imported.",
                "user": null
            }
            """;

    var processor = new UserEventProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);

    // Assert
    var result = output.ToString();
    Assert.Contains("Event Information:", result);
    Assert.Contains("Event ID: 12345", result);
    Assert.Contains("Timestamp: 2023-10-01 12:00:00", result);
    Assert.Contains("Source: User Activity System", result);
    Assert.Contains("Message: User has been imported.", result);
  }

  [Fact]
  public void Process_NullEvent_WritesNoDataMessage()
  {
    // Arrange
    var json = "null";
    var processor = new UserEventProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);

    // Assert
    var result = output.ToString();
    Assert.Contains("No event information found.", result);
  }
}