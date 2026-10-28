// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Headless.XUnit;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Avalonia.Converters;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Reflection;
using Stride.Core.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.Particles.Components;
using Stride.Particles.Editor;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// What the plugins declare with attributes reaches the cross-platform editor as it reaches the WPF one: the settings keys
/// of <see cref="EditorSettingsAttribute"/> classes and the images of <see cref="TypeImageAttribute"/> and <see cref="EnumImageAttribute"/>.
/// </summary>
public sealed class PluginContextTests
{
    [AvaloniaFact]
    public async Task ThePluginsSettingsKeysAreRegisteredWhileTheEditorIsOpen()
    {
        using (var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset())))
        {
            Assert.Contains(fixture.Plugins.SessionPlugins, x => x is ShellTestPlugin);
            Assert.Contains(ShellTestSettings.Key, fixture.Plugins.SettingsKeys);
            Assert.Same(ShellTestSettings.Key, ShellTestSettings.Container.GetSettingsKey(ShellTestSettings.Key.Name));
        }

        // The editor closed its session: the keys went with the plugins
        Assert.Null(ShellTestSettings.Container.GetSettingsKey(ShellTestSettings.Key.Name));
    }

    [AvaloniaFact]
    public async Task ADeclaredEnumImageIsShown()
    {
        // A plugin package loaded by the session registers its assembly as an asset assembly: its plugin joins the open editor
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var assembly = typeof(ParticlesEditorPlugin).Assembly;
        AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
        try
        {
            Assert.Contains(fixture.Plugins.SessionPlugins, x => x is ParticlesEditorPlugin);

            Assert.True(fixture.Plugins.HasImagesForEnum(fixture.Session, typeof(StateControl)));
            var image = new EnumToResource().Convert(StateControl.Play, typeof(object), null, CultureInfo.InvariantCulture);
            Assert.IsType<AvaloniaBitmap>(image);
            Assert.NotSame(image, new EnumToResource().Convert(StateControl.Stop, typeof(object), null, CultureInfo.InvariantCulture));

            // The type images too, for the type and the types that derive from it
            Assert.IsType<AvaloniaBitmap>(TypeToResource.FetchResourceFromType(typeof(ParticleSystemComponent), true));
        }
        finally
        {
            AssemblyRegistry.Unregister(assembly);
        }

        // The assembly is unloaded: its plugin and its images left the editor
        Assert.DoesNotContain(fixture.Plugins.SessionPlugins, x => x is ParticlesEditorPlugin);
        Assert.False(fixture.Plugins.HasImagesForEnum(fixture.Session, typeof(StateControl)));
    }
}

/// <summary>
/// A plugin of the test assembly (an asset assembly), so that its declarations join the editor.
/// </summary>
public sealed class ShellTestPlugin : AssetsPlugin;

[EditorSettings]
public static class ShellTestSettings
{
    public static readonly SettingsContainer Container = new();

    public static readonly SettingsKey<bool> Key = new("Test/Shell/PluginKey", Container, true);
}
