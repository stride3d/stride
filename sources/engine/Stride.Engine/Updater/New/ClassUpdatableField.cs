using System.Diagnostics;

namespace Stride.Updater.New;

public sealed unsafe class ClassUpdatableField<TParent, TField> : UpdatableMember<TParent, TField>
    where TParent : class
{
    private readonly string name;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly delegate*<TParent, ref TField> fieldAccessor;
    private readonly UpdatableType<TField> fieldType;

    public ClassUpdatableField(string name, delegate* managed<TParent, ref TField> fieldAccessor, UpdatableType<TField> fieldType)
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

    protected override TField GetValue(TParent parent)
    {
        return fieldAccessor(parent);
    }

    protected override ref TField GetReference(TParent parent)
    {
        return ref fieldAccessor(parent);
    }

    protected override void SetValue(TParent parent, TField value)
    {
        fieldAccessor(parent) = value;
    }
}
