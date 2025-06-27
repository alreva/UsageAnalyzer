// <copyright file="UserEventProcessor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors;

using Dto;

public class UserEventProcessor : BaseProcessor<UserEventDto>
{
  public override void Process(string jsonInput, TextWriter output)
  {
    var userEventDto = this.Deserialize(jsonInput);
    if (userEventDto != null)
    {
      output.WriteLine("Event Information:");
      output.WriteLine($"Event ID: {userEventDto.EventId}");
      output.WriteLine($"Timestamp: {userEventDto.Timestamp:yyyy-MM-dd HH:mm:ss}");
      output.WriteLine($"Source: {userEventDto.Source}");
      output.WriteLine($"Message: {userEventDto.Message}");
    }
    else
    {
      this.WriteNoDataMessage(output, "event information");
    }
  }
}