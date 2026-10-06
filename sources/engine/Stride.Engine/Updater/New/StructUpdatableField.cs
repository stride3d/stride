using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public sealed unsafe class StructUpdatableField<TParent, TField>(UpdatableMember<TParent> parent, delegate*<ref TParent, ref TField> fieldAccessor, UpdatableType<TField> fieldType) : UpdatableMember<TField>
    where TParent : struct
{
    public override bool SupportsByReference => parent.SupportsByReference;

    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
    {
        return fieldType.ResolveProperty(name, this);
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
    {
        return fieldType.ResolveIndexer(name, this);
    }

    public override TField GetValue(object instance)
    {
        if (SupportsByReference)
        {
            ref TParent reference = ref parent.GetReference(instance);
            return Unsafe.IsNullRef(ref reference) ? default : fieldAccessor(ref reference);
        }
        else
        {
            var temp = parent.GetValue(instance);
            return fieldAccessor(ref temp);
        }
    }

    public override ref TField GetReference(object instance)
    {
        ref TParent reference = ref parent.GetReference(instance);
        return ref Unsafe.IsNullRef(ref reference) ? ref Unsafe.NullRef<TField>() : ref fieldAccessor(ref reference);
    }

    public override void SetValue(object instance, TField value)
    {
        if (SupportsByReference)
        {
            ref TParent reference = ref parent.GetReference(instance);
            if (!Unsafe.IsNullRef(ref reference))
            {
                fieldAccessor(ref reference) = value;
            }
        }
        else
        {
            var temp = parent.GetValue(instance);
            fieldAccessor(ref temp) = value;
            parent.SetValue(instance, temp);
        }
    }
}
