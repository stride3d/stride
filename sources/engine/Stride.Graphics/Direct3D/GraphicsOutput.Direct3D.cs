// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

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
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate an graphics output (a monitor), it is equivalent to <see cref="IDXGIOutput"/>.
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

        protected readonly OutputDesc outputDescription;

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

            HResult result = nativeOutput->GetDesc(ref outputDescription);

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

            var d3d11 = D3D11.GetApi(window: null);

            // NOTE: Assume the same underlying integer type
            Debug.Assert(sizeof(GraphicsProfile) == sizeof(D3DFeatureLevel));
            var featureLevels = MemoryMarshal.Cast<GraphicsProfile, D3DFeatureLevel>(targetProfiles);

            IDXGIAdapter* nativeAdapter = (IDXGIAdapter*) adapter.NativeAdapter;
            ID3D11Device* deviceTemp = null;
            ID3D11DeviceContext* deviceContext = null;
            D3DFeatureLevel createdFeatureLevel;

            d3d11.CreateDevice(nativeAdapter, D3DDriverType.Unknown, Software: 0, Flags: 0,
                               featureLevels, (uint) featureLevels.Length,
                               D3D11.SdkVersion,
                               &deviceTemp, &createdFeatureLevel, &deviceContext);

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

            HResult result = NativeOutput->FindClosestMatchingMode(in modeDescription, &closestDescription, (IUnknown*) deviceTemp);

            if (result.IsFailure)
            {
                //Log.Error($"Failed to create Direct3D device using {adapter.NativeAdapter.Description} adapter with profiles: {string.Join(", ", targetProfiles)}.\nException: {exception}");
                result.Throw();
            }

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
                if (format == Format.FormatForceUint)
                    continue;

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
        /// Initializes <see cref="CurrentDisplayMode"/> with the current <see cref="DisplayMode"/>,
        /// closest matching mode with the provided format (<see cref="Format.R8G8B8A8_UNorm"/> or <see cref="Format.B8G8R8A8_UNorm"/>)
        /// or null in case of errors.
        /// </summary>
        private void InitializeCurrentDisplayMode()
        {
            if (!TryGetCurrentDisplayMode(out DisplayMode displayMode) && !TryGetClosestMatchingMode(Format.R8G8B8A8_UNorm, out displayMode))
                TryGetClosestMatchingMode(Format.B8G8R8A8_UNorm, out displayMode);

            currentDisplayMode = displayMode;
        }

        /// <summary>
        /// Tries to get current display mode based on output bounds from <see cref="outputDescription"/>.
        /// </summary>
        /// <param out name="currentDisplayMode">Current <see cref="DisplayMode"/> or null</param>
        /// <returns><see cref="bool"/> depending on the outcome</returns>
        private bool TryGetCurrentDisplayMode(out DisplayMode currentDisplayMode)
        {
            // We don't care about FeatureLevel because we want to get missing data 
            // about the current display/monitor mode and not the supported display mode for the specific graphics profile
            if (!TryCreateDirect3DDevice(out SharpDX.Direct3D11.Device deviceTemp))
            {
                currentDisplayMode = null;

                return false;
            }

            RawRectangle desktopBounds = outputDescription.DesktopBounds;
            // We don't specify RefreshRate on purpose, it will be automatically
            // filled in with current RefreshRate of the output (dispaly/monitor) by GetClosestMatchingMode
            ModeDescription description = new ModeDescription
            {
                Width = desktopBounds.Right - desktopBounds.Left,
                Height = desktopBounds.Bottom - desktopBounds.Top,
                // Format will be automatically filled with the RefreshRate parameter if we pass reference to the Direct3D11 device
                Format = Format.Unknown
            };

            using (SharpDX.Direct3D11.Device device = deviceTemp)
            {
                try
                {
                    output.GetClosestMatchingMode(device, description, out ModeDescription closestDescription);

                    currentDisplayMode = DisplayMode.FromDescription(closestDescription);

                    return true;
                }
                catch (Exception exception)
                {
                    Log.Error($"Failed to get current display mode. The resolution: {description.Width}x{description.Height} " +
                        $"taken from output is not correct.\nException: {exception}");

                    currentDisplayMode = null;

                    return false;
                }
            }
        }

        /// <summary>
        /// Tries to get closest display mode based on output bounds from <see cref="outputDescription"/> and provided format.
        /// </summary>
        /// <param name="format">Format in which we want find closest display mode</param>
        /// <param out name="closestMatchingMode">closest <see cref="DisplayMode"/> or null</param>
        /// <returns><see cref="bool"/> depending on the outcome</returns>
        private bool TryGetClosestMatchingMode(Format format, out DisplayMode closestMatchingMode)
        {
            RawRectangle desktopBounds = outputDescription.DesktopBounds;
            // We don't specify RefreshRate on purpose, it will be automatically
            // filled in with current RefreshRate of the output (dispaly/monitor) by GetClosestMatchingMode
            ModeDescription description = new ModeDescription
            {
                Width = desktopBounds.Right - desktopBounds.Left,
                Height = desktopBounds.Bottom - desktopBounds.Top,
                Format = format
            };

            try
            {
                output.GetClosestMatchingMode(null, description, out ModeDescription closestDescription);

                closestMatchingMode = DisplayMode.FromDescription(closestDescription);

                return true;
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to find closest matching mode. The resolution: {description.Width}x{description.Height} " +
                    $"taken from output and/or format: {format} is not correct.\nException: {exception}");

                closestMatchingMode = null;

                return false;
            }
        }

        private bool TryCreateDirect3DDevice(out SharpDX.Direct3D11.Device device)
        {
            device = null;

            try
            {
                device = new SharpDX.Direct3D11.Device(adapter.NativeAdapter);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to create Direct3D device using {adapter.NativeAdapter.Description}.\nException: {exception}");

                return false;
            }

            return true;
        }
    }
}

#endif
