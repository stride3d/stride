using System;
using System.Collections.Generic;
using System.Globalization;

namespace Stride.Updater.New;

public sealed class ListUpdatableType<TList, TElement>(UpdatableType<TElement> elementType) : UpdatableType<TList>
    where TList : IList<TElement>
{
    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name, UpdatableMember<TList> parent)
    {
        throw new NotSupportedException("Lists do not have properties.");
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name, UpdatableMember<TList> parent)
    {
        if (int.TryParse(name, NumberStyles.Any, CultureInfo.InvariantCulture, out int index))
        {
            return new ListIndexAccessor<TList, TElement>(parent, index, elementType);
        }
        throw new NotSupportedException($"List index '{name}' is not valid.");
    }
}
