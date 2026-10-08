using System.Diagnostics;

namespace Stride.Updater.New;

public sealed unsafe class StructUpdatableProperty<TParent, TProperty> : UpdatableMember<TParent, TProperty>
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

    protected override bool SupportsByReference => false;

    public override UpdatableMember<TProperty> CreateIndexer(string name)
    {
        return propertyType.CreateIndexer(name);
    }

    public override UpdatableMember<TProperty> CreateProperty(string name)
    {
        return propertyType.CreateProperty(name);
    }

    protected override TProperty GetValue(ref TParent parent)
    {
        return getter(ref parent);
    }

    protected override void SetValue(ref TParent parent, TProperty value)
    {
        setter(ref parent, value);
    }
}
