// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Extensions;

public static class ListExtensions
{
    public static IEnumerable<T> Subset<T>(this IList<T> list, int startIndex, int count)
    {
        for (var i = startIndex; i < startIndex + count; ++i)
        {
            yield return list[i];
        }
    }

    public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> items)
    {
        if (list is List<T> l)
        {
            l.AddRange(items);
        }
        else
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }
}
