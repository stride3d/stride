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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

using Stride.Core.Mathematics;
using Stride.Core.UnsafeExtensions;

using Rectangle = Stride.Core.Mathematics.Rectangle;

namespace Stride.Graphics
{
    public unsafe partial class GraphicsOutput
    {
        private IDXGIOutput* dxgiOutput;
        private readonly uint dxgiOutputVersion;

        /// <summary>
        ///   Gets the native DXGI output.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<IDXGIOutput> NativeOutput => ComPtrHelpers.ToComPtr(dxgiOutput);

        /// <summary>
        ///   Gets the version number of the native DXGI output supported.
        /// </summary>
        /// <value>
        ///   This indicates the latest DXGI output interface version supported by this output.
        ///   For example, if the value is 4, then this output supports up to <see cref="IDXGIOutput4"/>.
        /// </value>
        internal uint NativeOutputVersion => dxgiOutputVersion;

        /// <summary>
        ///   Gets the handle of the monitor associated with this <see cref="GraphicsOutput"/>.
        /// </summary>
        public nint MonitorHandle { get; }

        /// <summary>
        ///   Gets a description or friendly name of the output.
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///   Gets a value indicating whether the output (monitor) is currently attached to the Windows desktop.
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


        /// <summary>
        ///   Initializes a new instance of <see cref="GraphicsOutput"/>.
        /// </summary>
        /// <param name="adapter">The Graphics Adapter this output is attached to.</param>
        /// <param name="nativeOutput">
        ///   A COM pointer to the native <see cref="IDXGIOutput"/> interface.
        ///   The ownership is transferred to this instance, so the reference count is not incremented.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> is <see langword="null"/>.</exception>
        internal GraphicsOutput(GraphicsAdapter adapter, ComPtr<IDXGIOutput> nativeOutput)
        {
            ArgumentNullException.ThrowIfNull(adapter);

            Debug.Assert(nativeOutput.IsNotNull());

            dxgiOutput = nativeOutput;
            dxgiOutputVersion = GetLatestDxgiOutputVersion(dxgiOutput);

            Adapter = adapter;

            Unsafe.SkipInit(out OutputDesc outputDesc);
            HResult result = nativeOutput.GetDesc(ref outputDesc);

            if (result.IsFailure)
                result.Throw();

            Name = StringMarshal.GetString(outputDesc.DeviceName)!;
            Description = GetFriendlyName(outputDesc.DeviceName) ?? Name;

            ref var rectangle = ref outputDesc.DesktopCoordinates;
            DesktopBounds = new Rectangle
            {
                Location = rectangle.Min.BitCast<Vector2D<int>, Point>(),
                Width = rectangle.Size.X,
                Height = rectangle.Size.Y
            };

            MonitorHandle = outputDesc.Monitor;
            IsAttached = outputDesc.AttachedToDesktop;
            Rotation = outputDesc.Rotation switch
            {
                ModeRotation.Rotate90 => DisplayRotation.Rotate90,
                ModeRotation.Rotate180 => DisplayRotation.Rotate180,
                ModeRotation.Rotate270 => DisplayRotation.Rotate270,
                _ => DisplayRotation.Default
            };

            if (dxgiOutputVersion >= 6)
            {
                var nativeOutput6 = nativeOutput.AsComPtrUnsafe<IDXGIOutput, IDXGIOutput6>();
                Unsafe.SkipInit(out OutputDesc1 outputDesc1);
                result = nativeOutput6.GetDesc1(ref outputDesc1);

                ColorSpace = (ColorSpaceType) outputDesc1.ColorSpace;
                BitsPerChannel = (int) outputDesc1.BitsPerColor;

                MinLuminance = outputDesc1.MinLuminance;
                MaxLuminance = outputDesc1.MaxLuminance;
                MaxFullFrameLuminance = outputDesc1.MaxFullFrameLuminance;

                WhitePoint = *(Vector2*) outputDesc1.WhitePoint;
                RedPrimary = *(Vector2*) outputDesc1.RedPrimary;
                GreenPrimary = *(Vector2*) outputDesc1.GreenPrimary;
                BluePrimary = *(Vector2*) outputDesc1.BluePrimary;
            }
            else
            {
                // Default values for outputs that do not support IDXGIOutput6 (pre-Windows 10)
                ColorSpace = ColorSpaceType.Rgb_Full_G22_None_P709;
                BitsPerChannel = 8;
                MinLuminance = 0.0f;
                MaxLuminance = 100.0f;
                MaxFullFrameLuminance = 100.0f;
                WhitePoint = new Vector2(0.3127f, 0.3290f);
                RedPrimary = new Vector2(0.6400f, 0.3300f);
                GreenPrimary = new Vector2(0.3000f, 0.6000f);
                BluePrimary = new Vector2(0.1500f, 0.0600f);
            }

            //
            // Queries the latest DXGI output version supported.
            //
            static uint GetLatestDxgiOutputVersion(IDXGIOutput* output)
            {
                uint outputVersion;

                if (((HResult) output->QueryInterface<IDXGIOutput6>(out _)).IsSuccess)
                {
                    outputVersion = 6;
                    output->Release();
                }
                else if (((HResult) output->QueryInterface<IDXGIOutput5>(out _)).IsSuccess)
                {
                    outputVersion = 5;
                    output->Release();
                }
                else if (((HResult) output->QueryInterface<IDXGIOutput4>(out _)).IsSuccess)
                {
                    outputVersion = 4;
                    output->Release();
                }
                else if (((HResult) output->QueryInterface<IDXGIOutput3>(out _)).IsSuccess)
                {
                    outputVersion = 3;
                    output->Release();
                }
                else if (((HResult) output->QueryInterface<IDXGIOutput2>(out _)).IsSuccess)
                {
                    outputVersion = 2;
                    output->Release();
                }
                else if (((HResult) output->QueryInterface<IDXGIOutput1>(out _)).IsSuccess)
                {
                    outputVersion = 1;
                    output->Release();
                }
                else
                {
                    outputVersion = 0;
                }

                return outputVersion;
            }

            //
            // Attempts to get a friendly name for the output using Win32 API.
            //
            static string GetFriendlyName(char* deviceName)
            {
                Win32.DISPLAY_DEVICEW displayDevice = default;
                displayDevice.cb = (uint) sizeof(Win32.DISPLAY_DEVICEW);

                if (Win32.EnumDisplayDevicesW(deviceName, iDevNum: 0, &displayDevice, dwFlags: 0))
                {
                    return StringMarshal.GetString(&displayDevice.DeviceString.e0);
                }
                return null;
            }
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            base.Destroy();

            ComPtrHelpers.SafeRelease(ref dxgiOutput);
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
        /// <exception cref="ArgumentNullException"><paramref name="targetProfiles"/> is empty and does not specify any graphics profile to test.</exception>
        /// <exception cref="InvalidOperationException">
        ///   Coult not create a device with any of the profiles specified in <paramref name="targetProfiles"/>.
        /// </exception>
        /// <remarks>
        ///   Direct3D devices require UNORM pixel formats.
        ///   <para>
        ///     Unspecified fields are lower priority than specified fields and will be resolved later than specified fields.
        ///     Similarly ranked fields (i.e. all specified, or all unspecified, etc.) are resolved in the following order: <c>Format</c>, <c>Width</c>, <c>Height</c>, <c>RefreshRate</c>.
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
        public DisplayMode FindClosestMatchingDisplayMode(ReadOnlySpan<GraphicsProfile> targetProfiles, DisplayMode modeToMatch)
        {
            if (targetProfiles.IsEmpty)
                throw new ArgumentNullException(nameof(targetProfiles));

            var d3d12 = D3D12.GetApi();

            // NOTE: Assume the same underlying integer type
            Debug.Assert(sizeof(GraphicsProfile) == sizeof(D3DFeatureLevel));
            var featureLevels = targetProfiles.Cast<GraphicsProfile, D3DFeatureLevel>();

            HResult result = default;

            var nativeAdapter = Adapter.NativeAdapter.AsIUnknown();
            ComPtr<ID3D12Device> deviceTemp = null;

            for (int i = 0; i < featureLevels.Length; i++)
            {
                var featureLevelToTry = featureLevels[i];

                // Create Device D3D12 with feature Level based on profile
                result = d3d12.CreateDevice(nativeAdapter, featureLevelToTry, out deviceTemp);

                if (result.IsSuccess)
                    break;
            }

            if (deviceTemp.IsNull() && result.IsFailure)
                ThrowNoCompatibleProfile(result, Adapter, targetProfiles);

            Unsafe.SkipInit(out ModeDesc closestDescription);
            ModeDesc modeDescription = new()
            {
                Width = (uint) modeToMatch.Width,
                Height = (uint) modeToMatch.Height,
                RefreshRate = modeToMatch.RefreshRate.ToSilk(),
                Format = (Format) modeToMatch.Format,
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            result = dxgiOutput->FindClosestMatchingMode(in modeDescription, ref closestDescription, deviceTemp);

            if (result.IsFailure)
                result.Throw();

            deviceTemp.Release();

            return DisplayMode.FromDescription(in closestDescription);

            //
            // Logs and throws an exception reporting that no compatible profile was found among the specified ones.
            //
            [DoesNotReturn]
            static void ThrowNoCompatibleProfile(HResult result, GraphicsAdapter adapter, ReadOnlySpan<GraphicsProfile> targetProfiles)
            {
                var exception = Marshal.GetExceptionForHR(result.Value)!;
                Log.Error($"Failed to create Direct3D device using adapter '{adapter.Description}' with profiles: " +
                          $"{string.Join(", ", targetProfiles.ToArray())}.\nException: {exception}");
                throw exception;
            }
        }

        /// <summary>
        ///   Enumerates all available display modes for this output and stores them in <see cref="SupportedDisplayModes"/>.
        /// </summary>
        private void InitializeSupportedDisplayModes()
        {
            HResult result = default;

            var modesAvailable = new List<DisplayMode>();
            var knownModes = new Dictionary<int, DisplayMode>();

#if DIRECTX11_1
            using ComPtr<IDXGIOutput1> output1 = dxgiOutput->QueryInterface<IDXGIOutput1>();
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
                result = dxgiOutput->GetDisplayModeList(format, DisplayModeEnumerationFlags, ref displayModeCount, null);
#endif
                if (result.IsFailure && result.Code != DxgiConstants.ErrorNotCurrentlyAvailable)
                    result.Throw();
                if (displayModeCount == 0)
                    continue;

#if DIRECTX11_1
                Span<ModeDesc1> displayModes = stackalloc ModeDesc1[(int) displayModeCount];
                result = output1.GetDisplayModeList1(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes.GetReference());
#else
                Span<ModeDesc> displayModes = stackalloc ModeDesc[(int) displayModeCount];
                result = dxgiOutput->GetDisplayModeList(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes.GetReference());
#endif

                for (int i = 0; i < displayModeCount; i++)
                {
                    ref var mode = ref displayModes[i];

                    if (mode.Scaling != ModeScaling.Unspecified)
                        continue;

                    var modeKey = HashCode.Combine(format, mode.Width, mode.Height, mode.RefreshRate.Numerator, mode.RefreshRate.Denominator);

                    if (!knownModes.ContainsKey(modeKey))
                    {
                        var displayMode = DisplayMode.FromDescription(in mode);

                        knownModes.Add(modeKey, displayMode);
                        modesAvailable.Add(displayMode);
                    }
                }
            }

            supportedDisplayModes = modesAvailable.ToArray();
        }

        /// <summary>
        ///   Initializes <see cref="CurrentDisplayMode"/> with the current <see cref="DisplayMode"/>,
        ///   the closest matching mode with the common formats <see cref="PixelFormat.R8G8B8A8_UNorm"/> or <see cref="PixelFormat.B8G8R8A8_UNorm"/>),
        ///   or <see langword="null"/> in no matching mode could be found.
        /// </summary>
        private void InitializeCurrentDisplayMode()
        {
            currentDisplayMode = GetCurrentDisplayMode() ??
                                 TryFindMatchingDisplayMode(Format.FormatR8G8B8A8Unorm) ??
                                 TryFindMatchingDisplayMode(Format.FormatB8G8R8A8Unorm);
        }

        /// <summary>
        ///   Tries to get the current <see cref="DisplayMode"/> based on the <see cref="DesktopBounds"/>.
        /// </summary>
        /// <returns>The current <see cref="DisplayMode"/> of the output, or <see langword="null"/> if couldn't be determined.</returns>
        private DisplayMode? GetCurrentDisplayMode()
        {
            var d3d12 = D3D12.GetApi();

            // Try to create a dummy ID3D12Device with no consideration to Graphics Profiles, etc.
            // We only want to get missing information about the current display irrespective of graphics profiles
            var unspecifiedAdapter = ComPtrHelpers.NullComPtr<IUnknown>();
            D3DFeatureLevel selectedLevel = 0;
            HResult result = d3d12.CreateDevice(unspecifiedAdapter, selectedLevel, out ComPtr<ID3D12Device> deviceTemp);

            if (result.IsFailure)
            {
                var exception = Marshal.GetExceptionForHR(result.Value)!;
                Log.Error($"Failed to create Direct3D device using adapter '{Adapter.Description}'.\nException: {exception}");
                return null;
            }

            Unsafe.SkipInit(out ModeDesc closestMatch);
            var modeDesc = new ModeDesc
            {
                Width = (uint) DesktopBounds.Width,
                Height = (uint) DesktopBounds.Height,
                // Format and RefreshRate will be automatically filled if we pass reference to the Direct3D 12 device
                Format = Format.FormatUnknown
            };

            result = dxgiOutput->FindClosestMatchingMode(in modeDesc, ref closestMatch, deviceTemp);

            if (result.IsFailure)
            {
                var exception = Marshal.GetExceptionForHR(result.Value)!;
                Log.Error($"Failed to get current display mode. The resolution ({modeDesc.Width}x{modeDesc.Height}) " +
                          $"taken from the output is not correct.\nException: {exception}");
                return null;
            }

            return DisplayMode.FromDescription(in closestMatch);
        }

        /// <summary>
        ///   Tries to find a display mode with the specified format that has the same size as the current desktop size
        ///   of this <see cref="GraphicsOutput"/>.
        /// </summary>
        /// <param name="format">The format to match with.</param>
        /// <returns>A matched <see cref="DisplayMode"/>, or <see langword="null"/> if nothing is found.</returns>
        private DisplayMode? TryFindMatchingDisplayMode(Format format)
        {
            var desktopBounds = DesktopBounds;
            var width = desktopBounds.Width;
            var height = desktopBounds.Height;

            foreach (var supportedDisplayMode in SupportedDisplayModes)
            {
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
