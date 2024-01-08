using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace SDSL.TAC;

public sealed partial class IR
{
    MemoryOwner<Quadruple> _values;

    public Span<Quadruple> Span => _values.Span[..Count];
    public Memory<Quadruple> Memory => _values.Memory[..Count];

    public ref Quadruple this[int index] { get => ref Span[index]; }

    public int Count { get; private set; }

    public IR()
    {
        _values = MemoryOwner<Quadruple>.Allocate(4, AllocationMode.Clear);
    }

    void Expand(int size)
    {
        int nsize = Count + size;
        if(nsize > _values.Length)
        {
            var tmp = MemoryOwner<Quadruple>.Allocate(
                (int)BitOperations.RoundUpToPowerOf2((uint)nsize),
                AllocationMode.Clear
            );
            Span.CopyTo(tmp.Span);
            _values.Dispose();
            _values = tmp;

        }
    }

    public Span<Quadruple>.Enumerator GetEnumerator() => Span.GetEnumerator();

    public void Add(Quadruple item)
    {
        Expand(1);
        Count += 1;
        Span[Count - 1] = item;
    }
    public void Clear()
    {
        Span.Clear();
        Count = 0;
    }
    public bool Contains(Quadruple item) => Span.Contains(item);
    public int IndexOf(Quadruple item) => Span.IndexOf(item);
    public void Insert(int index, Quadruple item) 
    {
        Expand(1);
        var data = Span[index..];
        Count += 1;
        data.CopyTo(Span[(index+1)..]);
        data[index] = item;
    }
}