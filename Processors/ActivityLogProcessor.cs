using Dto;

namespace Processors;

public class ActivityLogProcessor : BaseProcessor<UserEventDto>
{
    public override void Process(string jsonInput, TextWriter output)
    {
        var userEventDto = Deserialize(jsonInput);
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
            WriteNoDataMessage(output, "activity log");
        }
    }
} 