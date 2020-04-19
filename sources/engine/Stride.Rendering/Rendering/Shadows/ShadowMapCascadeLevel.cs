// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

using Stride.Core.Mathematics;

namespace Stride.Rendering.Shadows
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ShadowMapCascadeLevel
    {
        public Matrix ViewProjReceiver;
        public Vector4 CascadeTextureCoordsBorder;
        public Vector3 Offset;
        private float padding;
    }
}
