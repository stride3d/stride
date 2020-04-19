// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Rendering.LightProbes
{
    public struct TetrahedronSortKey : IComparable<TetrahedronSortKey>
    {
        public int Index;
        public int SortKey;

        public TetrahedronSortKey(int index, int sortKey)
        {
            Index = index;
            SortKey = sortKey;
        }

        public int CompareTo(TetrahedronSortKey other)
        {
            return SortKey.CompareTo(other.SortKey);
        }

        public override string ToString()
        {
            return $"Tetrahedron Index: {Index}; SortKey: {SortKey}";
        }
    }
}
