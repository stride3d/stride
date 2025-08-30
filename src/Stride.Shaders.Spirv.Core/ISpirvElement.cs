using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public interface ISpirvElement : IDisposable
{
    MemoryOwner<int> Words { get; }
    public int WordCount { get; }
}