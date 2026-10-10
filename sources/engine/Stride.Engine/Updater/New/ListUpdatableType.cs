using System;
using System.Collections.Generic;
using System.Globalization;

namespace Stride.Updater.New;

public sealed class ListUpdatableType<TList, TElement>(UpdatableType<TElement> elementType) : UpdatableType<TList>
    where TList : IList<TElement>
{
    public override UpdatableMember<TList> CreateProperty(string name)
    {
        throw new NotSupportedException("Lists do not have properties.");
    }

    public override UpdatableMember<TList> CreateIndexer(string name)
    {
        if (int.TryParse(name, NumberStyles.Any, CultureInfo.InvariantCulture, out int index) && index >= 0)
        {
            return new ListIndexAccessor<TList, TElement>(name, index, elementType);
        }
        throw new NotSupportedException($"List index '{name}' is not valid.");
    }
}
