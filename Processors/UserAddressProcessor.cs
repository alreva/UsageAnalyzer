// <copyright file="UserAddressProcessor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors;

using Dto;

public class UserAddressProcessor : BaseProcessor<UserEventDto>
{
  public override void Process(string jsonInput, TextWriter output)
  {
    var userEventDto = this.Deserialize(jsonInput);
    if (userEventDto?.User?.Address != null)
    {
      var address = userEventDto.User.Address;
      output.WriteLine("User Address:");
      output.WriteLine($"Street: {address.Street}");
      output.WriteLine($"City: {address.City}");
      output.WriteLine($"State: {address.State}");
      output.WriteLine($"Zip Code: {address.ZipCode}");
      output.WriteLine($"Zip Code: {address.ZipCode}");
      output.WriteLine($"Country: {address.Country}");
    }
    else
    {
      this.WriteNoDataMessage(output, "user address");
    }
  }
}