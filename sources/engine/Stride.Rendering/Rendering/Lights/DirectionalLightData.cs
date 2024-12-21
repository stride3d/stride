// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Rendering.Lights
{
    public struct DirectionalLightData
    {
#pragma warning disable 169 // The field <X> is never used
        public Vector3 DirectionWS;
        private float padding0;
        public Color3 Color;
        private float padding1;
#pragma warning restore 169
    }
}
