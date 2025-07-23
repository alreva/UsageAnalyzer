namespace Processors;

public interface IProcessor
{
  void Process(string jsonInput, TextWriter output);
}
