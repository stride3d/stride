using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace SDSL.TAC;

public sealed partial class IR()
{
    readonly List<Quadruple> values = [];

    public Span<Quadruple> Span => CollectionsMarshal.AsSpan(values);

    public ref Quadruple this[int index] { get => ref Span[index];}

    public int Count => values.Count;

    public List<Quadruple>.Enumerator GetEnumerator() => values.GetEnumerator(); 

    public void Add(Quadruple item) => values.Add(item);
    public void Clear() => values.Clear();
    public bool Contains(Quadruple item) => values.Contains(item);
    public int IndexOf(Quadruple item) => values.IndexOf(item);
    public void Insert(int index, Quadruple item) => values.Insert(index,item);
    public bool Remove(Quadruple item) => values.Remove(item);
    public void RemoveAt(int index) => values.RemoveAt(index);

}