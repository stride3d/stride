// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Assets.Presentation.DebugShapes
{
    public class DebugShapeTorus : DebugShape
    {
        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Torus.New(device).ToMeshDraw();
        }
    }
}
