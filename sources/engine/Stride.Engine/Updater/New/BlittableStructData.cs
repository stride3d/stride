using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

internal sealed class BlittableStructData<TParent>(int offset, uint size) : UpdatableMember<TParent>
{
    public override string Name => nameof(BlittableStructData<>);

    internal override Type MemberType => typeof(void);

    internal override bool IsBlittable => true;

    public int Offset => offset;

    public uint Size => size;

    public override UpdatableMember GetOrCreateIndexer(string name)
    {
        throw new NotSupportedException();
    }

    public override UpdatableMember GetOrCreateProperty(string name)
    {
        throw new NotSupportedException();
    }

    public override unsafe void Update(TParent parent, byte* data, UpdateObjectData[] updateObjects)
    {
        throw new NotSupportedException();
    }

    public override unsafe void Update(ref TParent parent, byte* data, UpdateObjectData[] updateObjects)
    {
        int* conditionPtr = (int*)(data + DataOffset);
        if (*conditionPtr != 0)
        {
            ref byte destination = ref Unsafe.AddByteOffset(ref Unsafe.As<TParent, byte>(ref parent), Offset);
            ref byte source = ref Unsafe.AsRef<byte>(data + DataOffset + sizeof(int));
            Unsafe.CopyBlock(ref destination, ref source, Size);
        }
    }

    internal override bool TryMergeWithParent<TGrandParent>(UpdatableMember<TGrandParent> parent, out UpdatableMember<TGrandParent> merged)
    {
        if (parent is StructUpdatableField<TGrandParent, TParent> parentField)
        {
            merged = new BlittableStructData<TGrandParent>(parentField.FieldOffset + Offset, Size);
        }
        else if (parent is ArrayIndexAccessor<TParent> parentArrayIndexAccessor)
        {
            merged = (UpdatableMember<TGrandParent>)(object)new BlittableArrayData<TParent>(parentArrayIndexAccessor.Index, Offset, Size);
        }
        else
        {
            return base.TryMergeWithParent(parent, out merged);
        }

        merged.MakeLeaf(DataOffset);
        return true;
    }
}
