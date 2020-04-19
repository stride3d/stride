// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Rendering.Lights
{
    public struct SpotLightData
    {
        public Vector3 PositionWS;
        private float padding0;
        public Vector3 DirectionWS;
        private float padding1;
        public Vector3 AngleOffsetAndInvSquareRadius;
        private float padding2;
        public Color3 Color;
        private float padding3;
    }
}
