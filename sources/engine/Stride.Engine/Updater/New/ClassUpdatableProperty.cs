namespace Stride.Updater.New;

public sealed unsafe class ClassUpdatableProperty<TParent, TProperty>(string name, delegate* managed<TParent, TProperty> getter, delegate* managed<TParent, TProperty, void> setter, UpdatableType<TProperty> propertyType) : UpdatableMember<TParent, TProperty>
    where TParent : class
{
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
