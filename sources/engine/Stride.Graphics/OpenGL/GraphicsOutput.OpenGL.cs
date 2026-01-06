// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;

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
        ///   Initializes a new instance of <see cref="GraphicsOutput"/>.
        /// </summary>
        /// <param name="adapter">The Graphics Adapter the output depends on.</param>
        /// <param name="displayIndex">The index of the output.</param>
        /// <exception cref="System.ArgumentNullException"><paramref cref="output"/> is <see langword="null"/>.</exception>
        internal GraphicsOutput(GraphicsAdapter adapter, int displayIndex)
        {
            ArgumentNullException.ThrowIfNull(adapter);

            Adapter = adapter;
            this.displayIndex = displayIndex;

            var SDL = Graphics.SDL.Window.SDL;

            unsafe
            {
                var bounds = new Silk.NET.Maths.Rectangle<int>();
                SDL.GetDisplayBounds(displayIndex, &bounds);
                DesktopBounds = new Rectangle(bounds.Origin.X, bounds.Origin.Y, bounds.Size.X, bounds.Size.Y);
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
            var mode = new DisplayMode(PixelFormat.None, DesktopBounds.Width, DesktopBounds.Height, new Rational(1, 0));

            currentDisplayMode = FindClosestMatchingDisplayMode([], mode);
        }
    }
}
#endif
