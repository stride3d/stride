// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;

namespace Stride.Rendering
{
    public static class StrideTessellationMethodExtensions
    {
        public static bool PerformsAdjacentEdgeAverage(this StrideTessellationMethod method)
        {
            return (method & StrideTessellationMethod.AdjacentEdgeAverage) != 0;
        }

        public static PrimitiveType GetPrimitiveType(this StrideTessellationMethod method)
        {
            if ((method & StrideTessellationMethod.PointNormal) == 0)
                return PrimitiveType.TriangleList;

            var controlsCount = method.PerformsAdjacentEdgeAverage() ? 12 : 3;
            return PrimitiveType.PatchList.ControlPointCount(controlsCount);
        }
    }
}
