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

        internal static DisplayMode SelectDisplayMode(ReadOnlySpan<DisplayMode> modes, DisplayMode? current, DisplayMode mode)
        {
            if (mode.Width < 0 || mode.Height < 0 || mode.RefreshRate.Numerator < 0 || mode.RefreshRate.Denominator < 0 ||
                (mode.RefreshRate.Numerator > 0 && mode.RefreshRate.Denominator == 0))
                throw new ArgumentOutOfRangeException(nameof(mode));

            var width = mode.Width > 0 ? mode.Width : current?.Width ?? 0;
            var height = mode.Height > 0 ? mode.Height : current?.Height ?? 0;
            var format = mode.Format != PixelFormat.None ? mode.Format.ToNonSRgb() : current?.Format ?? PixelFormat.None;
            var refresh = RefreshHz(mode.RefreshRate);
            if (refresh == 0 && current.HasValue)
                refresh = RefreshHz(current.Value.RefreshRate);

            DisplayMode? best = null;
            (int Format, long Size, double Refresh) bestScore = (int.MaxValue, long.MaxValue, double.MaxValue);
            foreach (var candidate in modes)
            {
                var score = (Format: candidate.Format == format ? 0 : 1,
                    Size: (width > 0 ? Math.Abs((long)candidate.Width - width) : 0) +
                          (height > 0 ? Math.Abs((long)candidate.Height - height) : 0),
                    Refresh: refresh > 0 ? Math.Abs(RefreshHz(candidate.RefreshRate) - refresh) : 0);
                if (score.CompareTo(bestScore) < 0)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            // No enumerated modes (e.g. an unmapped desktop format): keep the request, resolved against the desktop.
            return best ?? new DisplayMode(mode.Format != PixelFormat.None ? mode.Format : format, width, height,
                mode.RefreshRate.Numerator > 0 ? mode.RefreshRate : current?.RefreshRate ?? mode.RefreshRate);
        }

        private static double RefreshHz(Rational refresh) => refresh.Denominator > 0
            ? (double)refresh.Numerator / refresh.Denominator : 0;
    }
}
