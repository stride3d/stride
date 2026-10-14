using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

internal sealed class ArrayOffsetAccessor<TElement, TData> : UpdatableMember<TElement[], TData>, IUpdatableMember<ArrayOffsetAccessor<TElement, TData>, TElement[], TData>
{
    private readonly int index;
    private readonly int offset;
    private readonly UpdatableType<TData> dataType;

    public ArrayOffsetAccessor(int index, int offset, UpdatableType<TData> dataType)
    {
        this.index = index;
        this.offset = offset;
        this.dataType = dataType;
    }

    public static bool SupportsByReference => true;

    internal override bool IsIndexer => true;

    public override string Name => $"Index_{index}_Offset_{offset}";

    public override UpdatableMember<TData> CreateProperty(string name)
    {
        return dataType.CreateProperty(name);
    }

    public override UpdatableMember<TData> CreateIndexer(string name)
    {
        return dataType.CreateIndexer(name);
    }

    public static TData GetValue(ArrayOffsetAccessor<TElement, TData> @this, TElement[] parent)
    {
        if (parent.Length > @this.index)
        {
            return GetReferenceInternal(parent, @this.index, @this.offset);
        }
        else
        {
            return default;
        }
    }

    public static ref TData GetReference(ArrayOffsetAccessor<TElement, TData> @this, TElement[] parent)
    {
        if (parent.Length > @this.index)
        {
            return ref GetReferenceInternal(parent, @this.index, @this.offset);
        }
        else
        {
            return ref Unsafe.NullRef<TData>();
        }
    }

    public static void SetValue(ArrayOffsetAccessor<TElement, TData> @this, TElement[] parent, TData value)
    {
        if (parent.Length > @this.index)
        {
            GetReferenceInternal(parent, @this.index, @this.offset) = value;
        }
    }

    private static ref TData GetReferenceInternal(TElement[] parent, int index, int offset)
    {
        ref TElement elementRef = ref parent[index];
        return ref Unsafe.As<byte, TData>(ref Unsafe.AddByteOffset(ref Unsafe.As<TElement, byte>(ref elementRef), offset));
    }

    public override unsafe void Update(TElement[] parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref TElement[] parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);

    internal override bool TryReduce(out UpdatableMember<TElement[]> reducedMember)
    {
        if (IsBlittable && IsLeaf)
        {
            reducedMember = new BlittableArrayData<TElement>(index, offset, (uint)Unsafe.SizeOf<TData>());
            return true;
        }
        return base.TryReduce(out reducedMember);
    }
}
