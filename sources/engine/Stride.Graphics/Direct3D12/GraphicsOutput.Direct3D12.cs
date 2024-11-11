// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public unsafe partial class GraphicsOutput
    {
        private readonly uint outputIndex;

        /// <summary>
        ///   Gets the native DXGI output.
        /// </summary>
        internal ComPtr<IDXGIOutput> NativeOutput { get; }

        private readonly OutputDesc outputDescription;

        /// <summary>
        ///   Gets the handle of the monitor associated with this <see cref="GraphicsOutput"/>.
        /// </summary>
        public nint MonitorHandle => outputDescription.Monitor;

        /// <summary>
        ///   Initializes a new instance of <see cref="GraphicsOutput" />.
        /// </summary>
        /// <param name="adapter">The graphics adapter this output is attached to.</param>
        /// <param name="outputIndex">Index of the output.</param>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> is <see langword="null"/>.</exception>
        internal GraphicsOutput(GraphicsAdapter adapter, ComPtr<IDXGIOutput> nativeOutput, uint outputIndex)
        {
            ArgumentNullException.ThrowIfNull(adapter);

            Debug.Assert(nativeOutput.Handle != null);

            this.outputIndex = outputIndex;
            Adapter = adapter;

            // The received IDXGIOutput's lifetime is already tracked by GraphicsAdapter
            NativeOutput = nativeOutput;

            Unsafe.SkipInit(out OutputDesc outputDesc);
            HResult result = NativeOutput.GetDesc(ref outputDesc);

            if (result.IsFailure)
                result.Throw();

            Name = SilkMarshal.PtrToString((nint) outputDesc.DeviceName, NativeStringEncoding.LPWStr);

            var rectangle = outputDesc.DesktopCoordinates;
            DesktopBounds = new()
            {
                Location = *(Point*) &rectangle.Min,
                Width = rectangle.Size.X,
                Height = rectangle.Size.Y
            };

            outputDescription = outputDesc;
        }

        /// <summary>
        ///   Finds the display mode that most closely matches the requested display mode.
        /// </summary>
        /// <param name="targetProfiles">The target profiles, as available formats differ depending on the graphics profile.</param>
        /// <param name="modeToMatch">
        ///   The desired display mode.
        ///   <para>
        ///     Members of <see cref="DisplayMode"/> can be unspecified indicating no preference for that member.
        ///   </para>
        ///   <para>
        ///     A value of 0 for <see cref="DisplayMode.Width"/> or <see cref="DisplayMode.Height"/> indicates the value is unspecified.
        ///     If either <c>Width</c> or <c>Height</c> are 0, <strong>both must be 0</strong>.
        ///   </para>
        ///   <para>
        ///     A numerator and denominator of 0 in <see cref="DisplayMode.RefreshRate"/> indicate it is unspecified.
        ///   </para>
        ///   <para>
        ///     A value of <see cref="PixelFormat.None"/> for <see cref="DisplayMode"/> indicates the pixel format is unspecified.
        ///   </para>
        /// </param>
        /// <returns>Returns the mode that most closely matches <paramref name="modeToMatch"/>.</returns>
        /// <remarks>
        ///   Direct3D devices require UNORM pixel formats.
        ///   <para>
        ///     Unspecified fields are lower priority than specified fields and will be resolved later than specified fields.
        ///     Similarly ranked fields (i.e. all specified, or all unspecified, etc) are resolved in the following order: <c>Format</c>, <c>Width</c>, <c>Height</c>, <c>RefreshRate</c>.
        ///   </para>
        ///   <para>
        ///     When determining the closest value for a particular field, previously matched fields are used to filter the display mode list choices, and other fields are ignored.
        ///     For example, when matching resolution, the display mode list will have already been filtered by a certain pixel format, while the refresh rate is ignored.
        ///   </para>
        ///   <para>
        ///     This ordering doesn't define the absolute ordering for every usage scenario of <see cref="FindClosestMatchingDisplayMode"/>, because the application can choose some
        ///     values initially, effectively changing the order that fields are chosen. Fields of the display mode are matched one at a time, generally in a specified order.
        ///     If a field is unspecified, this method gravitates toward the values for the desktop related to this output. If this output is not part of the desktop, then
        ///     the default desktop output is used to find values.
        ///   </para>
        ///   <para>
        ///     If an application uses a fully unspecified display mode, <see cref="FindClosestMatchingDisplayMode"/> will typically return a display mode that matches the
        ///     desktop settings for this output.
        ///   </para>
        /// </remarks>
        public DisplayMode FindClosestMatchingDisplayMode(GraphicsProfile[] targetProfiles, DisplayMode modeToMatch)
        {
            ArgumentNullException.ThrowIfNull(targetProfiles);

            var d3d12 = D3D12.GetApi();

            // NOTE: Assume the same underlying integer type
            Debug.Assert(sizeof(GraphicsProfile) == sizeof(D3DFeatureLevel));
            var featureLevels = MemoryMarshal.Cast<GraphicsProfile, D3DFeatureLevel>(targetProfiles);

            HResult result;

            IUnknown* nativeAdapter = (IUnknown*) Adapter.NativeAdapter.Handle;
            ID3D12Device* deviceTemp = null;

            for (int i = 0; i < targetProfiles.Length; i++)
            {
                var featureLevelToTry = (D3DFeatureLevel) targetProfiles[i];

                // Create Device D3D12 with feature Level based on profile
                result = d3d12.CreateDevice(nativeAdapter, featureLevelToTry, SilkMarshal.GuidPtrOf<ID3D12Device>(),
                                            (void**) &deviceTemp);

                if (result.IsSuccess)
                    break;
            }

            if (deviceTemp == null)
                throw new InvalidOperationException("Could not create D3D12 graphics device");

            ModeDesc closestDescription;
            ModeDesc modeDescription = new()
            {
                Width = (uint) modeToMatch.Width,
                Height = (uint) modeToMatch.Height,
                RefreshRate = modeToMatch.RefreshRate.ToSilk(),
                Format = (Format) modeToMatch.Format,
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            result = NativeOutput.FindClosestMatchingMode(in modeDescription, &closestDescription, (IUnknown*) deviceTemp);

            if (result.IsFailure)
                result.Throw();

            deviceTemp->Release();

            return DisplayMode.FromDescription(closestDescription);
        }

        /// <summary>
        ///   Enumerates all available display modes for this output and stores them in <see cref="SupportedDisplayModes"/>.
        /// </summary>
        private void InitializeSupportedDisplayModes()
        {
            const int DXGI_ERROR_NOT_CURRENTLY_AVAILABLE = unchecked((int) 0x887A0022);

            HResult result = default;

            var modesAvailable = new List<DisplayMode>();
            var knownModes = new Dictionary<int, DisplayMode>();

#if DIRECTX11_1
            using ComPtr<IDXGIOutput1> output1 = NativeOutput.QueryInterface<IDXGIOutput1>();
#endif
            const uint DisplayModeEnumerationFlags = DXGI.EnumModesInterlaced | DXGI.EnumModesScaling;

            foreach (var format in Enum.GetValues<Format>())
            {
                if (format == Format.FormatForceUint)
                    continue;

                uint displayModeCount = 0;
#if DIRECTX11_1
                result = output1.GetDisplayModeList1(format, DisplayModeEnumerationFlags, ref displayModeCount, null);
#else
                result = NativeOutput.GetDisplayModeList(format, DisplayModeEnumerationFlags, ref displayModeCount, null);
#endif
                if (result.IsFailure && result.Code != DXGI_ERROR_NOT_CURRENTLY_AVAILABLE)
                    result.Throw();
                if (displayModeCount == 0)
                    continue;

#if DIRECTX11_1
                Span<ModeDesc1> displayModes = stackalloc ModeDesc1[(int) displayModeCount];
                result = output1.GetDisplayModeList1(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes[0]);
#else
                Span<ModeDesc> displayModes = stackalloc ModeDesc[(int) displayModeCount];
                result = NativeOutput.GetDisplayModeList(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes[0]);
#endif

                for (int i = 0; i < displayModeCount; i++)
                {
                    var mode = displayModes[i];

                    if (mode.Scaling == ModeScaling.Unspecified)
                    {
                        var modeKey = HashCode.Combine(format, mode.Width, mode.Height, mode.RefreshRate.Numerator, mode.RefreshRate.Denominator);

                        if (!knownModes.ContainsKey(modeKey))
                        {
                            var displayMode = DisplayMode.FromDescription(mode);

                            knownModes.Add(modeKey, displayMode);
                            modesAvailable.Add(displayMode);
                        }
                    }
                }
            }

            supportedDisplayModes = modesAvailable.ToArray();
        }

        /// <summary>
        /// Initializes <see cref="CurrentDisplayMode"/> with the most appropiate mode from <see cref="SupportedDisplayModes"/>.
        /// </summary>
        /// <remarks>
        /// It checks first for a mode with <see cref="Format.FormatR8G8B8A8Unorm"/>.
        /// If it is not found, it checks for <see cref="Format.FormatB8G8R8A8Unorm"/>.
        /// </remarks>
        private void InitializeCurrentDisplayMode()
        {
            currentDisplayMode = TryFindMatchingDisplayMode(Format.FormatR8G8B8A8Unorm) ??
                                 TryFindMatchingDisplayMode(Format.FormatB8G8R8A8Unorm);

            /// <summary>
            ///   Tries to find a display mode with the specified format that has the same size as the current desktop size
            ///   of this <see cref="GraphicsOutput"/>.
            /// </summary>
            /// <param name="format">The format to match with.</param>
            /// <returns>A matched <see cref="DisplayMode"/>, or <see langword="null"/> if nothing is found.</returns>
            private DisplayMode? TryFindMatchingDisplayMode(Format format)
            {
                var desktopBounds = outputDescription.DesktopCoordinates;

                foreach (var supportedDisplayMode in SupportedDisplayModes)
                {
                    var width = desktopBounds.Size.X;
                    var height = desktopBounds.Size.Y;
                    var matchingFormat = (PixelFormat) format;

                    if (supportedDisplayMode.Width == width &&
                        supportedDisplayMode.Height == height &&
                        supportedDisplayMode.Format == matchingFormat)
                    {
                        // TODO: DXGI, there is no way to get the DXGI.Format, nor the refresh rate
                        return new DisplayMode(matchingFormat, width, height, supportedDisplayMode.RefreshRate);
                    }
                }

                return null;
            }
        }
    }
}

#endif
