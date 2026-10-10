using System;

namespace Stride.Updater.New;

public sealed class PrimitiveUpdatableType<T> : UpdatableType<T>
{
    public static PrimitiveUpdatableType<T> Instance { get; } = new();
    public override UpdatableMember<T> CreateProperty(string name)
    {
        throw new NotSupportedException("Primitive types do not have children.");
    }
    public override UpdatableMember<T> CreateIndexer(string name)
    {
        throw new NotSupportedException("Primitive types do not have children.");
    }
}
