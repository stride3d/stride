using System.Diagnostics;

namespace Stride.Updater.New;

public sealed unsafe class ClassUpdatableField<TParent, TField> : UpdatableMember<TParent, TField>, IUpdatableMember<ClassUpdatableField<TParent, TField>, TParent, TField>
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

    public static bool SupportsByReference => true;

    public override string Name => name;

    public override UpdatableMember<TField> CreateProperty(string name)
    {
        return fieldType.CreateProperty(name);
    }

    public override UpdatableMember<TField> CreateIndexer(string name)
    {
        return fieldType.CreateIndexer(name);
    }

    public static TField GetValue(ClassUpdatableField<TParent, TField> @this, TParent parent)
    {
        return @this.fieldAccessor(parent);
    }

    public static ref TField GetReference(ClassUpdatableField<TParent, TField> @this, TParent parent)
    {
        return ref @this.fieldAccessor(parent);
    }

    public static void SetValue(ClassUpdatableField<TParent, TField> @this, TParent parent, TField value)
    {
        @this.fieldAccessor(parent) = value;
    }

    public override void Update(TParent parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override void Update(ref TParent parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
