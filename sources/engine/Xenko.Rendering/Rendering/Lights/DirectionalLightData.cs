// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Lights
{
    public struct DirectionalLightData
    {
        public Vector3 DirectionWS;
        private float padding0;
        public Color3 Color;
        private float padding1;
    }
}
