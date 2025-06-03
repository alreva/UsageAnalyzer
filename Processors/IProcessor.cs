using System.IO;

namespace Processors
{
    public interface IProcessor<TDto>
    {
        void Process(string jsonInput, TextWriter output);
    }
} 