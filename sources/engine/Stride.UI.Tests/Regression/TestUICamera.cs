// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Graphics.Regression;
using Stride.Rendering.Compositing;

namespace Stride.UI.Tests.Regression
{
    public class TestUICamera : TestCamera
    {
        public TestUICamera(GraphicsCompositor graphicsCompositor)
            : base(graphicsCompositor)
        {
        }

        protected override void SetCamera()
        {
            base.SetCamera();
            Camera.NearClipPlane = 1f;
            Camera.FarClipPlane = 10000f;
        }
    }
}
