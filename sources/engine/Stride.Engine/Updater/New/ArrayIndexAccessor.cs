using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public sealed class ArrayIndexAccessor<T>(UpdatableMember<T[]> parent, int index, UpdatableType<T> elementType) : UpdatableMember<T>
{
    public override bool SupportsByReference => true;
    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
    {
        return elementType.ResolveProperty(name, this);
    }
    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
    {
        return elementType.ResolveIndexer(name, this);
    }
    public override T GetValue(object instance)
    {
        var parentValue = parent.GetValue(instance);
        if (parentValue is not null && parentValue.Length > index)
        {
            return parentValue[index];
        }
        else
        {
            return default;
        }
    }
    public override ref T GetReference(object instance)
    {
        var parentValue = parent.GetValue(instance);
        if (parentValue is not null && parentValue.Length > index)
        {
            return ref parentValue[index];
        }
        else
        {
            return ref Unsafe.NullRef<T>();
        }
    }
    public override void SetValue(object instance, T value)
    {
        var parentValue = parent.GetValue(instance);
        if (parentValue is not null && parentValue.Length > index)
        {
            parentValue[index] = value;
        }
    }
}
