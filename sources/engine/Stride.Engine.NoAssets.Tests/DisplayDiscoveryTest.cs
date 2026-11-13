// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using Xunit;
using Stride.Core;
using Stride.Graphics;
using Stride.Games;
using Stride.Graphics.Regression;

namespace Stride.Engine.Tests;

[Collection("Windowing")]
public class DisplayDiscoveryTest : GameTestBase
{
    // Run this case alone in a fresh test process: the window tests intentionally
    // leave the global SDL video subsystem initialized.
    [SkippableFact]
    public void AdapterDiscoveryBeforeWindowDoesNotInitializeSdl()
    {
        Skip.If(SdlDisplayProvider.IsVideoInitialized, "SDL video was already initialized by another window test.");
        Assert.Null(SdlDisplayProvider.Displays);
        Assert.Empty(SdlDisplayProvider.Enumerate());
        Assert.False(GraphicsAdapterFactory.Adapters.IsEmpty);
        foreach (var adapter in GraphicsAdapterFactory.Adapters)
            _ = adapter.Outputs.Length;
        Assert.False(SdlDisplayProvider.IsVideoInitialized);
        Assert.Null(SdlDisplayProvider.Displays);
    }

    [SkippableFact]
    public void SdlWindowInitializationPublishesDisplaySnapshot()
    {
        // AppKit needs the main thread; mobile SDL needs its platform application host.
        Skip.If(Platform.Type != PlatformType.Windows && Platform.Type != PlatformType.Linux,
            "Desktop SDL window creation is supported by this test host only on Windows and Linux.");

        PerformTest(game =>
        {
            using var renderer = new GameWindowRenderer(game.Services,
                GameContextFactory.NewGameContext(AppContextType.DesktopSDL, 320, 240, isUserManagingRun: true));
            renderer.Initialize();
            Assert.True(SdlDisplayProvider.IsVideoInitialized);
            Assert.NotNull(SdlDisplayProvider.Displays);
            Assert.NotEmpty(SdlDisplayProvider.Displays);
            foreach (var display in SdlDisplayProvider.Displays)
            {
                Assert.True(display.DesktopBounds.Width > 0 && display.DesktopBounds.Height > 0);
                Assert.True(display.CurrentDisplayMode.HasValue);
                Assert.NotEmpty(display.SupportedDisplayModes);
            }
            // Snapshot publication is independent of attached hardware outputs (e.g. WARP).
            // Hardware output availability is covered by GraphicsOutputEnumerationTest.
        });
    }
}
#endif
