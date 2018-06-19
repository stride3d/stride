// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core.Annotations;

namespace Xenko.Core.Extensions
{
    public static class ListExtensions
    {
        public static IEnumerable<T> Subset<T>(this IList<T> list, int startIndex, int count)
        {
            for (var i = startIndex; i < startIndex + count; ++i)
            {
                yield return list[i];
            }
        }

        public static void AddRange<T>(this ICollection<T> list, [NotNull] IEnumerable<T> items)
        {
            var l = list as List<T>;
            if (l != null)
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
}
