// <copyright file="ActivityLogProcessor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors;

using Dto;

public class ActivityLogProcessor : BaseProcessor<UserEventDto>
{
  public override void Process(string jsonInput, TextWriter output)
  {
    var userEventDto = this.Deserialize(jsonInput);
    if (userEventDto?.User?.ActivityLog != null && userEventDto.User.ActivityLog.Any())
    {
      output.WriteLine("User Activity Log:");
      foreach (var activity in userEventDto.User.ActivityLog.OrderByDescending(a => a.Timestamp))
      {
        output.WriteLine($"Action: {activity.Action}");
        output.WriteLine($"Timestamp: {activity.Timestamp}");
        if (!string.IsNullOrEmpty(activity.ProductId))
        {
          output.WriteLine($"Product ID: {activity.ProductId}");
        }

        output.WriteLine();
      }
    }
    else
    {
      this.WriteNoDataMessage(output, "activity log");
    }
  }
}