using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public sealed unsafe class StructUpdatableProperty<TParent, TProperty>(UpdatableMember<TParent> parent, delegate*<ref TParent, TProperty> getter, delegate*<ref TParent, TProperty, void> setter, UpdatableType<TProperty> propertyType) : UpdatableMember<TProperty>
    where TParent : struct
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
        if (parent.SupportsByReference)
        {
            ref TParent reference = ref parent.GetReference(instance);
            return Unsafe.IsNullRef(ref reference) ? default : getter(ref reference);
        }
        else
        {
            var temp = parent.GetValue(instance);
            return getter(ref temp);
        }
    }
    public override ref TProperty GetReference(object instance)
    {
        throw new NotSupportedException("Properties do not support by-reference access.");
    }
    public override void SetValue(object instance, TProperty value)
    {
        if (parent.SupportsByReference)
        {
            ref TParent reference = ref parent.GetReference(instance);
            if (!Unsafe.IsNullRef(ref reference))
            {
                setter(ref reference, value);
            }
        }
        else
        {
            var temp = parent.GetValue(instance);
            setter(ref temp, value);
            parent.SetValue(instance, temp);
        }
    }
}
