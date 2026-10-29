// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Reflection;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// Declares a tool of the views of the scene and prefab editors. The class implements <see cref="IEditorViewTool"/> and
/// has a public parameterless constructor: each view creates its own instance.
/// </summary>
/// <remarks>
/// The tools of an assembly are found through its source-generated scan index when it registers as an asset assembly
/// (a plugin, a game's editor project). They show in the transformation group of the toolbar of the view, after
/// select, translate, rotate and scale.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[AssemblyScan]
public sealed class EditorViewToolAttribute : Attribute
{
    /// <summary>
    /// The order of the tools of the editor; a plugin tool keeps it to come after them.
    /// </summary>
    public const int DefaultOrder = 1000;

    /// <summary>
    /// Gets or sets the place of the tool in the toolbar (ties by type name).
    /// </summary>
    public int Order { get; set; } = DefaultOrder;
}
