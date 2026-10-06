using System;

namespace Stride.Updater.New;

public abstract class UpdatableType
{
    internal abstract UpdatableMember CreateRootMember();
}
public abstract class UpdatableType<T> : UpdatableType
{
    public abstract UpdatableMember ResolveProperty(ReadOnlySpan<char> name, UpdatableMember<T> parent);
    public virtual UpdatableMember ResolveIndexer(ReadOnlySpan<char> name, UpdatableMember<T> parent)
    {
        throw new NotSupportedException($"Indexer '{name}' is not supported in {typeof(T).Name}.");
    }

    internal sealed override UpdatableMember CreateRootMember()
    {
        return new RootMember<T>(this);
    }
}
