// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL

using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Graphics
{
    public partial class GraphicsOutput
    {
        private readonly int displayIndex;

        public string DisplayName { get; init; }

        public IntPtr MonitorHandle
        {
            get { return IntPtr.Zero; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GraphicsOutput" />.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <param name="displayIndex">Index of the output.</param>
        /// <exception cref="System.ArgumentNullException">output</exception>
        internal GraphicsOutput(GraphicsAdapter adapter, int displayIndex)
        {
            if (adapter == null) throw new ArgumentNullException("adapter");
            
            this.adapter = adapter;
            this.displayIndex = displayIndex;

            var SDL = Stride.Graphics.SDL.Window.SDL;

            unsafe
            {
                var bounds = new Silk.NET.Maths.Rectangle<int>();
                SDL.GetDisplayBounds(displayIndex, &bounds);
                desktopBounds.Width = bounds.Size.X;
                desktopBounds.Height = bounds.Size.Y;
                desktopBounds.X = bounds.Origin.X;
                desktopBounds.Y = bounds.Origin.Y;
            }

            DisplayName = SDL.GetDisplayNameS(displayIndex);
        }

        /// <summary>
        /// Find the display mode that most closely matches the requested display mode.
        /// </summary>
        /// <param name="targetProfiles">The target profile, as available formats are different depending on the feature level..</param>
        /// <param name="mode">The mode.</param>
        /// <returns>Returns the closes display mode.</returns>
        public DisplayMode FindClosestMatchingDisplayMode(GraphicsProfile[] targetProfiles, DisplayMode mode)
        {
            var SDL = Stride.Graphics.SDL.Window.SDL;

            DisplayMode closest;
            unsafe
            {
                var modeSDL = new Silk.NET.SDL.DisplayMode(0, mode.Width, mode.Height, mode.RefreshRate.Denominator / mode.RefreshRate.Numerator);
                var closestSDL = new Silk.NET.SDL.DisplayMode();

                SDL.GetClosestDisplayMode(displayIndex, &modeSDL, &closestSDL);
                closest = new DisplayMode(PixelFormat.None, closestSDL.W, closestSDL.H, new Rational(1, closestSDL.RefreshRate));
            }

            return closest;
        }

        private void InitializeSupportedDisplayModes()
        {
            var SDL = Stride.Graphics.SDL.Window.SDL;

            var modesMap = new Dictionary<(int w, int h, int refreshRate), DisplayMode>();
            var modeCount = SDL.GetNumDisplayModes(displayIndex);

            unsafe
            {
                for (int i = 0; i < modeCount; i++)
                {
                    var sdlMode = new Silk.NET.SDL.DisplayMode();
                    SDL.GetDisplayMode(displayIndex, i, &sdlMode);

                    var key = (sdlMode.W, sdlMode.H, sdlMode.RefreshRate);

                    if (!modesMap.ContainsKey(key))
                    {
                        //We should probably convert the sdlMode format
                        //to the engine's Pixel Format
                        modesMap.Add(key, new DisplayMode(PixelFormat.None, sdlMode.W, sdlMode.H, new Rational(1, sdlMode.RefreshRate)));
                    }
                }
            }

            supportedDisplayModes = modesMap.Values.ToArray();
        }

        private void InitializeCurrentDisplayMode()
        {
            var SDL = Stride.Graphics.SDL.Window.SDL;
            var mode = new DisplayMode(PixelFormat.None, desktopBounds.Width, desktopBounds.Height, new Rational(1, 0));

            currentDisplayMode = FindClosestMatchingDisplayMode([], mode);
        }
    }
}
#endif
