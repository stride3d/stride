// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Presentation;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The built-in plugins of the cross-platform editor register what they registered before they moved to the plugin
/// context: the recorded registrations are in the <c>Registrations</c> folder.
/// </summary>
public sealed class PluginRegistrationTests
{
    [Fact]
    public void TheDefaultAssetsPluginRegistersTheSameAsBefore()
    {
        AssertEditorRegistrations(new StrideDefaultAssetsPlugin());
    }

    [Fact]
    public void TheEditorPluginRegistersTheSameAsBefore()
    {
        AssertEditorRegistrations(new StrideEditorPlugin());
    }

    [Fact]
    public async Task TheEditorPluginInitializesTheSessionAsBefore()
    {
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("GameSettings", gameSettings));

        var registrations = SessionRegistrations.Of(new StrideEditorPlugin { EnableThumbnailService = false }, fixture.Session);

        // The builder imports the shaders of the session: it must be done before the fixture disposes it
        await fixture.ServiceProvider.Get<GameStudioBuilderService>().WaitForShadersAsync().WaitAsync(TimeSpan.FromSeconds(30));
        AssertSame(typeof(StrideEditorPlugin).FullName + ".Session", registrations.Describe());
    }

    private static void AssertEditorRegistrations(AssetsPlugin plugin)
    {
        AssertSame(plugin.GetType().FullName!, PluginRegistrations.Of(plugin, typeof(IAssetEditorView)).Describe());
    }

    private static void AssertSame(string name, string registrations)
    {
        if (RegistrationsGolden.Compare(name, registrations) is { } difference)
            Assert.Fail(difference);
    }
}
