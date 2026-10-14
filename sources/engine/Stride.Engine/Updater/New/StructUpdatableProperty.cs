using System.Diagnostics;

namespace Stride.Updater.New;

public sealed unsafe class StructUpdatableProperty<TParent, TProperty> : UpdatableMember<TParent, TProperty>, IUpdatableMember<StructUpdatableProperty<TParent, TProperty>, TParent, TProperty>
    where TParent : struct
{
    private readonly string name;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly delegate*<ref TParent, TProperty> getter;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly delegate*<ref TParent, TProperty, void> setter;
    private readonly UpdatableType<TProperty> propertyType;

    public StructUpdatableProperty(string name, delegate*<ref TParent, TProperty> getter, delegate*<ref TParent, TProperty, void> setter, UpdatableType<TProperty> propertyType)
    {
        this.name = name;
        this.getter = getter;
        this.setter = setter;
        this.propertyType = propertyType;
    }

    public override string Name => name;

    public static bool SupportsByReference => false;

    public override UpdatableMember<TProperty> CreateIndexer(string name)
    {
        return propertyType.CreateIndexer(name);
    }

    public override UpdatableMember<TProperty> CreateProperty(string name)
    {
        return propertyType.CreateProperty(name);
    }

    public static TProperty GetValue(StructUpdatableProperty<TParent, TProperty> @this, ref TParent parent)
    {
        return @this.getter(ref parent);
    }

    public static void SetValue(StructUpdatableProperty<TParent, TProperty> @this, ref TParent parent, TProperty value)
    {
        @this.setter(ref parent, value);
    }

    public override void Update(TParent parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override void Update(ref TParent parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
