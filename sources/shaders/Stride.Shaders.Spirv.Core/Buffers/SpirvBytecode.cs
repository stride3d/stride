using System.Runtime.InteropServices;
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

    public Span<byte> ToSpan()
    {
        return CreateBytecodeFromBuffers(Header, false, Buffer);
    }

    public static SpirvBytecode CreateFromSpan(Span<byte> span)
    {
        return CreateFromSpan(MemoryMarshal.Cast<byte, int>(span));
    }

    public static SpirvBytecode CreateFromSpan(Span<int> span)
    {
        if (span[0] != Specification.MagicNumber)
            throw new InvalidOperationException("SPIRV Magic number not found");

        var header = SpirvHeader.Read(span);

        return new(header, new SpirvBuffer(span[SpirvHeader.IntSpanSize..]));
    }

    // Returns a freshly allocated array. Do not back this with ArrayPool/SpanOwner: the caller
    // receives a Span that carries no ownership, so a pooled buffer would be returned to the pool
    // by the finalizer and reused while the span is still in use, silently corrupting the bytecode
    // (e.g. another shader's OpCapability bleeding into vkCreateShaderModule input).
    public static Span<int> CreateSpanFromBuffers(SpirvHeader header, bool computeBounds, params Span<SpirvBuffer> buffers)
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

        var result = new int[5 + instructionsMemorySize];
        var span = result.AsSpan();
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
        return span;
    }

    public static Span<byte> CreateBytecodeFromBuffers(params Span<SpirvBuffer> buffers)
    {
        return CreateBytecodeFromBuffers(new("1.4", 0, 1), true, buffers);
    }

    public static Span<byte> CreateBytecodeFromBuffers(SpirvHeader header, bool computeBounds, params Span<SpirvBuffer> buffers)
    {
        return MemoryMarshal.AsBytes(CreateSpanFromBuffers(header, computeBounds, buffers));
    }
}
