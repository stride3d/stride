using System;
using System.Globalization;

namespace Stride.Updater.New;

public sealed class ArrayUpdatableType<T>(UpdatableType<T> elementType) : UpdatableType<T[]>
{
    public override UpdatableMember<T[]> CreateProperty(string name)
    {
        throw new NotSupportedException("Arrays do not have properties.");
    }

    public override UpdatableMember<T[]> CreateIndexer(string name)
    {
        if (int.TryParse(name, NumberStyles.Any, CultureInfo.InvariantCulture, out int index) && index >= 0)
        {
            return new ArrayIndexAccessor<T>(name, index, elementType);
        }
        throw new NotSupportedException($"Array index '{name}' is not valid.");
    }
}
