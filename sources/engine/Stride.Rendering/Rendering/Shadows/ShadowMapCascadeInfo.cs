// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

using Stride.Core.Mathematics;

namespace Stride.Rendering.Shadows
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShadowMapCascadeInfo
    {
        public ShadowMapCascadeLevel CascadeLevels;
        public Matrix ViewProjCaster;
        public Vector4 CascadeTextureCoords;
    }
}
