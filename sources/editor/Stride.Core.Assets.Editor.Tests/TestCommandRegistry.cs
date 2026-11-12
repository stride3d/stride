// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The command registry: what a command says about itself (its category, its keywords, the palette, a toggle's state), and
/// which command each shortcut runs (the user's overrides, then the editor's defaults, then the plugins').
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

    private static Dictionary<string, string> Overrides(params (string Id, string Gesture)[] overrides) => overrides.ToDictionary(x => x.Id, x => x.Gesture);

    private static List<(string, string)> Describe(CommandGestureBindings bindings) => bindings.Bindings.Select(x => (x.Gesture.ToString(), x.Command.Id)).ToList();

    [Fact]
    public void TheUserComesFirstThenTheEditorThenThePlugins()
    {
        // In registration order: a plugin's command first, then the editor's, then another plugin's
        var plugin = Command("Plugin.Save", "Ctrl+S");
        var save = Command("Stride.File.Save", "Ctrl+S");
        var build = Command("Stride.Project.Build", "F6");
        var other = Command("Other.Build", "F6");
        var builtIns = new HashSet<CommandDescriptor> { save, build };

        var bindings = CommandGestureBindings.Resolve([plugin, save, build, other], builtIns.Contains);

        // The editor's defaults beat the plugins' registered before them
        Assert.Equal([("Ctrl+S", "Stride.File.Save"), ("F6", "Stride.Project.Build")], Describe(bindings));
        Assert.Equal(2, bindings.Conflicts.Count);
        var conflict = bindings.Conflicts.Single(x => x.Command == plugin);
        Assert.Equal(CommandGestureConflictReason.Taken, conflict.Reason);
        Assert.Same(save, conflict.Winner);
        Assert.Equal("Ctrl+S", conflict.Gesture);
        Assert.Contains("[Ctrl+S]", conflict.Message);
        Assert.Contains("Plugin.Save", conflict.Message);
        Assert.Contains("Stride.File.Save", conflict.Message);

        // The user's shortcut beats the editor's default: the plugin keeps Ctrl+S once the user moved Save away from it
        bindings = CommandGestureBindings.Resolve([plugin, save, build, other], builtIns.Contains, Overrides(("Stride.File.Save", "Ctrl+Alt+S"), ("Other.Build", "F6")));
        Assert.Equal([("Ctrl+S", "Plugin.Save"), ("Ctrl+Alt+S", "Stride.File.Save"), ("F6", "Other.Build")], Describe(bindings));
        conflict = Assert.Single(bindings.Conflicts);
        Assert.Same(build, conflict.Command);
        Assert.Same(other, conflict.Winner);
        Assert.Contains("a shortcut of the user", conflict.Message);
        Assert.Equal(CommandGesture.Parse("Ctrl+Alt+S"), bindings.GetGesture(save));
        Assert.Null(bindings.GetGesture(build));
    }

    [Fact]
    public void AnOverrideRebindsOrRemovesAShortcut()
    {
        var save = Command("Stride.File.Save", "Ctrl+S");
        var open = Command("Stride.File.Open", "Ctrl+O");
        var close = Command("Stride.File.Close");
        var exit = Command("Stride.File.Exit", "Alt+F4");
        CommandDescriptor[] commands = [save, open, close, exit];

        var bindings = CommandGestureBindings.Resolve(commands, _ => true, Overrides(
            ("Stride.File.Save", "None"),
            ("Stride.File.Open", ""),
            // A command that had none gets one, and Exit gets Save's
            ("Stride.File.Close", "ctrl+w"),
            ("Stride.File.Exit", "Ctrl+S"),
            // A command that is not registered (its plugin is not loaded) is skipped
            ("Missing.Command", "Ctrl+M")));

        Assert.Equal([("Ctrl+W", "Stride.File.Close"), ("Ctrl+S", "Stride.File.Exit")], Describe(bindings));
        Assert.Empty(bindings.Conflicts);
        Assert.Equal([save, open], bindings.RemovedByUser);
        Assert.Null(bindings.GetGesture(save));
    }

    [Fact]
    public void AWrongOverrideIsListedAndTheCommandHasNoShortcut()
    {
        var delete = Command("Stride.Edit.Delete", "Delete");
        var rename = Command("Stride.Edit.Rename", "F2");
        var save = Command("Stride.File.Save", "Ctrl+S");
        var cut = Command("Stride.Edit.Cut", "Ctrl+X");
        var copy = Command("Stride.Edit.Copy", "Ctrl+C");

        var bindings = CommandGestureBindings.Resolve([delete, rename, save, cut, copy], _ => true, Overrides(
            // Its own default, which the rule would refuse: allowed
            ("Stride.Edit.Delete", "Delete"),
            // A plain key would be taken from the text boxes
            ("Stride.Edit.Rename", "R"),
            ("Stride.File.Save", "Ctrl+Nope+S"),
            // Two overrides of the same shortcut: the first command in registration order keeps it
            ("Stride.Edit.Cut", "Ctrl+X"),
            ("Stride.Edit.Copy", "Ctrl+X")));

        Assert.Equal([("Delete", "Stride.Edit.Delete"), ("Ctrl+X", "Stride.Edit.Cut")], Describe(bindings));
        Assert.Equal(
            [("Stride.Edit.Rename", CommandGestureConflictReason.NeedsModifier, null), ("Stride.File.Save", CommandGestureConflictReason.NotAShortcut, null), ("Stride.Edit.Copy", CommandGestureConflictReason.Taken, "Stride.Edit.Cut")],
            bindings.Conflicts.Select(x => (x.Command.Id, x.Reason, x.Winner?.Id)));
    }

    [Fact]
    public void TheEditorsDefaultsMayBePlainKeysButNotThePluginsOnes()
    {
        var delete = Command("Stride.Edit.Delete", "Delete");
        var typing = Command("Plugin.Typing", "Shift+K");

        var bindings = CommandGestureBindings.Resolve([delete, typing], x => x == delete);

        Assert.Equal([("Delete", "Stride.Edit.Delete")], Describe(bindings));
        Assert.Equal(CommandGestureConflictReason.NeedsModifier, Assert.Single(bindings.Conflicts).Reason);
    }

    [Fact]
    public void AShortcutOfAnEditorGoesWithTheMainWindowsButAPluginsDoesNotTakeIt()
    {
        var save = Command("Stride.File.Save", "Ctrl+S");
        var duplicate = Command("Stride.Scene.Duplicate", "Ctrl+D", scope: typeof(SceneEditor));
        var other = Command("Stride.File.Other", "Ctrl+D");
        var snap = Command("Stride.Scene.Snap", "Ctrl+Alt+G", scope: typeof(SceneEditor));
        var pluginInScene = Command("Plugin.SceneSave", "Ctrl+S", scope: typeof(SceneEditor));
        var pluginInSceneOwn = Command("Plugin.SceneOwn", "Ctrl+Alt+P", scope: typeof(SpecialSceneEditor));
        var pluginSameScope = Command("Plugin.Snap", "Ctrl+Alt+G", scope: typeof(SceneEditor));
        var builtIns = new HashSet<CommandDescriptor> { save, duplicate, other, snap };
        CommandDescriptor[] commands = [save, duplicate, other, snap, pluginInScene, pluginInSceneOwn, pluginSameScope];

        var bindings = CommandGestureBindings.Resolve(commands, builtIns.Contains);

        // The editor's Ctrl+D in the scene editor and the main window's Ctrl+D are both bound
        Assert.Equal([("Ctrl+S", "Stride.File.Save"), ("Ctrl+D", "Stride.Scene.Duplicate"), ("Ctrl+D", "Stride.File.Other"), ("Ctrl+Alt+G", "Stride.Scene.Snap"), ("Ctrl+Alt+P", "Plugin.SceneOwn")], Describe(bindings));
        Assert.Equal(
            [("Plugin.SceneSave", CommandGestureConflictReason.UsedByMainWindow, "Stride.File.Save"), ("Plugin.Snap", CommandGestureConflictReason.Taken, "Stride.Scene.Snap")],
            bindings.Conflicts.Select(x => (x.Command.Id, x.Reason, x.Winner?.Id)));
        Assert.Contains("in SceneEditor", bindings.Conflicts[1].Message);

        // The user can give a shortcut of the main window to a command of an editor
        bindings = CommandGestureBindings.Resolve(commands, builtIns.Contains, Overrides(("Plugin.SceneSave", "Ctrl+S")));
        Assert.Contains(("Ctrl+S", "Plugin.SceneSave"), Describe(bindings));
        Assert.Contains(("Ctrl+S", "Stride.File.Save"), Describe(bindings));
    }

    [Fact]
    public void ReservedShortcutsComeBeforeEveryLayer()
    {
        var save = Command("Stride.File.Save", "Ctrl+S");
        var user = Command("Plugin.Other", "Ctrl+K");

        var bindings = CommandGestureBindings.Resolve([save, user], x => x == save, Overrides(("Plugin.Other", "Ctrl+Q")), [new("Ctrl+Q", "Quit"), new("Ctrl+S", "Save")]);

        Assert.Empty(bindings.Bindings);
        Assert.All(bindings.Conflicts, x => Assert.Equal(CommandGestureConflictReason.Reserved, x.Reason));
        Assert.Contains(bindings.Conflicts, x => x.Message.Contains("Quit already uses it"));
    }

    [Fact]
    public void TheEditorsOwnCommandsComeBeforeThePluginsAndTheirShortcutsGoWithThePlugin()
    {
        var plugin = new TestPluginCommands.CommandPlugin { Commands = { Command("Plugin.Save", "Ctrl+S"), Command("Plugin.Find", "Ctrl+F") } };
        var (host, log) = Open(plugin);
        try
        {
            // Registered by the editor itself after the plugin joined
            var save = Command("Stride.File.Save", "Ctrl+S");
            using var registration = host.Commands.Register(save);
            Assert.True(host.IsBuiltInCommand(save));
            Assert.False(host.IsBuiltInCommand(plugin.Commands[0]));

            var bindings = host.ResolveCommandGestures();
            Assert.Equal([("Ctrl+F", "Plugin.Find"), ("Ctrl+S", "Stride.File.Save")], Describe(bindings));
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("Plugin.Save") && x.Text.Contains(typeof(TestPluginCommands.CommandPlugin).FullName!));

            // The user moves Save: the plugin's command gets Ctrl+S
            bindings = host.ResolveCommandGestures(Overrides(("Stride.File.Save", "Ctrl+Shift+S")));
            Assert.Equal([("Ctrl+S", "Plugin.Save"), ("Ctrl+F", "Plugin.Find"), ("Ctrl+Shift+S", "Stride.File.Save")], Describe(bindings));

            // The plugin leaves: its commands and their shortcuts go
            host.PluginsRemoved([plugin]);
            Assert.Equal([("Ctrl+Shift+S", "Stride.File.Save")], Describe(host.ResolveCommandGestures(Overrides(("Stride.File.Save", "Ctrl+Shift+S")))));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void ThePluginsOfTheEditorsOwnAssembliesAreBuiltIn()
    {
        var plugin = new TestPluginCommands.CommandPlugin { Commands = { Command("Stride.Own", "Ctrl+K") } };
        var host = new RecordingEditorPluginHost([plugin]) { PluginViewBaseType = typeof(object), IsEditorAssembly = assembly => assembly == typeof(TestCommandRegistry).Assembly };
        var session = new FakeSession();
        session.ServiceProvider.RegisterService(host);
        host.RegisterSession(session, new LoggerResult());
        try
        {
            Assert.True(host.IsBuiltInCommand(plugin.Commands[0]));
        }
        finally
        {
            host.Dispose();
        }
    }
}
