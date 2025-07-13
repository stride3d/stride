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
        private readonly uint outputIndex;

        private readonly OutputDesc outputDescription;

        /// <summary>
        ///   Gets the native DXGI output.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<IDXGIOutput> NativeOutput => ComPtrHelpers.ToComPtr(dxgiOutput);

        /// <summary>
        ///   Gets the handle of the monitor associated with this <see cref="GraphicsOutput"/>.
        /// </summary>
        public nint MonitorHandle => outputDescription.Monitor;


        /// <summary>
        ///   Initializes a new instance of <see cref="GraphicsOutput"/>.
        /// </summary>
        /// <param name="adapter">The Graphics Adapter this output is attached to.</param>
        /// <param name="nativeOutput">
        ///   A COM pointer to the native <see cref="IDXGIOutput"/> interface.
        ///   The ownership is transferred to this instance, so the reference count is not incremented.
        /// </param>
        /// <param name="outputIndex">The index of the output.</param>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> is <see langword="null"/>.</exception>
        internal GraphicsOutput(GraphicsAdapter adapter, ComPtr<IDXGIOutput> nativeOutput, uint outputIndex)
        {
            ArgumentNullException.ThrowIfNull(adapter);

            Debug.Assert(nativeOutput.IsNotNull());

            this.outputIndex = outputIndex;
            dxgiOutput = nativeOutput;

            Adapter = adapter;

            Unsafe.SkipInit(out OutputDesc outputDesc);
            HResult result = nativeOutput.GetDesc(ref outputDesc);

            if (result.IsFailure)
                result.Throw();

            Name = SilkMarshal.PtrToString((nint) outputDesc.DeviceName, NativeStringEncoding.LPWStr);

            ref var rectangle = ref outputDesc.DesktopCoordinates;
            DesktopBounds = new Rectangle
            {
                Location = rectangle.Min.BitCast<Vector2D<int>, Point>(),
                Width = rectangle.Size.X,
                Height = rectangle.Size.Y
            };

            outputDescription = outputDesc;
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

            /// <summary>
            ///   Logs and throws an exception reporting that no compatible profile was found among the specified ones.
            /// </summary>
            [DoesNotReturn]
            static void ThrowNoCompatibleProfile(HResult result, GraphicsAdapter adapter, ReadOnlySpan<GraphicsProfile> targetProfiles)
            {
                var exception = Marshal.GetExceptionForHR(result.Value)!;
                Log.Error($"Failed to create Direct3D device using adapter '{adapter.Description}' with profiles: {string.Join(", ", targetProfiles.ToArray())}.\nException: {exception}");
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
                result = output1.GetDisplayModeList1(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes[0]);
#else
                Span<ModeDesc> displayModes = stackalloc ModeDesc[(int) displayModeCount];
                result = dxgiOutput->GetDisplayModeList(format, DisplayModeEnumerationFlags, ref displayModeCount, ref displayModes[0]);
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
