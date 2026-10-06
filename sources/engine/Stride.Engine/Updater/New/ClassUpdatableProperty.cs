using System;

namespace Stride.Updater.New;

public sealed unsafe class ClassUpdatableProperty<TParent, TProperty>(UpdatableMember<TParent> parent, delegate* managed<TParent, TProperty> getter, delegate* managed<TParent, TProperty, void> setter, UpdatableType<TProperty> propertyType) : UpdatableMember<TProperty>
    where TParent : class
{
    public override bool SupportsByReference => false;
    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
    {
        return propertyType.ResolveProperty(name, this);
    }
    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
    {
        return propertyType.ResolveIndexer(name, this);
    }
    public override TProperty GetValue(object instance)
    {
        var parentValue = parent.GetValue(instance);
        return parentValue is null ? default : getter(parentValue);
    }
    public override ref TProperty GetReference(object instance)
    {
        throw new NotSupportedException("Properties do not support by-reference access.");
    }
    public override void SetValue(object instance, TProperty value)
    {
        var parentValue = parent.GetValue(instance);
        if (parentValue is not null)
        {
            setter(parentValue, value);
        }
    }
}
