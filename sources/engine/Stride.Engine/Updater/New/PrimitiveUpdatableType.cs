using System;

namespace Stride.Updater.New;

public sealed class PrimitiveUpdatableType<T> : UpdatableType<T>
{
    public static PrimitiveUpdatableType<T> Instance { get; } = new();
    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name, UpdatableMember<T> parent)
    {
        throw new NotSupportedException("Primitive types do not have children.");
    }
    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name, UpdatableMember<T> parent)
    {
        throw new NotSupportedException("Primitive types do not have children.");
    }
}
