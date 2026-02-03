// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using Vortice.Vulkan;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate an graphics output (a monitor).
    /// </summary>
    public partial class GraphicsOutput
    {
        private readonly DisplayInfo displayInfo;
        private readonly int outputIndex;

        // TODO VULKAN
        internal GraphicsOutput() { } // Here for GraphicsAdapter.Vulkan to be able to create a GraphicsOutput without an adapter

        /// <summary>
        /// Initializes a new instance of <see cref="GraphicsOutput" />.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <param name="outputIndex">Index of the output.</param>
        /// <exception cref="System.ArgumentNullException">output</exception>
        /// <exception cref="ArgumentOutOfRangeException">output</exception>
        internal GraphicsOutput(GraphicsAdapter adapter, DisplayInfo displayInfo, int outputIndex)
        {
            if (adapter == null) throw new ArgumentNullException("adapter");

            this.outputIndex = outputIndex;
            this.displayInfo = displayInfo;

            Adapter = adapter;
            DesktopBounds = displayInfo.Bounds;
        }

        /// <summary>
        /// Find the display mode that most closely matches the requested display mode.
        /// </summary>
        /// <param name="_">Vulkan does not rely on GraphicsProfile when finding the closest matching display mode.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>Returns the closes display mode.</returns>
        public DisplayMode FindClosestMatchingDisplayMode(GraphicsProfile[] _, DisplayMode mode)
        {
            if (supportedDisplayModes == null || supportedDisplayModes.Length == 0)
                throw new Exception("Couldn't find any supported display modes for selected display.");

            // Display mode selection is based solely on the GraphicsOutput’s supported display modes and VkFormats.
            //
            // Strategy:
            // - Primary: Closest resolution.
            // - Secondary: Closest/higher refresh rate.

            HashSet<VkFormat> supportedVkFormats = displayInfo.SupportedFormats
                .Select(f => f.format)
                .ToHashSet();
            DisplayMode bestMode = supportedDisplayModes[0];
            double bestScore = double.MaxValue;
            double modeRefreshDouble = GetRefreshRateDouble(mode.RefreshRate);

            foreach (var candidate in supportedDisplayModes)
            {
                double widthDiff = Math.Abs(candidate.Width - mode.Width);
                double heightDiff = Math.Abs(candidate.Height - mode.Height);
                double candidateRefreshDouble = GetRefreshRateDouble(candidate.RefreshRate);
                double refreshDiff = Math.Abs(candidateRefreshDouble - modeRefreshDouble);
                double score = widthDiff * 1000.0 + heightDiff * 1000.0 + refreshDiff * 10.0;
                bool betterRefreshRateOnTie = Math.Abs(score - bestScore) < 0.0001
                    && HasHigherRefreshRate(candidate.RefreshRate, bestMode.RefreshRate);

                if (score < bestScore || betterRefreshRateOnTie)
                {
                    bestScore = score;
                    bestMode = candidate;
                }
            }

            VkFormat desiredFormat = bestMode.Format.ConvertPixelFormat();
            bool supportsFormat = supportedVkFormats.Contains(desiredFormat);
            bool supportsAnyFormat = displayInfo.SupportedFormats.Length != 1 || displayInfo.SupportedFormats[0].format != VkFormat.Undefined;
            if (!supportsFormat && supportsAnyFormat)
                bestMode = new DisplayMode()
                {
                    Width = bestMode.Width,
                    Height = bestMode.Height,
                    RefreshRate = bestMode.RefreshRate,
                    Format = displayInfo.SupportedFormats[0].format.ConvertVkFormat()
                };

            return bestMode;
        }

        private static bool HasHigherRefreshRate(Rational left, Rational right)
        {
            return (long)left.Numerator * right.Denominator > (long)right.Numerator * left.Denominator;
        }

        private static double GetRefreshRateDouble(Rational rational)
        {
            return rational.Denominator == 0 ? 0.0 : rational.Numerator / (double)rational.Denominator;
        }

        /// <summary>
        /// Retrieves the handle of the monitor associated with this <see cref="GraphicsOutput"/>.
        /// </summary>
        /// <msdn-id>bb173068</msdn-id>	
        /// <unmanaged>HMONITOR Monitor</unmanaged>	
        /// <unmanaged-short>HMONITOR Monitor</unmanaged-short>	
        public IntPtr MonitorHandle
        {
            get
            {
                return displayInfo.Handle;
            }
        }

        /// <summary>
        /// Gets all available display modes for this output from <see cref="displayInfo"/> and stores them in <see cref="SupportedDisplayModes"/>.
        /// </summary>
        private void InitializeSupportedDisplayModes()
        {
            supportedDisplayModes = displayInfo.SupportedModes;
        }

        /// <summary>
        /// Initializes <see cref="CurrentDisplayMode"/> with the current mode from <see cref="displayInfo"/>.
        /// </summary>
        private void InitializeCurrentDisplayMode()
        {
            currentDisplayMode = displayInfo.CurrentMode;
        }
    }
}
#endif
