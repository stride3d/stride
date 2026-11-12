// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The command registry: what a command says about itself (its category, its keywords, the palette, a toggle's state).
/// </summary>
public sealed class TestCommandRegistry
{
    private class SceneEditor;

    private sealed class SpecialSceneEditor : SceneEditor;

    private static CommandDescriptor Command(string id, string? gesture = null, Func<CommandContext, bool>? isChecked = null, Type? scope = null) => new()
    {
        Id = id,
        Title = () => id,
        DefaultGesture = gesture,
        IsChecked = isChecked,
        GestureScope = scope,
        ExecuteAsync = _ => Task.CompletedTask,
    };

    private static CommandContext Context(string location = CommandLocations.MainMenuTools) => new()
    {
        // The guard and the registry never read the session
        Session = null!,
        Location = location,
    };

    private static (RecordingEditorPluginHost Host, LoggerResult Log) Open(params AssetsPlugin[] plugins)
    {
        // The plugins of the tests are not the editor's own
        var host = new RecordingEditorPluginHost(plugins) { PluginViewBaseType = typeof(object), IsEditorAssembly = _ => false };
        var session = new FakeSession();
        session.ServiceProvider.RegisterService(host);
        var log = new LoggerResult();
        host.RegisterSession(session, log);
        return (host, log);
    }

    [Fact]
    public void ACommandIsInThePaletteWithNoCategoryNorKeywordsByDefault()
    {
        var command = Command("Tests.Plain");

        Assert.True(command.ShowInPalette);
        Assert.Null(command.Category);
        Assert.Empty(command.Keywords);
        Assert.Null(command.GestureScope);
        Assert.Null(command.IsChecked);
    }

    [Fact]
    public void AToggleSaysWhetherItIsOnAndAToggleThatFailsIsTurnedOff()
    {
        var on = true;
        var toggle = Command("Tests.Toggle", isChecked: _ => on);
        var plain = Command("Tests.Plain");
        var failing = Command("Tests.Failing", isChecked: _ => throw new InvalidOperationException("Broken toggle"));
        var plugin = new TestPluginCommands.CommandPlugin { Commands = { toggle, plain, failing } };
        var (host, log) = Open(plugin);
        try
        {
            Assert.True(host.IsCommandChecked(toggle, Context()));
            on = false;
            Assert.False(host.IsCommandChecked(toggle, Context()));
            // Not a toggle
            Assert.Null(host.IsCommandChecked(plain, Context()));

            // A state that throws is off, and turns the command off, with the plugin's name
            Assert.False(host.IsCommandChecked(failing, Context()));
            Assert.False(host.IsCommandAvailable(failing));
            Assert.True(host.IsCommandAvailable(toggle));
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("Tests.Failing"));
        }
        finally
        {
            host.Dispose();
        }
    }
}
