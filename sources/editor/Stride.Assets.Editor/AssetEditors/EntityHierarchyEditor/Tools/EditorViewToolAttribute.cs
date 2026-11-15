// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// Declares a tool of the views of the scene and prefab editors. The class implements <see cref="IEditorViewTool"/> and has a
/// public parameterless constructor: each view creates its own instance.
/// </summary>
/// <remarks>
/// When a plugin joins the session, the plugin host finds the tools of its assemblies through the source-generated scan
/// index and registers them in the plugin's name, as <see cref="SceneViewToolDescriptor"/>s in
/// <see cref="ScenePluginContextExtensions.SceneViewTools"/>: they go when the plugin leaves. A type that is not such a
/// class is logged and left out, as when it is registered through the context. The tools show in the toolbar of the view,
/// after select, translate, rotate and scale.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[AssemblyScan]
public sealed class EditorViewToolAttribute : Attribute
{
    /// <summary>
    /// The order of the tools of the plugins: after the tools of the editor, which take 0 to 30.
    /// </summary>
    public const int DefaultOrder = 1000;

    /// <summary>
    /// Gets or sets the place of the tool in the toolbar (ties by type name).
    /// </summary>
    public int Order { get; set; } = DefaultOrder;
}
