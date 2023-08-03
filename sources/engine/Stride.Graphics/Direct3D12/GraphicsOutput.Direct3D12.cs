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
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate an graphics output (a monitor), it is equivalent to <see cref="Output"/>.
    /// </summary>
    /// <msdn-id>bb174546</msdn-id>
    /// <unmanaged>IDXGIOutput</unmanaged>
    /// <unmanaged-short>IDXGIOutput</unmanaged-short>
    public unsafe partial class GraphicsOutput
    {
        private readonly int outputIndex;

        /// <summary>
        ///   Gets the native output.
        /// </summary>
        /// <value>The native output.</value>
        internal IDXGIOutput* NativeOutput { get; }

        private readonly OutputDesc outputDescription;

        /// <summary>
        ///   Gets the handle of the monitor associated with this <see cref="GraphicsOutput"/>.
        /// </summary>
        /// <msdn-id>bb173068</msdn-id>
        /// <unmanaged>HMONITOR Monitor</unmanaged>
        /// <unmanaged-short>HMONITOR Monitor</unmanaged-short>
        public nint MonitorHandle => outputDescription.Monitor;

        /// <summary>
        /// Initializes a new instance of <see cref="GraphicsOutput" />.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <param name="outputIndex">Index of the output.</param>
        /// <exception cref="ArgumentNullException">output</exception>
        /// <exception cref="ArgumentOutOfRangeException">output</exception>
        internal GraphicsOutput(GraphicsAdapter adapter, IDXGIOutput* nativeOutput, int outputIndex)
        {
            ArgumentNullException.ThrowIfNull(adapter);

            Debug.Assert(nativeOutput != null);

            this.outputIndex = outputIndex;
            this.adapter = adapter;

            NativeOutput = nativeOutput;
            NativeOutput->AddRef();

            HResult result = NativeOutput->GetDesc(ref outputDescription);

            if (result.IsFailure)
                result.Throw();

            var rectangle = outputDescription.DesktopCoordinates;
            desktopBounds = new()
            {
                Location = *(Point*) &rectangle.Min,
                Width = rectangle.Size.X,
                Height = rectangle.Size.Y
            };
        }

        /// <summary>
        /// Find the display mode that most closely matches the requested display mode.
        /// </summary>
        /// <param name="targetProfiles">The target profile, as available formats are different depending on the feature level..</param>
        /// <param name="mode">The mode.</param>
        /// <returns>Returns the closes display mode.</returns>
        /// <unmanaged>HRESULT IDXGIOutput::FindClosestMatchingMode([In] const DXGI_MODE_DESC* pModeToMatch,[Out] DXGI_MODE_DESC* pClosestMatch,[In, Optional] IUnknown* pConcernedDevice)</unmanaged>
        /// <remarks>Direct3D devices require UNORM formats. This method finds the closest matching available display mode to the mode specified in pModeToMatch. Similarly ranked fields (i.e. all specified, or all unspecified, etc) are resolved in the following order.  ScanlineOrdering Scaling Format Resolution RefreshRate  When determining the closest value for a particular field, previously matched fields are used to filter the display mode list choices, and  other fields are ignored. For example, when matching Resolution, the display mode list will have already been filtered by a certain ScanlineOrdering,  Scaling, and Format, while RefreshRate is ignored. This ordering doesn't define the absolute ordering for every usage scenario of FindClosestMatchingMode, because  the application can choose some values initially, effectively changing the order that fields are chosen. Fields of the display mode are matched one at a time, generally in a specified order. If a field is unspecified, FindClosestMatchingMode gravitates toward the values for the desktop related to this output.  If this output is not part of the desktop, then the default desktop output is used to find values. If an application uses a fully unspecified  display mode, FindClosestMatchingMode will typically return a display mode that matches the desktop settings for this output.   Unspecified fields are lower priority than specified fields and will be resolved later than specified fields.</remarks>
        public DisplayMode FindClosestMatchingDisplayMode(GraphicsProfile[] targetProfiles, DisplayMode mode)
        {
            ArgumentNullException.ThrowIfNull(targetProfiles);

            var d3d12 = D3D12.GetApi();

            // NOTE: Assume the same underlying integer type
            Debug.Assert(sizeof(GraphicsProfile) == sizeof(D3DFeatureLevel));
            var featureLevels = MemoryMarshal.Cast<GraphicsProfile, D3DFeatureLevel>(targetProfiles);

            HResult result;

            IUnknown* nativeAdapter = (IUnknown*) adapter.NativeAdapter;
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
                Width = (uint) mode.Width,
                Height = (uint) mode.Height,
                RefreshRate = mode.RefreshRate.ToSilk(),
                Format = (Format) mode.Format,
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            result = NativeOutput->FindClosestMatchingMode(in modeDescription, &closestDescription, (IUnknown*) deviceTemp);

            if (result.IsFailure)
                result.Throw();

            return DisplayMode.FromDescription(closestDescription);
        }

        /// <summary>
        /// Enumerates all available display modes for this output and stores them in <see cref="SupportedDisplayModes"/>.
        /// </summary>
        private void InitializeSupportedDisplayModes()
        {
            const int DXGI_ERROR_NOT_CURRENTLY_AVAILABLE = unchecked((int) 0x887A0022);

            HResult result = default;

            var modesAvailable = new List<DisplayMode>();
            var modesMap = new Dictionary<string, DisplayMode>();

#if DIRECTX11_1
            IDXGIOutput1* output1 = null;
            NativeOutput->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIOutput1>(), (void**) &output1);
#endif
            const uint DisplayModeEnumerationFlags = DXGI.EnumModesInterlaced | DXGI.EnumModesScaling;

            foreach (var format in Enum.GetValues<Format>())
            {
                uint displayModeCount = 0;
#if DIRECTX11_1
                result = output1->GetDisplayModeList1(format, DisplayModeEnumerationFlags, ref displayModeCount, null);
#else
                result = NativeOutput->GetDisplayModeList(format, DisplayModeEnumerationFlags, ref displayModeCount, null);
#endif
                if (result.IsFailure && result.Code != DXGI_ERROR_NOT_CURRENTLY_AVAILABLE)
                    result.Throw();

#if DIRECTX11_1
                Span<ModeDesc1> displayModes = stackalloc ModeDesc1[(int) displayModeCount];
                result = output1->GetDisplayModeList1(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes[0]);
#else
                Span<ModeDesc> displayModes = stackalloc ModeDesc[(int) displayModeCount];
                result = NativeOutput->GetDisplayModeList(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes[0]);
#endif

                for (int i = 0; i < displayModeCount; i++)
                {
                    var mode = displayModes[i];

                    if (mode.Scaling == ModeScaling.Unspecified)
                    {
                        var key = FormattableString.Invariant($"{format};{mode.Width};{mode.Height};{mode.RefreshRate.Numerator};{mode.RefreshRate.Denominator}");

                        if (!modesMap.TryGetValue(key, out DisplayMode oldMode))
                        {
                            var displayMode = DisplayMode.FromDescription(mode);

                            modesMap.Add(key, displayMode);
                            modesAvailable.Add(displayMode);
                        }
                    }
                }
            }

            supportedDisplayModes = modesAvailable.ToArray();

#if DIRECTX11_1
            if (output1 != null)
                output1->Release();
#endif
        }

        /// <summary>
        /// Initializes <see cref="CurrentDisplayMode"/> with the most appropiate mode from <see cref="SupportedDisplayModes"/>.
        /// </summary>
        /// <remarks>It checks first for a mode with <see cref="Format.FormatR8G8B8A8Unorm"/>,
        /// if it is not found - it checks for <see cref="Format.FormatB8G8R8A8Unorm"/>.</remarks>
        private void InitializeCurrentDisplayMode()
        {
            currentDisplayMode = TryFindMatchingDisplayMode(Format.FormatR8G8B8A8Unorm) ??
                                 TryFindMatchingDisplayMode(Format.FormatB8G8R8A8Unorm);
        }

        /// <summary>
        /// Tries to find a display mode that has the same size as the current <see cref="OutputDesc"/> associated with this instance
        /// of the specified format.
        /// </summary>
        /// <param name="format">The format to match with.</param>
        /// <returns>A matched <see cref="DisplayMode"/> or null if nothing is found.</returns>
        private DisplayMode TryFindMatchingDisplayMode(Format format)
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

#endif
