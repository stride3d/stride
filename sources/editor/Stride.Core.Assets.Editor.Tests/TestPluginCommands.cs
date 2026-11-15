// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The commands and toolbar widgets of the plugins: registered through the context, gone with their plugin, run through the
/// contribution guard, and their shortcuts bound first come, first served.
/// </summary>
public sealed class TestPluginCommands
{
    public sealed class CommandPlugin : AssetsPlugin
    {
        public List<CommandDescriptor> Commands { get; } = [];

        public List<ToolbarWidgetDescriptor> Widgets { get; } = [];

        public override IReadOnlyList<System.Reflection.Assembly> ScannedAssemblies => [];

        public override void Initialize(IEditorPluginContext context)
        {
            foreach (var command in Commands)
                context.Commands.Register(command);
            foreach (var widget in Widgets)
                context.ToolbarWidgets.Register(widget);
        }
    }

    public sealed class WidgetViewModel : IDestroyable
    {
        public bool Destroyed { get; private set; }

        public void Destroy() => Destroyed = true;
    }

    private static CommandDescriptor Command(string id, string? gesture = null, Func<CommandContext, bool>? canExecute = null, Func<CommandContext, Task>? execute = null, params CommandPlacement[] placements) => new()
    {
        Id = id,
        Title = () => id,
        DefaultGesture = gesture,
        CanExecute = canExecute,
        ExecuteAsync = execute ?? (_ => Task.CompletedTask),
        Placements = placements,
    };

    private static CommandContext Context(string location = CommandLocations.MainMenuTools, params object[] selection) => new()
    {
        // The guard and the registry never read the session
        Session = null!,
        Location = location,
        Selection = selection,
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
    public void TheCommandsAndWidgetsOfAPluginGoWithIt()
    {
        var plugin = new CommandPlugin
        {
            Commands = { Command("Tests.Open", placements: new CommandPlacement(CommandLocations.MainMenuFile)) },
            Widgets = { new ToolbarWidgetDescriptor { Id = "Tests.Widget", CreateViewModel = _ => new WidgetViewModel() } },
        };
        var (host, _) = Open(plugin);
        try
        {
            Assert.Equal(plugin.Commands, host.Commands.Items);
            Assert.Same(plugin, host.Commands.GetOwner(plugin.Commands[0]));
            Assert.Equal(plugin.Widgets, host.ToolbarWidgets.Items);
            Assert.Same(plugin, host.ToolbarWidgets.GetOwner(plugin.Widgets[0]));
            Assert.True(host.IsCommandAvailable(plugin.Commands[0]));

            host.PluginsRemoved([plugin]);
            Assert.Empty(host.Commands.Items);
            Assert.Empty(host.ToolbarWidgets.Items);
            Assert.False(host.IsCommandAvailable(plugin.Commands[0]));

            host.PluginsDiscovered([plugin]);
            Assert.Equal(plugin.Commands, host.Commands.Items);
            Assert.Equal(plugin.Widgets, host.ToolbarWidgets.Items);
        }
        finally
        {
            host.Dispose();
        }
        Assert.Empty(host.Commands.Items);
        Assert.Empty(host.ToolbarWidgets.Items);
    }

    [Fact]
    public void ACommandWithoutAnIdentifierOrWithTheIdentifierOfAnotherIsRefused()
    {
        var first = new CommandPlugin { Commands = { Command("Tests.Same") } };
        var second = new CommandPlugin { Commands = { Command("Tests.Other"), Command("Tests.Same") } };
        var empty = new CommandPlugin { Commands = { Command(" ") } };
        var noLocation = new CommandPlugin { Commands = { Command("Tests.NoLocation", placements: new CommandPlacement("")) } };
        var (host, log) = Open(first, second, empty, noLocation);
        try
        {
            // The plugins that register a wrong command fail to join, without what they registered before
            Assert.Equal([first.Commands[0]], host.Commands.Items);
            Assert.Equal(PluginState.Failed, host.GetPluginStatus(second)!.State);
            Assert.Equal(PluginState.Failed, host.GetPluginStatus(empty)!.State);
            Assert.Equal(PluginState.Failed, host.GetPluginStatus(noLocation)!.State);
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(typeof(CommandPlugin).FullName!));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public async Task ACommandRunsAndSaysWhetherItCan()
    {
        var runs = new List<CommandContext>();
        var enabled = true;
        var plugin = new CommandPlugin
        {
            Commands =
            {
                Command("Tests.Always", execute: x => { runs.Add(x); return Task.CompletedTask; }),
                Command("Tests.Sometimes", canExecute: _ => enabled, execute: x => { runs.Add(x); return Task.CompletedTask; }),
            },
        };
        var (host, _) = Open(plugin);
        try
        {
            var context = Context(CommandLocations.AssetViewContext, "asset");
            Assert.True(host.CanExecuteCommand(plugin.Commands[0], context));
            Assert.True(host.CanExecuteCommand(plugin.Commands[1], context));
            Assert.True(await host.ExecuteCommandAsync(plugin.Commands[1], context));
            Assert.Same(context, Assert.Single(runs));

            // It cannot run: it does not
            enabled = false;
            Assert.False(host.CanExecuteCommand(plugin.Commands[1], context));
            Assert.False(await host.ExecuteCommandAsync(plugin.Commands[1], context));
            Assert.Single(runs);
            Assert.True(await host.ExecuteCommandAsync(plugin.Commands[0], context));
            Assert.Equal(2, runs.Count);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Theory]
    [InlineData("CanExecute")]
    [InlineData("Execute")]
    [InlineData("ExecuteLater")]
    [InlineData("ContextTitle")]
    public async Task ACommandThatFailsIsTurnedOffAloneAndRecorded(string failing)
    {
        var failure = new InvalidOperationException("The command failed.");
        var command = new CommandDescriptor
        {
            Id = "Tests.Failing",
            Title = () => "Failing",
            ContextTitle = failing == "ContextTitle" ? _ => throw failure : null,
            CanExecute = failing == "CanExecute" ? _ => throw failure : null,
            ExecuteAsync = failing switch
            {
                "Execute" => _ => throw failure,
                "ExecuteLater" => async _ => { await Task.Yield(); throw failure; },
                _ => _ => Task.CompletedTask,
            },
        };
        var other = Command("Tests.Other");
        var plugin = new CommandPlugin { Commands = { command, other } };
        var (host, log) = Open(plugin);
        try
        {
            var context = Context();
            if (failing == "ContextTitle")
                Assert.Equal("Failing", host.GetCommandTitle(command, context));
            else
                Assert.False(await host.ExecuteCommandAsync(command, context));

            // Turned off for the session, logged with the plugin's name, recorded in its status
            Assert.False(host.IsCommandAvailable(command));
            Assert.False(host.CanExecuteCommand(command, context));
            Assert.False(await host.ExecuteCommandAsync(command, context));
            var status = host.GetPluginStatus(plugin)!;
            Assert.Equal(PluginState.Joined, status.State);
            Assert.Contains(PluginContributions.Command(command.Id), status.TurnedOffContributions);
            Assert.Same(failure, Assert.Single(status.Errors).Exception);
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(typeof(CommandPlugin).FullName!) && x.Text.Contains(command.Id));
            // The plugin's other command stays
            Assert.True(host.IsCommandAvailable(other));
            Assert.True(await host.ExecuteCommandAsync(other, context));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void TheTitleInAContextFallsBackOnTheTitle()
    {
        var plugin = new CommandPlugin
        {
            Commands =
            {
                new CommandDescriptor { Id = "Tests.Plain", Title = () => "Plain", ExecuteAsync = _ => Task.CompletedTask },
                new CommandDescriptor { Id = "Tests.Contextual", Title = () => "Toggle", ContextTitle = x => $"Toggle {x.Selection.Count}", ExecuteAsync = _ => Task.CompletedTask },
            },
        };
        var (host, _) = Open(plugin);
        try
        {
            Assert.Equal("Plain", host.GetCommandTitle(plugin.Commands[0], Context()));
            Assert.Equal("Toggle 2", host.GetCommandTitle(plugin.Commands[1], Context(CommandLocations.AssetViewContext, "a", "b")));
            Assert.Equal("Toggle", host.GetCommandTitle(plugin.Commands[1], null));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AToolbarWidgetIsCreatedAndDestroyedThroughTheGuard()
    {
        var created = new WidgetViewModel();
        var good = new ToolbarWidgetDescriptor { Id = "Tests.Good", CreateViewModel = _ => created };
        var bad = new ToolbarWidgetDescriptor { Id = "Tests.Bad", CreateViewModel = _ => throw new InvalidOperationException("No widget.") };
        var plugin = new CommandPlugin { Widgets = { good, bad } };
        var (host, log) = Open(plugin);
        try
        {
            Assert.Same(created, host.CreateToolbarWidget(good, new ToolbarWidgetContext { Descriptor = good, Session = null!, ServiceProvider = null! }));
            host.DestroyToolbarWidget(good, created);
            Assert.True(created.Destroyed);

            Assert.Null(host.CreateToolbarWidget(bad, new ToolbarWidgetContext { Descriptor = bad, Session = null!, ServiceProvider = null! }));
            Assert.False(host.IsToolbarWidgetAvailable(bad));
            Assert.True(host.IsToolbarWidgetAvailable(good));
            Assert.Contains(PluginContributions.ToolbarWidget(bad.Id), host.GetPluginStatus(plugin)!.TurnedOffContributions);
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(bad.Id));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Theory]
    [InlineData("Ctrl+Shift+K", "Ctrl+Shift+K")]
    [InlineData("shift+ctrl+k", "Ctrl+Shift+K")]
    [InlineData("Control+Alt+Delete", "Ctrl+Alt+Delete")]
    [InlineData(" Cmd + F5 ", "Meta+F5")]
    [InlineData("F2", "F2")]
    [InlineData("Ctrl++", "Ctrl++")]
    public void AShortcutIsParsedWhateverTheOrderAndCaseOfItsParts(string text, string expected)
    {
        var gesture = CommandGesture.Parse(text);
        Assert.Equal(expected, gesture.ToString());
        Assert.Equal(CommandGesture.Parse(expected), gesture);
        Assert.Equal(CommandGesture.Parse(expected).GetHashCode(), gesture.GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Shift")]
    [InlineData("Ctrl+Ctrl+K")]
    [InlineData("Ctrl+K+L")]
    [InlineData("Hyper+K")]
    public void AWrongShortcutIsNotParsed(string text)
    {
        Assert.False(CommandGesture.TryParse(text, out _));
        Assert.Throws<FormatException>(() => CommandGesture.Parse(text));
    }

    [Fact]
    public void AShortcutGoesToTheFirstThatClaimsIt()
    {
        var builtIn = Command("Stride.Builtin", "Ctrl+Shift+B");
        var first = new CommandPlugin { Commands = { Command("First.Command", "ctrl+shift+k"), Command("First.Save", "Ctrl+S") } };
        var second = new CommandPlugin { Commands = { Command("Second.Command", "Shift+Ctrl+K"), Command("Second.Other", "Alt+K"), Command("Second.Wrong", "Ctrl+Nope+K"), Command("Second.Typing", "Shift+K"), Command("Second.Function", "Shift+F9") } };
        var (host, log) = Open(first, second);
        try
        {
            host.Commands.Register(builtIn);
            var bindings = host.ResolveCommandGestures(reserved: [new("Ctrl+S", "File > Save")]);

            Assert.Equal(
                [("Ctrl+Shift+K", "First.Command"), ("Alt+K", "Second.Other"), ("Shift+F9", "Second.Function"), ("Ctrl+Shift+B", "Stride.Builtin")],
                bindings.Bindings.Select(x => (x.Gesture.ToString(), x.Command.Id)));
            // A warning names both
            Assert.Equal(4, bindings.Warnings.Count);
            Assert.Contains(bindings.Warnings, x => x.Contains("[Ctrl+S]") && x.Contains("First.Save") && x.Contains("File > Save"));
            Assert.Contains(bindings.Warnings, x => x.Contains("[Ctrl+Shift+K]") && x.Contains("Second.Command") && x.Contains("First.Command"));
            Assert.Contains(bindings.Warnings, x => x.Contains("[Ctrl+Nope+K]") && x.Contains("Second.Wrong"));
            // A plain or shifted key would be taken from the text boxes
            Assert.Contains(bindings.Warnings, x => x.Contains("[Shift+K]") && x.Contains("Second.Typing") && x.Contains("needs Ctrl, Alt or Meta"));
            Assert.Equal(4, log.Messages.Count(x => x.Type == LogMessageType.Warning && bindings.Warnings.Contains(x.Text)));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void ATurnedOffCommandLosesItsShortcut()
    {
        var command = Command("Tests.Failing", "Ctrl+K", canExecute: _ => throw new InvalidOperationException());
        var plugin = new CommandPlugin { Commands = { command } };
        var (host, _) = Open(plugin);
        try
        {
            Assert.Single(host.ResolveCommandGestures().Bindings);
            Assert.False(host.CanExecuteCommand(command, Context()));
            Assert.Empty(host.ResolveCommandGestures().Bindings);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void TheCommandsOfALocationComeInGroupsByOrder()
    {
        const string Location = CommandLocations.MainMenuTools;
        var commands = new[]
        {
            Command("A", placements: new CommandPlacement(Location, "Second", 20)),
            Command("B", placements: new CommandPlacement(Location, "First", 10)),
            Command("C", placements: new CommandPlacement(Location, "Second", 5)),
            Command("D", placements: new CommandPlacement(CommandLocations.MainMenuFile)),
            Command("E", placements: [new CommandPlacement(CommandLocations.MainMenuFile), new CommandPlacement(Location, "First", 10)]),
            Command("F", placements: new CommandPlacement(Location)),
        };

        var groups = CommandArrangement.Arrange(commands, Location);

        // Groups by their lowest order: Default (0), Second (5), First (10); in a group by order, then registration
        Assert.Equal([["F"], ["C", "A"], ["B", "E"]], groups.Select(x => x.Select(c => c.Id).ToArray()).ToArray());
        Assert.Equal(["D", "E"], CommandArrangement.Arrange(commands, CommandLocations.MainMenuFile).Single().Select(x => x.Id));
        Assert.Empty(CommandArrangement.Arrange(commands, CommandLocations.PropertyGridContext));
    }
}
