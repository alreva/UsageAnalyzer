namespace Processors;

using System.Text.Json;

public abstract class BaseProcessor<TDto> : IProcessor
{
  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
    IncludeFields = true,
  };

  /// <inheritdoc/>
  public abstract void Process(string jsonInput, TextWriter output);

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
}
