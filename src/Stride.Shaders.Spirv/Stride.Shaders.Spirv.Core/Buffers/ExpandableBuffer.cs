using System.Numerics;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A buffer that works similarly to List<T> 
/// </summary>
/// <typeparam name="T"></typeparam>
public class ExpandableBuffer<T> : BufferBase<T>
    where T : struct
{

    public ExpandableBuffer()
    {
        _owner = MemoryOwner<T>.Allocate(4, AllocationMode.Clear);
        Length = 0;
    }

    public ExpandableBuffer(int initialCapacity)
    {
        _owner = MemoryOwner<T>.Allocate(initialCapacity, AllocationMode.Clear);
        Length = 0;
    }
    /// <summary>
    /// Expands the buffer by the size demanded. It allocates a new underlying array when needed.
    /// </summary>
    /// <param name="size"></param>
    private void Expand(int size)
    {
        if(Length + size > _owner.Length)
        {
            var n = MemoryOwner<T>.Allocate((int)BitOperations.RoundUpToPowerOf2((uint)(Length + size)), AllocationMode.Clear);
            _owner.Span.CopyTo(n.Span);
            var toDispose = _owner;
            _owner = n;
            toDispose.Dispose();
        }
    }
    /// <summary>
    /// Adds an element to the buffer
    /// </summary>
    /// <param name="item"></param>
    public void Add(T item)
    {
        Expand(1);
        _owner.Span[Length] = item;
        Length += 1;
    }
    /// <summary>
    /// Adds many elements to the buffer
    /// </summary>
    /// <param name="items"></param>
    public void Add(Span<T> items)
    {
        Expand(items.Length);
        items.CopyTo(_owner.Span[Length..]);
        Length += items.Length;
    }
    /// <summary>
    /// Inserts many elements at a specific place in the buffer
    /// </summary>
    /// <param name="start"></param>
    /// <param name="words"></param>
    public void Insert(int start, Span<T> words)
    {
        Expand(words.Length);
        var slice = _owner.Span[start..Length];
        slice.CopyTo(_owner.Span[(start + words.Length)..]);
        words.CopyTo(_owner.Span.Slice(start, words.Length));
        Length += words.Length;
    }
    
    /// <summary>
    /// Remove an element from the buffer
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool RemoveAt(int index)
    {
        if(index < Length && index > 0)
        {
            Span[(index+1)..].CopyTo(Span[index..]);
            Length -= 1;
            return true;
        }
        return false;
    }
}