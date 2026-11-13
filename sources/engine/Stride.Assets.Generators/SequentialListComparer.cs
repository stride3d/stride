// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Assets.Generators;

internal sealed class SequentialListComparer<T> : IEqualityComparer<List<T>> where T : IEquatable<T>
{
    public static readonly SequentialListComparer<T> Instance = new();
    public bool Equals(List<T>? x, List<T>? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null || x.Count != y.Count)
            return false;
        for (var i = 0; i < x.Count; i++)
        {
            if (!x[i].Equals(y[i]))
                return false;
        }
        return true;
    }
    public int GetHashCode(List<T> obj)
    {
        var hash = 17;
        foreach (var item in obj)
            hash = unchecked(hash * 31 + item.GetHashCode());
        return hash;
    }
}
