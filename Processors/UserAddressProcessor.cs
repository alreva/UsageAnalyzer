using System;
using System.IO;
using Dto;

namespace Processors
{
    public class UserAddressProcessor : BaseProcessor
    {
        public void ProcessUserAddress(string jsonInput, TextWriter output)
        {
            var userEventDto = DeserializeUserEvent(jsonInput);
            if (userEventDto?.User?.Address != null)
            {
                var address = userEventDto.User.Address;
                output.WriteLine($"User Address:");
                output.WriteLine($"Street: {address.Street}");
                output.WriteLine($"City: {address.City}");
                output.WriteLine($"State: {address.State}");
                output.WriteLine($"Zip Code: {address.ZipCode}");
                output.WriteLine($"Country: {address.Country}");
            }
            else
            {
                WriteNoDataMessage(output, "user address");
            }
        }
    }
} 