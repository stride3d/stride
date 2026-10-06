using System;
using System.Globalization;

namespace Stride.Updater.New;

public sealed class ArrayUpdatableType<T>(UpdatableType<T> elementType) : UpdatableType<T[]>
{
    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name, UpdatableMember<T[]> parent)
    {
        throw new NotSupportedException("Arrays do not have properties.");
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name, UpdatableMember<T[]> parent)
    {
        if (int.TryParse(name, NumberStyles.Any, CultureInfo.InvariantCulture, out int index))
        {
            return new ArrayIndexAccessor<T>(parent, index, elementType);
        }
        throw new NotSupportedException($"Array index '{name}' is not valid.");
    }
}
