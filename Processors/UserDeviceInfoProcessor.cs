// <copyright file="UserDeviceInfoProcessor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors;

using Dto;

public class UserDeviceInfoProcessor : BaseProcessor<UserEventDto>
{
  /// <inheritdoc/>
  public override void Process(string jsonInput, TextWriter output)
  {
    var userEventDto = this.Deserialize(jsonInput);
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
      this.WriteNoDataMessage(output, "device information");
    }
  }
}