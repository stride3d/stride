// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;

namespace Stride.Rendering
{
    public class ViewportState
    {
        public Viewport Viewport0;
        //public Viewport Viewport1;
        //public Viewport Viewport2;
        //public Viewport Viewport3;
        //public Viewport Viewport4;
        //public Viewport Viewport5;
        //public Viewport Viewport6;
        //public Viewport Viewport7;

        /// <summary>
        /// Capture state from <see cref="CommandList.Viewports"/>.
        /// </summary>
        /// <param name="commandList">The command list to capture state from.</param>
        public unsafe void CaptureState(CommandList commandList)
        {
            // TODO: Support multiple viewports
            var viewports = commandList.Viewports;
            fixed (Viewport* viewport0 = &Viewport0)
            {
                for (int i = 0; i < 1; ++i)
                {
                    viewport0[i] = viewports[i];
                }
            }
        }
    }
}
