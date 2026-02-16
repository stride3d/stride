using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Parsing.SDSL.PreProcessing;


public static class MemoryOwnerExtensions
{
    public static MemoryOwner<T> Resize<T>(this MemoryOwner<T> owner, int size)
    {
        var result = MemoryOwner<T>.Allocate(owner.Length + size, AllocationMode.Clear);
        owner.Span[..Math.Min(result.Length, owner.Length)].CopyTo(result.Span);
        owner.Dispose();
        return result;
    }

    public static MemoryOwner<T> Add<T>(this MemoryOwner<T> owner, ReadOnlySpan<T> span)
    {
        var result = owner.Resize(span.Length);
        span.CopyTo(result.Span[^span.Length..]);
        return result;
    }
    public static MemoryOwner<T> Add<T>(this MemoryOwner<T> owner, Span<T> span)
    {
        var result = owner.Resize(span.Length);
        span.CopyTo(result.Span[^span.Length..]);
        return result;
    }
    public static MemoryOwner<T> Add<T>(this MemoryOwner<T> owner, Memory<T> other, Range range)
    {
        return owner.Add(other.Span[range]);
    }
}