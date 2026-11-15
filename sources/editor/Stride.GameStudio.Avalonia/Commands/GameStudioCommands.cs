// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core.Translation;
using Stride.GameStudio.Avalonia.Docking;
using Stride.GameStudio.Avalonia.Helpers;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Commands;

/// <summary>
/// The commands of the main menu and of the main window's shortcuts that belong to this editor, for the command registry.
/// Each one runs the existing command of the main view model, as its menu item and its shortcut do; the Edit commands go to
/// the control that last had the focus, through the main view's router.
/// </summary>
/// <remarks>
/// The commands that act on the session are shared (<see cref="SessionCommands"/>). These have no placement: the main menu
/// shows them from its own items.
/// </remarks>
internal static class GameStudioCommands
{
    public const string NewId = "Stride.File.New";
    public const string OpenId = "Stride.File.Open";
    public const string CloseId = "Stride.File.Close";
    public const string ClearRecentId = "Stride.File.ClearRecent";
    public const string SaveId = "Stride.File.Save";
    public const string ReloadProjectId = "Stride.File.ReloadProject";
    public const string ExitId = "Stride.File.Exit";
    public const string UndoId = "Stride.Edit.Undo";
    public const string RedoId = "Stride.Edit.Redo";
    public const string CutId = "Stride.Edit.Cut";
    public const string CopyId = "Stride.Edit.Copy";
    public const string PasteId = "Stride.Edit.Paste";
    public const string DeleteId = "Stride.Edit.Delete";
    public const string RenameId = "Stride.Edit.Rename";
    public const string SettingsId = "Stride.Edit.Settings";
    public const string KeyboardShortcutsId = "Stride.Edit.KeyboardShortcuts";
    public const string ResetLayoutId = "Stride.View.ResetLayout";
    public const string CommandPaletteId = "Stride.View.CommandPalette";
    public const string BuildProjectId = "Stride.Project.Build";
    public const string StartProjectId = "Stride.Project.Start";
    public const string CancelBuildId = "Stride.Project.CancelBuild";
    public const string ReloadGameAssembliesId = "Stride.Project.ReloadGameAssemblies";
    public const string DocumentationId = "Stride.Help.Documentation";
    public const string QuestionsId = "Stride.Help.Questions";
    public const string ReportIssueId = "Stride.Help.ReportIssue";
    public const string ForumId = "Stride.Help.Forum";
    public const string DebugWindowId = "Stride.Help.DebugWindow";
    public const string AboutId = "Stride.Help.About";

    /// <summary>
    /// Creates the commands, each time new descriptors (a descriptor is registered once). The default shortcuts are those of
    /// this platform, as the main window binds them.
    /// </summary>
    public static IReadOnlyList<CommandDescriptor> Create() =>
    [
        // File
        Shared(NewId),
        Shared(OpenId),
        Wrap(CloseId, CommandCategories.File, () => Tr._p("Menu", "Close"), null, x => x.CloseCommand, keywords: ["project", "session"]),
        Wrap(ClearRecentId, CommandCategories.File, () => Tr._p("Menu", "Clear recent list"), null, x => x.ClearRecentFilesCommand, keywords: ["recent", "history"]),
        Wrap(SaveId, CommandCategories.File, () => Tr._p("Menu", "Save"), Primary(Key.S), x => x.SaveCommand, keywords: ["session", "all"]),
        Wrap(ReloadProjectId, CommandCategories.File, () => Tr._p("Menu", "Reload project"), null, x => x.ReloadCommand, keywords: ["session", "restart"]),
        Shared(ExitId),
        // Edit: the router's commands, which act on the control that last had the focus
        Edit(UndoId, () => Tr._p("Menu", "Undo"), EditCommandKind.Undo),
        Edit(RedoId, () => Tr._p("Menu", "Redo"), EditCommandKind.Redo),
        Edit(CutId, () => Tr._p("Menu", "Cut"), EditCommandKind.Cut, "clipboard"),
        Edit(CopyId, () => Tr._p("Menu", "Copy"), EditCommandKind.Copy, "clipboard"),
        Edit(PasteId, () => Tr._p("Menu", "Paste"), EditCommandKind.Paste, "clipboard"),
        Edit(DeleteId, () => Tr._p("Menu", "Delete"), EditCommandKind.Delete, "remove"),
        Edit(RenameId, () => Tr._p("Menu", "Rename"), EditCommandKind.Rename),
        Wrap(SettingsId, CommandCategories.Edit, () => Tr._p("Menu", "Settings..."), null, x => x.OpenSettingsWindowCommand, keywords: ["preferences", "options"]),
        Wrap(KeyboardShortcutsId, CommandCategories.Edit, () => Tr._p("Menu", "Keyboard shortcuts..."), null, x => x.OpenKeyboardShortcutsCommand, keywords: ["keys", "bindings", "gestures", "hotkeys", "preferences"]),
        // View
        Wrap(ResetLayoutId, CommandCategories.View, () => Tr._p("Menu", "Reset layout"), null, x => x.ViewMenu.ResetLayoutCommand, keywords: ["panels", "docking", "windows"]),
        CommandPalette(),
        // Project
        Wrap(BuildProjectId, CommandCategories.Project, () => Tr._p("Menu", "Build project"), new KeyGesture(Key.F6), x => x.Build.BuildProjectCommand, keywords: ["compile"]),
        Wrap(StartProjectId, CommandCategories.Project, () => Tr._p("Menu", "Start project"), new KeyGesture(Key.F5), x => x.Build.StartProjectCommand, keywords: ["run", "play", "launch", "game"]),
        Wrap(CancelBuildId, CommandCategories.Project, () => Tr._p("Menu", "Cancel build"), Primary(Key.F6), x => x.Build.CancelBuildCommand, keywords: ["stop"]),
        Wrap(ReloadGameAssembliesId, CommandCategories.Project, () => Tr._p("Menu", "Reload game assemblies"), null, x => x.CodeReload.ReloadCommand, keywords: ["code", "scripts"]),
        // Help
        .. WithoutSession.Where(x => x.Category == CommandCategories.Help).Select(Wrap),
    ];

    /// <summary>
    /// The commands that also work without a session, each with the command of the main view model it runs: File > New, Open
    /// and Exit, and the Help menu. The registry has the editor's commands only while a session is open (the editor's plugins
    /// register them when they join it); without one, the main window binds these by their identifier itself, with the user's
    /// shortcuts.
    /// </summary>
    public static IReadOnlyList<(CommandDescriptor Descriptor, ICommand Command)> CreateWithoutSession(MainViewModel main)
    {
        ArgumentNullException.ThrowIfNull(main);
        return [.. WithoutSession.Select(x => (x.Id == CommandPaletteId ? NotInPalette(Wrap(x)) : Wrap(x), (ICommand)new ParameterCommand(x.Command(main), x.Parameter)))];

        static CommandDescriptor NotInPalette(CommandDescriptor command) => new()
        {
            Id = command.Id,
            Category = command.Category,
            Title = command.Title,
            DefaultGesture = command.DefaultGesture,
            Keywords = command.Keywords,
            ShowInPalette = false,
            CanExecute = command.CanExecute,
            ExecuteAsync = command.ExecuteAsync,
        };
    }

    /// <summary>
    /// The command of the main view model (or of its session) that a built-in command runs, which its menu items and toolbar
    /// buttons follow to enable as its state changes; null when there is none.
    /// </summary>
    public static ICommand? StateSource(MainViewModel main, string id) => id switch
    {
        NewId => main.NewSessionCommand,
        OpenId => main.OpenCommand,
        CloseId => main.CloseCommand,
        SaveId => main.SaveCommand,
        ReloadProjectId => main.ReloadCommand,
        ExitId => main.ExitCommand,
        UndoId => main.EditRouter?.Undo,
        RedoId => main.EditRouter?.Redo,
        CutId => main.EditRouter?.Cut,
        CopyId => main.EditRouter?.Copy,
        PasteId => main.EditRouter?.Paste,
        DeleteId => main.EditRouter?.Delete,
        RenameId => main.EditRouter?.Rename,
        SettingsId => main.OpenSettingsWindowCommand,
        KeyboardShortcutsId => main.OpenKeyboardShortcutsCommand,
        CommandPaletteId => main.CommandPaletteCommand,
        ResetLayoutId => main.ViewMenu.ResetLayoutCommand,
        BuildProjectId => main.Build.BuildProjectCommand,
        StartProjectId => main.Build.StartProjectCommand,
        CancelBuildId => main.Build.CancelBuildCommand,
        ReloadGameAssembliesId => main.CodeReload.ReloadCommand,
        SessionCommands.AddDependencyId => main.Session?.AddDependencyCommand,
        SessionCommands.OpenInIdeId => main.Session?.OpenInIDECommand,
        _ => null,
    };

    // The command palette opens over the main window; the commands it runs act where it was opened from (the Edit commands, on
    // the control the key was pressed on)
    private static CommandDescriptor CommandPalette() => new()
    {
        Id = CommandPaletteId,
        Category = CommandCategories.View,
        Title = () => Tr._p("Menu", "Command palette..."),
        DefaultGesture = Primary(Key.P, KeyModifiers.Shift).ToString(),
        Keywords = ["commands", "search", "find", "run", "actions"],
        // Opening it from itself would do nothing
        ShowInPalette = false,
        CanExecute = context => MainOf(context)?.ShowCommandPalette is not null,
        ExecuteAsync = context =>
        {
            MainOf(context)?.ShowCommandPalette?.Invoke(context.Source);
            return Task.CompletedTask;
        },
    };

    // The commands of the main view model that work without a session (made on each call: the shortcuts depend on the platform)
    private static MainCommand[] WithoutSession =>
    [
        new(NewId, CommandCategories.File, () => Tr._p("Menu", "New..."), Primary(Key.N), x => x.NewSessionCommand, null, ["project", "game", "template"]),
        new(OpenId, CommandCategories.File, () => Tr._p("Menu", "Open..."), KeyGesture.Parse("Ctrl+O"), x => x.OpenCommand, null, ["project", "solution"]),
        new(ExitId, CommandCategories.File, () => Tr._p("Menu", "Exit"), KeyGesture.Parse("Alt+F4"), x => x.ExitCommand, null, ["quit", "close"]),
        new(DocumentationId, CommandCategories.Help, () => Tr._p("Menu", "Online documentation"), null, x => x.OpenWebPageCommand, StrideGameStudio.DocumentationUrl, ["manual", "web"]),
        new(QuestionsId, CommandCategories.Help, () => Tr._p("Menu", "Questions and answers"), null, x => x.OpenWebPageCommand, StrideGameStudio.AnswersUrl, ["web", "community"]),
        new(ReportIssueId, CommandCategories.Help, () => Tr._p("Menu", "Report an issue..."), null, x => x.OpenWebPageCommand, StrideGameStudio.ReportIssueUrl, ["bug", "github", "web"]),
        new(ForumId, CommandCategories.Help, () => Tr._p("Menu", "Discord forum"), null, x => x.OpenWebPageCommand, StrideGameStudio.ForumsUrl, ["chat", "community", "web"]),
        new(DebugWindowId, CommandCategories.Help, () => Tr._p("Menu", "Show debug window"), KeyGesture.Parse("Ctrl+Shift+D"), x => x.OpenDebugWindowCommand, null, ["log", "diagnostics"]),
        new(AboutId, CommandCategories.Help, () => Tr._p("Menu", "About..."), null, x => x.AboutCommand, null, ["version", "license"]),
        new(CommandPaletteId, CommandCategories.View, () => Tr._p("Menu", "Command palette..."), Primary(Key.P, KeyModifiers.Shift), x => x.CommandPaletteCommand, null, []),
    ];

    // A key with the platform's command modifier: Ctrl, or Cmd on macOS
    private static KeyGesture Primary(Key key, KeyModifiers others = KeyModifiers.None) => new(key, others | (Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control));

    private sealed record MainCommand(string Id, string Category, Func<string> Title, KeyGesture? Gesture, Func<MainViewModel, ICommand?> Command, object? Parameter, string[] Keywords);

    private static CommandDescriptor Shared(string id) => Wrap(WithoutSession.Single(x => x.Id == id));

    private static CommandDescriptor Wrap(MainCommand command) => Wrap(command.Id, command.Category, command.Title, command.Gesture, command.Command, command.Parameter, command.Keywords);

    private static MainViewModel? MainOf(CommandContext context) => context.Session.Main as MainViewModel;

    // A command of the registry that runs a command of the main view model: it can run when that command can, and runs it as
    // its menu item does, without waiting for what it starts
    private static CommandDescriptor Wrap(string id, string category, Func<string> title, KeyGesture? gesture, Func<MainViewModel, ICommand?> command, object? parameter = null, params string[] keywords) => new()
    {
        Id = id,
        Category = category,
        Title = title,
        DefaultGesture = gesture?.ToString(),
        Keywords = keywords,
        CanExecute = context => MainOf(context) is { } main && command(main) is { } target && target.CanExecute(parameter),
        ExecuteAsync = context =>
        {
            if (MainOf(context) is { } main && command(main) is { } target && target.CanExecute(parameter))
                target.Execute(parameter);
            return Task.CompletedTask;
        },
    };

    // An Edit command acts on the control that last had the focus in the window it was invoked from (the floating windows
    // have their own router), else in the main window; from a context menu, on the control the menu opened on
    private static CommandDescriptor Edit(string id, Func<string> title, EditCommandKind kind, params string[] keywords) => new()
    {
        Id = id,
        Category = CommandCategories.Edit,
        Title = title,
        DefaultGesture = EditCommands.GestureOf(kind).ToString(),
        Keywords = keywords,
        CanExecute = context => FindEditCommand(context, kind)?.CanExecute(null) ?? false,
        ExecuteAsync = context =>
        {
            if (FindEditCommand(context, kind) is { } command && command.CanExecute(null))
                command.Execute(null);
            return Task.CompletedTask;
        },
    };

    private static ICommand? FindEditCommand(CommandContext context, EditCommandKind kind)
    {
        if (IsContextMenu(context.Location) && context.Source is Visual source)
            return EditCommandRouter.Resolve(kind, source) is { } handler ? new HandlerCommand(handler, kind) : null;
        return FindRouter(context)?[kind];
    }

    // A context menu of a view or an editor: not the main menus, the toolbar, a shortcut or the palette
    private static bool IsContextMenu(string location)
    {
        return location is not (CommandLocations.Shortcut or CommandLocations.Palette or CommandLocations.MainToolbar)
            && !location.StartsWith("MainMenu/", StringComparison.Ordinal);
    }

    private static EditCommandRouter? FindRouter(CommandContext context)
    {
        if (context.Source is Visual visual)
        {
            switch (TopLevel.GetTopLevel(visual))
            {
                case StrideHostWindow floating:
                    return floating.EditRouter;
                case MainWindow window:
                    return window.View.EditRouter;
            }
        }
        return MainOf(context)?.EditRouter;
    }

    // The handler of an Edit command found from a control
    private sealed class HandlerCommand(IEditCommandHandler handler, EditCommandKind kind) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => handler.CanExecute(kind);

        public void Execute(object? parameter) => handler.Execute(kind);
    }

    // A command of the main view model with the parameter its menu item gives it
    private sealed class ParameterCommand(ICommand? command, object? parameter) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add
            {
                if (command is not null)
                    command.CanExecuteChanged += value;
            }
            remove
            {
                if (command is not null)
                    command.CanExecuteChanged -= value;
            }
        }

        public bool CanExecute(object? _) => command?.CanExecute(parameter) ?? false;

        public void Execute(object? _)
        {
            if (command is not null && command.CanExecute(parameter))
                command.Execute(parameter);
        }
    }
}
