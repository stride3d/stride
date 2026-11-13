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
    /// <remarks>
    ///   On Vulkan outside Windows, SDL does not report the rotation and color capabilities of a display, so
    ///   <see cref="Rotation"/> and the color and luminance properties describe an SDR sRGB display.
    /// </remarks>
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

        /// <summary>
        ///   Gets a description or friendly name of the output.
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///   Gets a value indicating whether the output (monitor) is currently attached to the desktop.
        /// </summary>
        /// <value>
        ///   <list type="bullet">
        ///     <item>
        ///       <see langword="true"/> if the output (monitor) is part of the visible workspace, meaning the
        ///       device is connected and has valid desktop coordinates;
        ///     </item>
        ///     <item>
        ///       <see langword="false"/> if it is known but disconnected or disabled, and thus not part of the desktop.
        ///     </item>
        ///   </list>
        /// </value>
        public bool IsAttached { get; }

        /// <summary>
        ///   Gets the rotation of the display attached to this output.
        /// </summary>
        /// <remarks>
        ///   This value indicates how the Back-Buffers should be rotated to fit the physical rotation of a monitor.
        /// </remarks>
        public DisplayRotation Rotation { get; }

        /// <summary>
        ///   Gets the advanced color capabilities of the display attached to this output.
        /// </summary>
        /// <value>
        ///   The advanced color space of the output. Specifically, whether it's capable of reproducing color and
        ///   luminance values outside of the sRGB color space.
        ///   <list type="bullet">
        ///     <item>
        ///       A value of <see cref="ColorSpaceType.Rgb_Full_G22_None_P709"/> indicates that the display is limited to SDR / sRGB.
        ///     </item>
        ///     <item>
        ///       A value of <see cref="ColorSpaceType.Rgb_Full_G2084_None_P2020"/> indicates that the display supports advanced color capabilities.
        ///     </item>
        ///   </list>
        /// </value>
        public ColorSpaceType ColorSpace { get; }

        /// <summary>
        ///   Gets a value indicating whether High Dynamic Range (HDR) is supported by the current device
        ///   or display configuration.
        /// </summary>
        public bool SupportsHDR => ColorSpace is ColorSpaceType.Rgb_Full_G2084_None_P2020;

        /// <summary>
        ///   Gets the number of bits per color channel for the active wire format of the display attached to this output.
        /// </summary>
        public int BitsPerChannel { get; }

        /// <summary>
        ///   Gets the minimum luminance, in nits, that the display attached to this output is capable of rendering.
        /// </summary>
        /// <remarks>
        ///   Content should not exceed this minimum value for optimal rendering.
        /// </remarks>
        public float MinLuminance { get; }

        /// <summary>
        ///   Gets the maximum luminance, in nits, that the display attached to this output is capable of rendering.
        /// </summary>
        /// <remarks>
        ///   This value is likely only valid for a small area of the panel.
        ///   Content should not exceed this maximum value for optimal rendering.
        /// </remarks>
        public float MaxLuminance { get; }

        /// <summary>
        ///   Gets the maximum luminance, in nits, that the display attached to this output is capable of rendering
        ///   when a color fills the entire area of the panel.
        /// </summary>
        /// <remarks>
        ///   Content should not exceed this value across the entire panel for optimal rendering.
        /// </remarks>
        public float MaxFullFrameLuminance { get; }

        /// <summary>
        ///   Gets the red color primary, in XY coordinates, of the display attached to this output.
        /// </summary>
        public Vector2 RedPrimary { get; }

        /// <summary>
        ///   Gets the green color primary, in XY coordinates, of the display attached to this output.
        /// </summary>
        public Vector2 GreenPrimary { get; }

        /// <summary>
        ///   Gets the blue color primary, in XY coordinates, of the display attached to this output.
        /// </summary>
        public Vector2 BluePrimary { get; }

        /// <summary>
        ///   Gets the white point, in XY coordinates, of the display attached to this output.
        /// </summary>
        public Vector2 WhitePoint { get; }

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
