// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Audio.Editor;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Particles.Editor;
using Stride.Physics.Editor;
using Stride.SpriteStudio.Editor;
using Stride.Video.Editor;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The built-in plugins of the Avalonia editor and of the plugin packages register what they registered before they moved
/// to the plugin context: the recorded registrations are in the <c>Registrations</c> folder.
/// </summary>
public sealed class PluginRegistrationTests
{
    public static TheoryData<string> Plugins =>
    [
        nameof(StrideCoreEditorViewPlugin),
        nameof(StrideEditorViewPlugin),
        nameof(AudioEditorAvaloniaPlugin),
        nameof(SpriteStudioEditorAvaloniaPlugin),
        nameof(AudioEditorPlugin),
        nameof(ParticlesEditorPlugin),
        nameof(PhysicsEditorPlugin),
        nameof(SpriteStudioEditorPlugin),
        nameof(VideoEditorPlugin),
    ];

    /// <summary>
    /// The plugins that need no more of a session than its services and property grid to initialize it.
    /// </summary>
    public static TheoryData<string> SessionPlugins =>
    [
        nameof(StrideCoreEditorViewPlugin),
        nameof(AudioEditorAvaloniaPlugin),
        nameof(SpriteStudioEditorAvaloniaPlugin),
        nameof(AudioEditorPlugin),
        nameof(ParticlesEditorPlugin),
        nameof(PhysicsEditorPlugin),
        nameof(SpriteStudioEditorPlugin),
        nameof(VideoEditorPlugin),
    ];

    [AvaloniaTheory]
    [MemberData(nameof(Plugins))]
    public void ThePluginRegistersTheSameAsBefore(string name)
    {
        var plugin = Create(name);

        AssertSame(plugin.GetType().FullName!, PluginRegistrations.Of(plugin, typeof(IAssetEditorView)).Describe());
    }

    [AvaloniaTheory]
    [MemberData(nameof(SessionPlugins))]
    public void ThePluginInitializesTheSessionAsBefore(string name)
    {
        var plugin = Create(name);

        var registrations = SessionRegistrations.Of(plugin, SessionRegistrations.CreateSession());

        AssertSame(plugin.GetType().FullName + ".Session", registrations.Describe());
    }

    private static AssetsPlugin Create(string name) => name switch
    {
        nameof(StrideCoreEditorViewPlugin) => new StrideCoreEditorViewPlugin(),
        nameof(StrideEditorViewPlugin) => new StrideEditorViewPlugin(),
        nameof(AudioEditorAvaloniaPlugin) => new AudioEditorAvaloniaPlugin(),
        nameof(SpriteStudioEditorAvaloniaPlugin) => new SpriteStudioEditorAvaloniaPlugin(),
        nameof(AudioEditorPlugin) => new AudioEditorPlugin(),
        nameof(ParticlesEditorPlugin) => new ParticlesEditorPlugin(),
        nameof(PhysicsEditorPlugin) => new PhysicsEditorPlugin(),
        nameof(SpriteStudioEditorPlugin) => new SpriteStudioEditorPlugin(),
        nameof(VideoEditorPlugin) => new VideoEditorPlugin(),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private static void AssertSame(string name, string registrations)
    {
        if (RegistrationsGolden.Compare(name, registrations) is { } difference)
            Assert.Fail(difference);
    }
}
