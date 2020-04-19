// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL

using System;

namespace Stride.Graphics
{
    public partial class GraphicsOutput
    {
        public DisplayMode FindClosestMatchingDisplayMode(GraphicsProfile[] targetProfiles, DisplayMode mode)
        {
            return mode;
        }

        public IntPtr MonitorHandle
        {
            get { return IntPtr.Zero; }
        }

        private void InitializeSupportedDisplayModes()
        {
        }

        private void InitializeCurrentDisplayMode()
        {
            currentDisplayMode = null;
        }
    }
}
#endif
