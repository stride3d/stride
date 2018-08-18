// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Xenko.Games
{
    /// <summary>
    /// Helper functions to determine lower and upper bounds in a sorted list.
    /// </summary>
    internal static class ListBoundExtensions
    {
        // http://www.cplusplus.com/reference/algorithm/lower_bound/
        public static int LowerBound<TItem>(this List<TItem> list, TItem value, IComparer<TItem> comparer, int index, int count)
        {
            while (count > 0)
            {
                int half = count >> 1;
                int middle = index + half;
                if (comparer.Compare(list[middle], value) < 0)
                {
                    index = middle + 1;
                    count = count - half - 1;
                }
                else
                {
                    count = half;
                }
            }
            return index;
        }

        // http://www.cplusplus.com/reference/algorithm/upper_bound/
        public static int UpperBound<TItem>(this List<TItem> list, TItem value, IComparer<TItem> comparer, int index, int count)
        {
            while (count > 0)
            {
                int half = count >> 1;
                int middle = index + half;
                if (comparer.Compare(value, list[middle]) >= 0)
                {
                    index = middle + 1;
                    count = count - half - 1;
                }
                else
                {
                    count = half;
                }
            }
            return index;
        }
    }
}
