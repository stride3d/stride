// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Rendering.Lights
{
    public struct PointLightData
    {
        public Vector3 PositionWS;
        public float InvSquareRadius;
        public Color3 Color;
        private float padding0;
    }
}
