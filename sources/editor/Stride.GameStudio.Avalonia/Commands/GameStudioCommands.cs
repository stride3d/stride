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
    public const string ResetLayoutId = "Stride.View.ResetLayout";
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
        Wrap(NewId, CommandCategories.File, () => Tr._p("Menu", "New..."), MainView.NewGesture, x => x.NewSessionCommand, keywords: ["project", "game", "template"]),
        Wrap(OpenId, CommandCategories.File, () => Tr._p("Menu", "Open..."), KeyGesture.Parse("Ctrl+O"), x => x.OpenCommand, keywords: ["project", "solution"]),
        Wrap(CloseId, CommandCategories.File, () => Tr._p("Menu", "Close"), null, x => x.CloseCommand, keywords: ["project", "session"]),
        Wrap(ClearRecentId, CommandCategories.File, () => Tr._p("Menu", "Clear recent list"), null, x => x.ClearRecentFilesCommand, keywords: ["recent", "history"]),
        Wrap(SaveId, CommandCategories.File, () => Tr._p("Menu", "Save"), MainView.SaveGesture, x => x.SaveCommand, keywords: ["session", "all"]),
        Wrap(ReloadProjectId, CommandCategories.File, () => Tr._p("Menu", "Reload project"), null, x => x.ReloadCommand, keywords: ["session", "restart"]),
        Wrap(ExitId, CommandCategories.File, () => Tr._p("Menu", "Exit"), KeyGesture.Parse("Alt+F4"), x => x.ExitCommand, keywords: ["quit", "close"]),
        // Edit: the router's commands, which act on the control that last had the focus
        Edit(UndoId, () => Tr._p("Menu", "Undo"), EditCommandKind.Undo),
        Edit(RedoId, () => Tr._p("Menu", "Redo"), EditCommandKind.Redo),
        Edit(CutId, () => Tr._p("Menu", "Cut"), EditCommandKind.Cut, "clipboard"),
        Edit(CopyId, () => Tr._p("Menu", "Copy"), EditCommandKind.Copy, "clipboard"),
        Edit(PasteId, () => Tr._p("Menu", "Paste"), EditCommandKind.Paste, "clipboard"),
        Edit(DeleteId, () => Tr._p("Menu", "Delete"), EditCommandKind.Delete, "remove"),
        Edit(RenameId, () => Tr._p("Menu", "Rename"), EditCommandKind.Rename),
        Wrap(SettingsId, CommandCategories.Edit, () => Tr._p("Menu", "Settings..."), null, x => x.OpenSettingsWindowCommand, keywords: ["preferences", "options"]),
        // View
        Wrap(ResetLayoutId, CommandCategories.View, () => Tr._p("Menu", "Reset layout"), null, x => x.ViewMenu.ResetLayoutCommand, keywords: ["panels", "docking", "windows"]),
        // Project
        Wrap(BuildProjectId, CommandCategories.Project, () => Tr._p("Menu", "Build project"), MainView.BuildProjectGesture, x => x.Build.BuildProjectCommand, keywords: ["compile"]),
        Wrap(StartProjectId, CommandCategories.Project, () => Tr._p("Menu", "Start project"), MainView.StartProjectGesture, x => x.Build.StartProjectCommand, keywords: ["run", "play", "launch", "game"]),
        Wrap(CancelBuildId, CommandCategories.Project, () => Tr._p("Menu", "Cancel build"), MainView.CancelBuildGesture, x => x.Build.CancelBuildCommand, keywords: ["stop"]),
        Wrap(ReloadGameAssembliesId, CommandCategories.Project, () => Tr._p("Menu", "Reload game assemblies"), null, x => x.CodeReload.ReloadCommand, keywords: ["code", "scripts"]),
        // Help
        Wrap(DocumentationId, CommandCategories.Help, () => Tr._p("Menu", "Online documentation"), null, x => x.OpenWebPageCommand, StrideGameStudio.DocumentationUrl, ["manual", "web"]),
        Wrap(QuestionsId, CommandCategories.Help, () => Tr._p("Menu", "Questions and answers"), null, x => x.OpenWebPageCommand, StrideGameStudio.AnswersUrl, ["web", "community"]),
        Wrap(ReportIssueId, CommandCategories.Help, () => Tr._p("Menu", "Report an issue..."), null, x => x.OpenWebPageCommand, StrideGameStudio.ReportIssueUrl, ["bug", "github", "web"]),
        Wrap(ForumId, CommandCategories.Help, () => Tr._p("Menu", "Discord forum"), null, x => x.OpenWebPageCommand, StrideGameStudio.ForumsUrl, ["chat", "community", "web"]),
        Wrap(DebugWindowId, CommandCategories.Help, () => Tr._p("Menu", "Show debug window"), KeyGesture.Parse("Ctrl+Shift+D"), x => x.OpenDebugWindowCommand, keywords: ["log", "diagnostics"]),
        Wrap(AboutId, CommandCategories.Help, () => Tr._p("Menu", "About..."), null, x => x.AboutCommand, keywords: ["version", "license"]),
    ];

    private static MainViewModel? Main(CommandContext context) => context.Session.Main as MainViewModel;

    // A command of the registry that runs a command of the main view model: it can run when that command can, and runs it as
    // its menu item does, without waiting for what it starts
    private static CommandDescriptor Wrap(string id, string category, Func<string> title, KeyGesture? gesture, Func<MainViewModel, ICommand?> command, object? parameter = null, params string[] keywords) => new()
    {
        Id = id,
        Category = category,
        Title = title,
        DefaultGesture = gesture?.ToString(),
        Keywords = keywords,
        CanExecute = context => Main(context) is { } main && command(main) is { } target && target.CanExecute(parameter),
        ExecuteAsync = context =>
        {
            if (Main(context) is { } main && command(main) is { } target && target.CanExecute(parameter))
                target.Execute(parameter);
            return Task.CompletedTask;
        },
    };

    // An Edit command acts on the control that last had the focus in the window it was invoked from (the floating windows
    // have their own router), else in the main window
    private static CommandDescriptor Edit(string id, Func<string> title, EditCommandKind kind, params string[] keywords) => new()
    {
        Id = id,
        Category = CommandCategories.Edit,
        Title = title,
        DefaultGesture = EditCommands.GestureOf(kind).ToString(),
        Keywords = keywords,
        CanExecute = context => FindRouter(context)?[kind].CanExecute(null) ?? false,
        ExecuteAsync = context =>
        {
            if (FindRouter(context)?[kind] is { } command && command.CanExecute(null))
                command.Execute(null);
            return Task.CompletedTask;
        },
    };

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
        return Main(context)?.EditRouter;
    }
}
