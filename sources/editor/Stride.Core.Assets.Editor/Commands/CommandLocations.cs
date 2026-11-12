// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// The locations of the editor where commands can be placed (<see cref="CommandPlacement.Location"/>). Other layers of the
/// editor add theirs, such as the scene editor's.
/// </summary>
/// <remarks>
/// Each built-in menu shows the commands of its location at its end, after a separator, by group and order. The Tools menu
/// is shown only when it has commands. A context menu fills its part when it opens, with the selection of its view. The
/// main toolbar shows its commands as buttons after the build buttons.
/// </remarks>
[Experimental("STREXP003")]
public static class CommandLocations
{
    /// <summary>
    /// The end of the File menu.
    /// </summary>
    public const string MainMenuFile = "MainMenu/File";

    /// <summary>
    /// The end of the Edit menu.
    /// </summary>
    public const string MainMenuEdit = "MainMenu/Edit";

    /// <summary>
    /// The end of the View menu.
    /// </summary>
    public const string MainMenuView = "MainMenu/View";

    /// <summary>
    /// The end of the Project menu.
    /// </summary>
    public const string MainMenuProject = "MainMenu/Project";

    /// <summary>
    /// The Tools menu, shown only when it has commands.
    /// </summary>
    public const string MainMenuTools = "MainMenu/Tools";

    /// <summary>
    /// The end of the Help menu.
    /// </summary>
    public const string MainMenuHelp = "MainMenu/Help";

    /// <summary>
    /// The main toolbar, after the build buttons.
    /// </summary>
    public const string MainToolbar = "MainToolbar";

    /// <summary>
    /// The context menu of the asset view; the selection is the selected assets and folders.
    /// </summary>
    public const string AssetViewContext = "AssetView/Context";

    /// <summary>
    /// The context menu of the solution explorer; the selection is the selected items (packages, folders, projects).
    /// </summary>
    public const string SolutionExplorerContext = "SolutionExplorer/Context";

    /// <summary>
    /// The context menu of a property of the property grid; the selection is the property's node view model.
    /// </summary>
    public const string PropertyGridContext = "PropertyGrid/Context";

    /// <summary>
    /// Not a place: the location a command gets when its shortcut runs it.
    /// </summary>
    public const string Shortcut = "Shortcut";

    /// <summary>
    /// Not a place: the location a command gets when the command palette runs it (see <see cref="CommandContext.ForPalette"/>).
    /// Every command with <see cref="CommandDescriptor.ShowInPalette"/> is listed there, whatever its placements.
    /// </summary>
    public const string Palette = "Palette";
}
