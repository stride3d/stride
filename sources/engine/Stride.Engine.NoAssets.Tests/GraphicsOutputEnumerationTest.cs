// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;

namespace Stride.Engine.Tests;

/// <summary>
/// Desktop integration regressions for #3396. Run with a real Windows desktop and
/// Vulkan, and also with Direct3D as a reference. No screenshot baseline is needed.
/// </summary>
[Collection("Windowing")]
public class GraphicsOutputEnumerationTest : GameTestBase
{
    [SkippableTheory]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void OutputsDescribeRealDesktopMonitors(AppContextType contextType)
    {
        WithDesktopWindow(contextType, () =>
        {
            var monitors = GetDesktopMonitors();
            var outputCount = 0;
            foreach (var adapter in GraphicsAdapterFactory.Adapters)
            {
                if (adapter.IsSoftwareAdapter)
                    continue;

                foreach (var output in adapter.Outputs)
                {
                    outputCount++;
                    Assert.Same(adapter, output.Adapter);
                    Assert.True(output.DesktopBounds.Width > 0 && output.DesktopBounds.Height > 0,
                        $"{adapter.Description}: output has empty desktop bounds.");
                    Assert.Contains(output.DesktopBounds, monitors);
                    // Mirrored monitors can legitimately share desktop bounds.
                }
            }
            // Do not require an output on every GPU: a hybrid laptop's discrete GPU
            // can render through an integrated GPU with the only connected display.
            Assert.True(outputCount > 0, "No real outputs were found on any hardware adapter.");
        });
    }

    [SkippableTheory]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void OutputsProvideModesForResolutionSettings(AppContextType contextType)
    {
        WithDesktopWindow(contextType, () =>
        {
            var outputCount = 0;
            foreach (var adapter in GraphicsAdapterFactory.Adapters)
            {
                if (adapter.IsSoftwareAdapter)
                    continue;

                foreach (var output in adapter.Outputs)
                {
                    outputCount++;
                    var current = output.CurrentDisplayMode;
                    Assert.True(current.HasValue, $"{adapter.Description}: current display mode is missing.");
                    AssertUsableMode(current.Value);
                    Assert.False(output.SupportedDisplayModes.IsEmpty,
                        $"{adapter.Description}: resolution settings have no display modes.");

                    var modesSeen = new HashSet<DisplayMode>();
                    foreach (var mode in output.SupportedDisplayModes)
                    {
                        AssertUsableMode(mode);
                        Assert.True(modesSeen.Add(mode), "An identical display mode was enumerated twice.");
                    }
                }
            }
            Assert.True(outputCount > 0, "No hardware outputs were available to query.");
        });
    }

    [SkippableTheory]
    [InlineData(AppContextType.DesktopWinForms, PixelFormat.B8G8R8A8_UNorm_SRgb)]
    [InlineData(AppContextType.DesktopSDL, PixelFormat.B8G8R8A8_UNorm_SRgb)]
    [InlineData(AppContextType.DesktopSDL, PixelFormat.R10G10B10A2_UNorm)]
    [InlineData(AppContextType.DesktopSDL, PixelFormat.R16G16B16A16_Float)]
    public void VulkanFullscreenKeepsRequestedBackBufferFormat(AppContextType contextType, PixelFormat format)
    {
        // The Vulkan swapchain selects its surface format, so matching must not replace it.
        Skip.If(GraphicsDevice.Platform != GraphicsPlatform.Vulkan, "Vulkan keeps the requested format; DXGI matches it.");
        WithDesktopWindow(contextType, () =>
        {
            var outputCount = 0;
            foreach (var adapter in GraphicsAdapterFactory.Adapters)
            {
                if (adapter.IsSoftwareAdapter)
                    continue;

                foreach (var output in adapter.Outputs)
                {
                    outputCount++;
                    var request = new DisplayMode(format, 0, 0, new Rational(0, 1));
                    var matched = output.FindClosestMatchingDisplayMode([GraphicsProfile.Level_11_0], request);
                    AssertUsableMode(matched);
                    Assert.Equal(format, matched.Format);
                }
            }
            Assert.True(outputCount > 0, "No hardware outputs were available for mode matching.");
        });
    }

    [SkippableTheory]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void UnspecifiedFullscreenSizeResolvesToSupportedMode(AppContextType contextType)
    {
        WithDesktopWindow(contextType, () =>
        {
            var outputCount = 0;
            foreach (var adapter in GraphicsAdapterFactory.Adapters)
            {
                if (adapter.IsSoftwareAdapter)
                    continue;

                foreach (var output in adapter.Outputs)
                {
                    outputCount++;
                    Assert.False(output.SupportedDisplayModes.IsEmpty, "No modes available for fullscreen selection.");
                    var format = output.SupportedDisplayModes[0].Format;
                    var request = new DisplayMode(format, 0, 0, new Rational(0, 1));
                    var matched = output.FindClosestMatchingDisplayMode([GraphicsProfile.Level_11_0], request);
                    AssertUsableMode(matched);
                    Assert.Equal(format, matched.Format);
                    Assert.Contains(matched, output.SupportedDisplayModes.ToArray());
                }
            }
            Assert.True(outputCount > 0, "No hardware outputs were available for mode matching.");
        });
    }

    [SkippableTheory]
    [InlineData(AppContextType.DesktopSDL, AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopWinForms, AppContextType.DesktopSDL)]
    public void OutputSnapshotsDoNotDependOnWindowCreationOrder(AppContextType firstType, AppContextType secondType)
    {
        Skip.If(Platform.Type != PlatformType.Windows, "Exercises both Windows window backends.");
        Skip.If(GetDesktopMonitors().Count == 0, "A real desktop monitor is required.");

        PerformTest(game =>
        {
            var adapter = GraphicsAdapterFactory.DefaultAdapter;
            var before = adapter.Outputs.ToArray();
            using var first = new GameWindowRenderer(game.Services,
                GameContextFactory.NewGameContext(firstType, 320, 240, isUserManagingRun: true));
            first.Initialize();
            using var second = new GameWindowRenderer(game.Services,
                GameContextFactory.NewGameContext(secondType, 320, 240, isUserManagingRun: true));
            second.Initialize();

            Assert.Equal(before.Length, adapter.Outputs.Length);
            for (int i = 0; i < before.Length; i++)
                Assert.Same(before[i], adapter.Outputs[i]);
        });
    }

    private void WithDesktopWindow(AppContextType contextType, Action assertion)
    {
        Skip.If(Platform.Type != PlatformType.Windows,
            "These tests use Windows monitor enumeration as an independent desktop oracle.");
        Skip.If(Environment.GetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING") == "1",
            "Hardware monitor assertions do not apply when software rendering is explicitly selected.");
        Skip.If(GetDesktopMonitors().Count == 0, "A real desktop monitor is required.");

        PerformTest(game =>
        {
            var context = GameContextFactory.NewGameContext(contextType, 320, 240, isUserManagingRun: true);
            using var renderer = new GameWindowRenderer(game.Services, context);
            renderer.Initialize();
            assertion();
        });
    }

    private static void AssertUsableMode(DisplayMode mode)
    {
        Assert.True(mode.Width > 0 && mode.Height > 0, "Display mode dimensions must be positive.");
        Assert.NotEqual(PixelFormat.None, mode.Format);
        // Zero numerator is allowed: SDL documents zero as an unknown refresh rate.
        // Stride's Rational also explicitly permits 0/0 as unspecified.
        Assert.True(mode.RefreshRate.Numerator >= 0 &&
            (mode.RefreshRate.Denominator > 0 ||
             (mode.RefreshRate.Numerator == 0 && mode.RefreshRate.Denominator == 0)),
            "Refresh rate must be a valid nonnegative rational.");
    }

    private static List<Rectangle> GetDesktopMonitors()
    {
        var monitors = new List<Rectangle>();
        MonitorEnum callback = (nint monitor, nint dc, ref NativeRectangle bounds, nint data) =>
        {
            monitors.Add(new Rectangle(bounds.Left, bounds.Top,
                bounds.Right - bounds.Left, bounds.Bottom - bounds.Top));
            return true;
        };
        Assert.True(EnumDisplayMonitors(0, 0, callback, 0), "Windows monitor enumeration failed.");
        return monitors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left, Top, Right, Bottom;
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool MonitorEnum(nint monitor, nint dc, ref NativeRectangle bounds, nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint dc, nint clip, MonitorEnum callback, nint data);
}
