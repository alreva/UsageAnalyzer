using System;
using System.Text.Json;
using System.IO;
using Dto;

namespace Processors
{
    public abstract class BaseProcessor
    {
        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected UserEventDto? DeserializeUserEvent(string jsonInput)
        {
            try
            {
                return JsonSerializer.Deserialize<UserEventDto>(jsonInput, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Failed to deserialize JSON input: {ex.Message}", ex);
            }
        }

        protected void WriteNoDataMessage(TextWriter output, string dataType)
        {
            output.WriteLine($"No {dataType} found.");
        }
    }
} 