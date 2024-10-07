using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv;

/// <summary>
/// A mixin that can be composed with others
/// </summary>
/// <param name="Name"></param>
/// <param name="Buffer"></param>
public record Composable(string Name, SortedWordBuffer Buffer)
{
    public InstructionEnumerator GetEnumerator() => new(Buffer); 
}