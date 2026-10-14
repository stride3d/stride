using System.Diagnostics;

namespace Stride.Updater.New;

public sealed unsafe class ClassUpdatableProperty<TParent, TProperty> : UpdatableMember<TParent, TProperty>, IUpdatableMember<ClassUpdatableProperty<TParent, TProperty>, TParent, TProperty>
    where TParent : class
{
    private readonly string name;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly delegate*<TParent, TProperty> getter;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly delegate*<TParent, TProperty, void> setter;
    private readonly UpdatableType<TProperty> propertyType;

    public ClassUpdatableProperty(string name, delegate* managed<TParent, TProperty> getter, delegate* managed<TParent, TProperty, void> setter, UpdatableType<TProperty> propertyType)
    {
        this.name = name;
        this.getter = getter;
        this.setter = setter;
        this.propertyType = propertyType;
    }

    public override string Name => name;

    public static bool SupportsByReference => false;

    public override UpdatableMember<TProperty> CreateProperty(string name)
    {
        return propertyType.CreateProperty(name);
    }

    public override UpdatableMember<TProperty> CreateIndexer(string name)
    {
        return propertyType.CreateIndexer(name);
    }

    public static TProperty GetValue(ClassUpdatableProperty<TParent, TProperty> @this, TParent parent) => @this.getter(parent);
    public static void SetValue(ClassUpdatableProperty<TParent, TProperty> @this, TParent parent, TProperty value) => @this.setter(parent, value);

    public override void Update(TParent parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override void Update(ref TParent parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
