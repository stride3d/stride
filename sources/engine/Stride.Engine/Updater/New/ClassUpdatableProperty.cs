using System.Diagnostics;

namespace Stride.Updater.New;

public sealed unsafe class ClassUpdatableProperty<TParent, TProperty> : UpdatableMember<TParent, TProperty>
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

    protected override bool SupportsByReference => false;

    public override UpdatableMember<TProperty> CreateProperty(string name)
    {
        return propertyType.CreateProperty(name);
    }

    public override UpdatableMember<TProperty> CreateIndexer(string name)
    {
        return propertyType.CreateIndexer(name);
    }

    protected override TProperty GetValue(TParent parent) => getter(parent);
    protected override void SetValue(TParent parent, TProperty value) => setter(parent, value);
}
