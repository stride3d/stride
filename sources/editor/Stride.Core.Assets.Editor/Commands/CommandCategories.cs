// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// The categories of the editor's own commands (<see cref="CommandDescriptor.Category"/>), which a plugin can use for its
/// commands too. They name the main menus, and the places of the editor that have no menu of their own.
/// </summary>
[Experimental("STREXP003")]
public static class CommandCategories
{
    /// <summary>
    /// The session and its files: the File menu.
    /// </summary>
    public const string File = "File";

    /// <summary>
    /// The Edit menu and the editor's settings.
    /// </summary>
    public const string Edit = "Edit";

    /// <summary>
    /// The panels and the layout: the View menu.
    /// </summary>
    public const string View = "View";

    /// <summary>
    /// Building and running the game, packages and the IDE: the Project menu.
    /// </summary>
    public const string Project = "Project";

    /// <summary>
    /// The assets of the asset view.
    /// </summary>
    public const string Asset = "Asset";

    /// <summary>
    /// The scene editor.
    /// </summary>
    public const string Scene = "Scene";

    /// <summary>
    /// The Tools menu.
    /// </summary>
    public const string Tools = "Tools";

    /// <summary>
    /// Documentation and support: the Help menu.
    /// </summary>
    public const string Help = "Help";

    /// <summary>
    /// The name of a category as the editor shows it: translated for the editor's categories, as it is for a plugin's own.
    /// </summary>
    public static string GetDisplayName(string? category) => category switch
    {
        File => Tr._p("Menu", "File"),
        Edit => Tr._p("Menu", "Edit"),
        View => Tr._p("Menu", "View"),
        Project => Tr._p("Menu", "Project"),
        Asset => Tr._p("Menu", "Asset"),
        Scene => Tr._p("Menu", "Scene"),
        Tools => Tr._p("Menu", "Tools"),
        Help => Tr._p("Menu", "Help"),
        _ => category ?? string.Empty,
    };
}
