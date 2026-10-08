using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public sealed unsafe class StructUpdatableField<TParent, TField> : UpdatableMember<TParent, TField>
{
    private readonly string name;
    private readonly UpdatableType<TField> fieldType;

    public StructUpdatableField(string name, delegate*<ref TParent, ref TField> fieldAccessor, UpdatableType<TField> fieldType)
    {
        this.name = name;
        this.fieldType = fieldType;
        {

            TParent parentValue = default;
            ref TParent parentRef = ref parentValue;
            ref TField fieldRef = ref fieldAccessor(ref parentRef);
            FieldOffset = (int)Unsafe.ByteOffset(ref Unsafe.As<TParent, byte>(ref parentRef), ref Unsafe.As<TField, byte>(ref fieldRef));
        }
    }

    public StructUpdatableField(int fieldOffset, UpdatableType<TField> fieldType)
    {
        name = $"Offset_{fieldOffset}";
        this.fieldType = fieldType;
        FieldOffset = fieldOffset;
    }

    protected override bool SupportsByReference => true;

    public override string Name => name;

    internal int FieldOffset { get; }

    internal static int Size => Unsafe.SizeOf<TField>();

    public override UpdatableMember<TField> CreateProperty(string name)
    {
        return fieldType.CreateProperty(name);
    }

    public override UpdatableMember<TField> CreateIndexer(string name)
    {
        return fieldType.CreateIndexer(name);
    }

    protected override TField GetValue(ref TParent parent)
    {
        return GetReference(ref parent);
    }

    protected override ref TField GetReference(ref TParent parent)
    {
        return ref Unsafe.As<byte, TField>(ref Unsafe.AddByteOffset(ref Unsafe.As<TParent, byte>(ref parent), FieldOffset));
    }

    protected override void SetValue(ref TParent parent, TField value)
    {
        GetReference(ref parent) = value;
    }

    internal override bool TryReduce(out UpdatableMember<TParent> reducedMember)
    {
        if (IsBlittable)
        {
            if (IsLeaf)
            {
                reducedMember = new BlittableStructData<TParent>(FieldOffset, (uint)Size);
                reducedMember.MakeLeaf(DataOffset);
                return true;
            }
            else if (Children.Count is 1 && Children[0] is BlittableStructData<TField> child)
            {
                Debug.Assert(child.IsLeaf);
                reducedMember = new BlittableStructData<TParent>(FieldOffset + child.Offset, child.Size);
                reducedMember.MakeLeaf(child.DataOffset);
                return true;
            }
        }

        return base.TryReduce(out reducedMember);
    }

    internal override bool TryMergeWithParent<TGrandParent>(UpdatableMember<TGrandParent> parent, out UpdatableMember<TGrandParent> merged)
    {
        if (parent is StructUpdatableField<TGrandParent, TParent> parentField)
        {
            merged = new StructUpdatableField<TGrandParent, TField>(parentField.FieldOffset + FieldOffset, fieldType);
            if (IsLeaf)
            {
                merged.MakeLeaf(DataOffset);
            }
            return true;
        }
        if (parent is ArrayIndexAccessor<TParent> parentArrayIndexAccessor)
        {
            merged = (UpdatableMember<TGrandParent>)(object)new ArrayOffsetAccessor<TParent, TField>(parentArrayIndexAccessor.Index, FieldOffset, fieldType);
            if (IsLeaf)
            {
                merged.MakeLeaf(DataOffset);
            }
            return true;
        }
        return base.TryMergeWithParent(parent, out merged);
    }
}
