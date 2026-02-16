using System.Reflection.Metadata;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;

public record struct ParameterizedFlag<T>(T Value, MemoryOwner<int> Parameters) : IDisposable
    where T : Enum
{
    public readonly Span<int> Span => Parameters.Span;
    public ParameterizedFlag(T value, ReadOnlySpan<int> parameters)
        : this(value, MemoryOwner<int>.Allocate(parameters.Length))
    {
        parameters.CopyTo(Parameters.Span);
    }
    public readonly void Dispose() => Parameters.Dispose();
    public readonly Span<int>.Enumerator GetEnumerator() => Parameters.Span.GetEnumerator();
    public static implicit operator ParameterizedFlag<T>(T value) => new(value, MemoryOwner<int>.Empty);

    public readonly override string ToString()
    {
        return $"{Value}";
    }
}