﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Rendering.Lights
{
    public struct DirectionalLightData
    {
        public Vector3 DirectionWS;
        private float padding0;
        public Color3 Color;
        private float padding1;
    }
}
