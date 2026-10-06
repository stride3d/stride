using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public sealed unsafe class ClassUpdatableField<TParent, TField>(UpdatableMember<TParent> parent, delegate* managed<TParent, ref TField> fieldAccessor, UpdatableType<TField> fieldType) : UpdatableMember<TField>
    where TParent : class
{
    public override bool SupportsByReference => true;

    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
    {
        return fieldType.ResolveProperty(name, this);
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
    {
        return fieldType.ResolveIndexer(name, this);
    }

    public override ref TField GetReference(object instance)
    {
        var parentValue = parent.GetValue(instance);
        if (parentValue is null)
        {
            return ref Unsafe.NullRef<TField>();
        }
        else
        {
            return ref fieldAccessor(parentValue);
        }
    }
}
