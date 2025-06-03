using System;
using System.Text.Json;
using System.IO;

namespace Processors
{
    public abstract class BaseProcessor<TDto> : IProcessor<TDto>
    {
        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected TDto? Deserialize(string jsonInput)
        {
            try
            {
                return JsonSerializer.Deserialize<TDto>(jsonInput, JsonOptions);
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

        public abstract void Process(string jsonInput, TextWriter output);
    }
} 