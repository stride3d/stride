using System;

namespace Stride.Updater.New;

internal sealed class UpdatableClassCast<TFrom, TTo>(UpdatableMember<TFrom> parent, UpdatableType<TTo> type) : UpdatableMember<TTo>
    where TFrom : class
    where TTo : class, TFrom
{
    public override bool SupportsByReference => false;

    public override TTo GetValue(object instance)
    {
        return parent.GetValue(instance) as TTo;
    }

    public override ref TTo GetReference(object instance)
    {
        throw new NotSupportedException();
    }

    public override void SetValue(object instance, TTo value)
    {
        parent.SetValue(instance, value);
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
    {
        return type.ResolveIndexer(name, this);
    }

    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
    {
        return type.ResolveProperty(name, this);
    }
}
