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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

using Stride.Core.UnsafeExtensions;

namespace Stride.Graphics;

// NOTE: This partial class is the Direct3D 11 implementation of GraphicsOutput.
//       The part that is common with Direct3D12 is in GraphicsOutput.Direct3D.cs

public unsafe partial class GraphicsOutput
{
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
        var featureLevels = targetProfiles.As<GraphicsProfile, D3DFeatureLevel>();

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
        deviceTemp.Release();

        if (result.IsFailure)
        {
            var exception = Marshal.GetExceptionForHR(result.Value)!;
            Log.Error($"Failed to get current display mode. The resolution ({modeDesc.Width}x{modeDesc.Height}) " +
                      $"taken from the output is not correct.\nException: {exception}");
            return null;
        }

        return DisplayMode.FromDescription(in closestMatch);
    }
}

#endif
