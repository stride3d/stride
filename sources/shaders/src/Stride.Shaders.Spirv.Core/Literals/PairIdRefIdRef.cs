using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public struct PairIdRefIdRef : ISpirvElement, IFromSpirv<PairIdRefIdRef>
{
    public readonly int Item1{ get => Memory.Span[0]; set => Memory.Span[0] = value; }
    public readonly int Item2{ get => Memory.Span[1]; set => Memory.Span[1] = value; }   
    public readonly int WordCount => 2;
    MemoryOwner<int> Memory { get; set; }
    public readonly ReadOnlySpan<int> Words => Memory.Span;

    public PairIdRefIdRef()
    {
        Memory = MemoryOwner<int>.Allocate(2, AllocationMode.Clear);
    }
    public PairIdRefIdRef((int, int) value)
    {
        Memory = MemoryOwner<int>.Allocate(2);
        Memory.Span[0] = value.Item1;
        Memory.Span[1] = value.Item2;
    }

    public static implicit operator (int, int)(PairIdRefIdRef r) => (r.Item1, r.Item2);
    public static implicit operator PairIdRefIdRef((int, int) v) => new(v);
    public static implicit operator LiteralInteger(PairIdRefIdRef v) => new((ulong)(v.Item1 << 16 | v.Item2));
    public static PairIdRefIdRef From(Span<int> words) => new() { Item1 = words[0], Item2 = words[1] };

    public static PairIdRefIdRef From(string value)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}