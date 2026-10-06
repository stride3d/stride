using System;

namespace Stride.Updater.New;

internal sealed class RootMember<T>(UpdatableType<T> type) : UpdatableMember<T>
{
    public sealed override T GetValue(object instance)
    {
        return (T)instance;
    }
    public sealed override ref T GetReference(object instance)
    {
        throw new NotSupportedException("Getting the reference to a root member is not supported.");
    }
    public sealed override void SetValue(object instance, T value)
    {
        throw new NotSupportedException("Setting the value of a root member is not supported.");
    }
    public sealed override bool SupportsByReference => false;
    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name) => type.ResolveProperty(name, this);
    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name) => type.ResolveIndexer(name, this);
}
