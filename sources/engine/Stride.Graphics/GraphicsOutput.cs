// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    ///   Represents an output (such as a monitor) attached to a <see cref="GraphicsAdapter"/>.
    /// </summary>
    public sealed partial class GraphicsOutput : ComponentBase
    {
        private static readonly Logger Log = GlobalLogger.GetLogger(typeof(GraphicsOutput).FullName);

        private readonly object lockModes = new();

        private DisplayMode? currentDisplayMode;
        private DisplayMode[] supportedDisplayModes;


        /// <summary>
        ///   Gets the <see cref="GraphicsAdapter"/> this output is attached to.
        /// </summary>
        public GraphicsAdapter Adapter { get; }

        /// <summary>
        ///   Gets the current display mode of this <see cref="GraphicsOutput"/>.
        /// </summary>
        public DisplayMode? CurrentDisplayMode
        {
            get
            {
                lock (lockModes)
                {
                    if (currentDisplayMode == null)
                        InitializeCurrentDisplayMode();
                }
                return currentDisplayMode;
            }
        }

        /// <summary>
        ///   Returns a collection of the supported display modes for this <see cref="GraphicsOutput"/>.
        /// </summary>
        public ReadOnlySpan<DisplayMode> SupportedDisplayModes
        {
            get
            {
                lock (lockModes)
                {
                    if (supportedDisplayModes == null)
                        InitializeSupportedDisplayModes();
                }
                return supportedDisplayModes;
            }
        }

        /// <summary>
        ///   Gets the desktop bounds of the current <see cref="GraphicsOutput"/>.
        /// </summary>
        public Rectangle DesktopBounds { get; }
    }
}
