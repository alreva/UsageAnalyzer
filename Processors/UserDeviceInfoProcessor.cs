using Dto;

namespace Processors;

public class UserDeviceInfoProcessor : BaseProcessor<UserEventDto>
{
    public override void Process(string jsonInput, TextWriter output)
    {
        var userEventDto = Deserialize(jsonInput);
        if (userEventDto?.User?.DeviceInfo != null)
        {
            var deviceInfo = userEventDto.User.DeviceInfo;
            output.WriteLine("Device Information:");
            output.WriteLine($"Device Type: {deviceInfo.DeviceType}");
            output.WriteLine($"Operating System: {deviceInfo.Os}");
            output.WriteLine($"Browser: {deviceInfo.Browser}");
            output.WriteLine($"IP Address: {deviceInfo.IpAddress}");
        }
        else
        {
            WriteNoDataMessage(output, "device information");
        }
    }
} 