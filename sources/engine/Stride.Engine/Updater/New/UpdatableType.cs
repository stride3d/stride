using System;

namespace Stride.Updater.New;

public abstract class UpdatableType
{
    internal abstract UpdatableMember<object> CreateRootMember();
}
public abstract class UpdatableType<T> : UpdatableType
{
    public abstract UpdatableMember<T> CreateProperty(string name);
    public virtual UpdatableMember<T> CreateIndexer(string name)
    {
        throw new NotSupportedException($"Indexer '{name}' is not supported in {typeof(T).Name}.");
    }

    internal sealed override UpdatableMember<object> CreateRootMember()
    {
        return new RootMember<T>(this);
    }
}
