using System;
using System.Collections.Generic;

namespace Stride.Updater.New;

public sealed class ListIndexAccessor<TList, TValue>(UpdatableMember<TList> parent, int index, UpdatableType<TValue> elementType) : UpdatableMember<TValue>
    where TList : IList<TValue>
{
    public override bool SupportsByReference => false;

    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
    {
        return elementType.ResolveProperty(name, this);
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
    {
        return elementType.ResolveIndexer(name, this);
    }

    public override TValue GetValue(object instance)
    {
        var parentValue = parent.GetValue(instance);
        if (parentValue is not null && parentValue.Count > index)
        {
            return parentValue[index];
        }
        else
        {
            return default;
        }
    }

    public override ref TValue GetReference(object instance)
    {
        throw new NotSupportedException("List elements do not support by-reference access.");
    }

    public override void SetValue(object instance, TValue value)
    {
        var parentValue = parent.GetValue(instance);
        if (parentValue is not null && parentValue.Count > index)
        {
            parentValue[index] = value;
        }
    }
}
