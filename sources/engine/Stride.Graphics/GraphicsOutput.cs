// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public partial class GraphicsOutput : ComponentBase
    {
        private readonly object lockModes = new object();
        private readonly GraphicsAdapter adapter;
        private DisplayMode currentDisplayMode;
        private DisplayMode[] supportedDisplayModes;
        private readonly Rectangle desktopBounds;

        /// <summary>
        /// Default constructor to initialize fields that are not explicitly set to avoid warnings at compile time.
        /// </summary>
        internal GraphicsOutput()
        {
            adapter = null;
            supportedDisplayModes = null;
            desktopBounds = Rectangle.Empty;
        }

        /// <summary>
        /// Gets the current display mode.
        /// </summary>
        /// <value>The current display mode.</value>
        public DisplayMode CurrentDisplayMode
        {
            get
            {
                lock (lockModes)
                {
                    if (currentDisplayMode == null)
                    {
                        InitializeCurrentDisplayMode();
                    }
                }
                return currentDisplayMode;
            }
        }

        /// <summary>
        /// Returns a collection of supported display modes for this <see cref="GraphicsOutput"/>.
        /// </summary>
        public DisplayMode[] SupportedDisplayModes
        {
            get
            {
                lock (lockModes)
                {
                    if (supportedDisplayModes == null)
                    {
                        InitializeSupportedDisplayModes();
                    }
                }
                return supportedDisplayModes;
            }
        }

        /// <summary>
        /// Gets the desktop bounds of the current output.
        /// </summary>
        public Rectangle DesktopBounds
        {
            get
            {
                return desktopBounds;
            }
        }

        /// <summary>
        /// Gets the adapter this output is attached.
        /// </summary>
        /// <value>The adapter.</value>
        public GraphicsAdapter Adapter
        {
            get { return adapter; }
        }
    }
}
