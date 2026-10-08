using System.Diagnostics;

namespace Stride.Updater.New;

public sealed unsafe class StructUpdatableField<TParent, TField> : UpdatableMember<TParent, TField>
    where TParent : struct
{
    private readonly string name;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly delegate*<ref TParent, ref TField> fieldAccessor;
    private readonly UpdatableType<TField> fieldType;

    public StructUpdatableField(string name, delegate*<ref TParent, ref TField> fieldAccessor, UpdatableType<TField> fieldType)
    {
        this.name = name;
        this.fieldAccessor = fieldAccessor;
        this.fieldType = fieldType;
    }

    protected override bool SupportsByReference => true;

    public override string Name => name;

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
        return fieldAccessor(ref parent);
    }

    protected override ref TField GetReference(ref TParent parent)
    {
        return ref fieldAccessor(ref parent);
    }

    protected override void SetValue(ref TParent parent, TField value)
    {
        fieldAccessor(ref parent) = value;
    }
}
