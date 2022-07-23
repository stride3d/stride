//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    public class BezierPoint
    {
        public Vector3 Position;
        public float DistanceToPreviousPoint;
        public float TotalLengthOnCurve;
    }
}
