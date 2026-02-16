using Stride.Shaders.Core;

namespace Stride.Shaders.Parsing.Analysis;

public interface IStreamChecker
{
    public void CheckIO(SymbolTable table, EntryPoint? entryPoint = null);
}