using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

internal sealed class ArrayOffsetAccessor<TElement, TData>(int index, int offset, UpdatableType<TData> dataType) : UpdatableMember<TElement[], TData>
{
    protected override bool SupportsByReference => true;

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

    protected override TData GetValue(TElement[] parent)
    {
        if (parent.Length > index)
        {
            return GetReferenceInternal(parent);
        }
        else
        {
            return default;
        }
    }

    protected override ref TData GetReference(TElement[] parent)
    {
        if (parent.Length > index)
        {
            return ref GetReferenceInternal(parent);
        }
        else
        {
            return ref Unsafe.NullRef<TData>();
        }
    }

    protected override void SetValue(TElement[] parent, TData value)
    {
        if (parent.Length > index)
        {
            GetReferenceInternal(parent) = value;
        }
    }

    private ref TData GetReferenceInternal(TElement[] parent)
    {
        ref TElement elementRef = ref parent[index];
        return ref Unsafe.As<byte, TData>(ref Unsafe.AddByteOffset(ref Unsafe.As<TElement, byte>(ref elementRef), offset));
    }

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
