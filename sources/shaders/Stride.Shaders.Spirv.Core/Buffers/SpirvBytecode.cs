using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core.Buffers;

public record SpirvBytecode(SpirvHeader Header, SpirvBuffer Buffer) : IDisposable
{
    public SpirvBytecode(SpirvBuffer buffer) : this(CreateHeader(buffer), buffer)
    {
    }

    public void Dispose() => Buffer.Dispose();

    public static SpirvHeader CreateHeader(SpirvBuffer buffer)
    {
        var header = new SpirvHeader("1.4", 0, 1);
        var bound = 1;
        foreach (var i in buffer)
        {
            ref var data = ref i.Data;
            if (data.IdResult is int index && index >= bound)
                bound = index + 1;
        }
        return new SpirvHeader("1.4", 0, bound);
    }

    public Span<byte> ToBytecode()
    {
        return CreateBytecodeFromBuffers(Header, false, Buffer);
    }

    public static SpirvBytecode CreateBufferFromBytecode(Span<byte> span)
    {
        return CreateBufferFromBytecode(MemoryMarshal.Cast<byte, int>(span));
    }

    public static SpirvBytecode CreateBufferFromBytecode(Span<int> span)
    {
        if (span[0] != Specification.MagicNumber)
            throw new InvalidOperationException("SPIRV Magic number not found");
        
        var header = SpirvHeader.Read(span);

        return new(header, new SpirvBuffer(span[SpirvHeader.IntSpanSize..]));
    }

    public static SpanOwner<int> CreateSpanFromBuffers(SpirvHeader header, bool computeBounds, params Span<SpirvBuffer> buffers)
    {
        int instructionsMemorySize = 0;
        var bound = header.Bound;
        foreach (var buffer in buffers)
        {
            foreach (var i in buffer)
            {
                ref var data = ref i.Data;
                if (data.IdResult is int index && index >= bound)
                    bound = index + 1;

                instructionsMemorySize += data.Memory.Length;
            }
        }

        header = header with { Bound = bound };

        var result = SpanOwner<int>.Allocate(5 + instructionsMemorySize);
        var span = result.Span;
        header.WriteTo(span);
        var offset = 5;
        foreach (var buffer in buffers)
        {
            foreach (var i in buffer)
            {
                i.Data.Memory.Span.CopyTo(span[offset..]);
                offset += i.Data.Memory.Length;
            }
        }
        return result;
    }

    public static Span<byte> CreateBytecodeFromBuffers(params Span<SpirvBuffer> buffers)
    {
        return CreateBytecodeFromBuffers(new("1.4", 0, 1), true, buffers);
    }

    public static Span<byte> CreateBytecodeFromBuffers(SpirvHeader header, bool computeBounds, params Span<SpirvBuffer> buffers)
    {
        return MemoryMarshal.AsBytes(CreateSpanFromBuffers(header, computeBounds, buffers).Span);
    }
}