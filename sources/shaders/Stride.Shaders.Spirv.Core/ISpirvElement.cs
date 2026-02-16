using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public interface ISpirvElement : IDisposable
{
    ReadOnlySpan<int> Words { get; }
    public int WordCount { get; }
}