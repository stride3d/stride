using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core.Parsing;


public ref struct SpirvWriter
{
    public int Length { get; private set;}
    MemoryOwner<int> buffer;

    public Span<int> SpirvCode => buffer.Span[..(Length-1)];

    public SpirvWriter(int initialSize = 32)
    {
        buffer = MemoryOwner<int>.Allocate(initialSize, AllocationMode.Clear);
        Length = 0;
    }

    void Expand(int size)
    {
        var futureLength = Length + size;
        var realLength = buffer.Length;
        if(Length > buffer.Length)
        {
            buffer.Dispose();
            buffer = MemoryOwner<int>.Allocate(realLength*2, AllocationMode.Clear);
        }
    }

    public void Write(int word)
    {
        Expand(1);
        buffer.Span[Length] = word;
        Length += 1;
    }
    public void Write(scoped Span<int> words)
    {
        Expand(words.Length);
        words.CopyTo(buffer.Span[Length..]);
        Length += Length;
    }

    public void Dispose()
    {
        buffer.Dispose();
    }
}