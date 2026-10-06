using System;
using Stride.Rendering;

namespace Stride.Updater.New;

internal sealed class ValueParameterKeyAccessor<T>(ValueParameterKey<T> parameterKey, UpdatableMember<ParameterCollection> parent, UpdatableType<T> type) : UpdatableMember<T> where T : struct
{
    public override bool SupportsByReference => false;

    public override T GetValue(object instance)
    {
        return parent.GetValue(instance)?.Get(parameterKey) ?? default;
    }

    public override ref T GetReference(object instance)
    {
        throw new NotSupportedException();
    }

    public override void SetValue(object instance, T value)
    {
        parent.GetValue(instance)?.Set(parameterKey, value);
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
