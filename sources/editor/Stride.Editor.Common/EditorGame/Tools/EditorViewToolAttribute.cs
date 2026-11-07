// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Reflection;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// Declares a tool of the views of the scene and prefab editors. The class implements <c>IEditorViewTool</c> and has a
/// parameterless constructor: each view creates its own instance.
/// </summary>
/// <remarks>
/// When a plugin (<see cref="Stride.Core.Assets.Editor.Services.AssetsPlugin"/>) joins the session, the plugin host finds
/// the tools of its assembly through the source-generated scan index and registers them in the plugin's name
/// (<see cref="Stride.Core.Assets.Editor.Services.IEditorPluginContext.EditorViewToolTypes"/>): they go when the plugin
/// leaves. They show in the transformation group of the toolbar of the view, after select, translate, rotate and scale.
/// The attribute lives below the tool contract so that the host can scan for it.
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
