// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Widgets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Editor;

/// <summary>
/// The contribution points of the scene and prefab editors in the plugin context. Their registries are declared by the
/// plugin host of the editors (<see cref="SceneEditorPluginHost"/>), so the Core context does not name them.
/// </summary>
public static class ScenePluginContextExtensions
{
    /// <summary>
    /// The tools of the views of the scene and prefab editors: each open view creates one instance of each tool, and adds or
    /// removes it as the plugin joins or leaves. A tool the views cannot use (not an <see cref="IEditorViewTool"/> class
    /// with a public parameterless constructor) is logged and left out, and the plugin joins without it.
    /// </summary>
    /// <param name="context">The context of the plugin.</param>
    [Experimental("STREXP004")]
    public static IPluginRegistry<SceneViewToolDescriptor> SceneViewTools(this IEditorPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new RefusingRegistry(context.GetRegistry<SceneViewToolDescriptor>(), context);
    }

    /// <summary>
    /// The widgets of the views of the scene and prefab editors, docked on an edge of the view over the game: each editor
    /// creates the view model of a widget the first time it shows, and destroys it when it closes or the plugin leaves. The
    /// toolkit companion of the plugin registers the view (<see cref="IEditorPluginContext.ViewTypes"/>).
    /// </summary>
    /// <param name="context">The context of the plugin.</param>
    /// <exception cref="ArgumentException">On registration: a widget without an identifier, or whose identifier is taken.</exception>
    [Experimental("STREXP005")]
    public static IPluginRegistry<SceneViewWidgetDescriptor> SceneViewWidgets(this IEditorPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetRegistry<SceneViewWidgetDescriptor>();
    }

    // Logs and leaves out a tool the views cannot use, rather than failing the plugin as the registry would
    private sealed class RefusingRegistry(IPluginRegistry<SceneViewToolDescriptor> registry, IEditorPluginContext context) : IPluginRegistry<SceneViewToolDescriptor>
    {
        public void Register(SceneViewToolDescriptor item)
        {
            if (SceneViewToolDescriptor.GetRefusal(item) is { } refusal)
            {
                context.Logger.Error(SceneViewToolDescriptor.DescribeRefusal(item, context.Plugin, refusal));
                return;
            }
            registry.Register(item);
        }
    }
}

/// <summary>
/// The names of the contributions of the scene and prefab editors, as the contribution guard (<see cref="IPluginContributionGuard"/>)
/// and <see cref="PluginError.Contribution"/> give them, besides <see cref="PluginContributions"/>.
/// </summary>
public static class ScenePluginContributions
{
    /// <summary>
    /// A tool of the scene and prefab views: its members, called by every view.
    /// </summary>
    [Experimental("STREXP004")]
    public static string SceneViewTool(Type toolType)
    {
        ArgumentNullException.ThrowIfNull(toolType);
        return $"SceneViewTool {toolType.FullName}";
    }

    /// <summary>
    /// A widget of the scene and prefab views: its title, and the creation and destruction of its view model.
    /// </summary>
    [Experimental("STREXP005")]
    public static string SceneViewWidget(string widgetId)
    {
        ArgumentNullException.ThrowIfNull(widgetId);
        return $"SceneViewWidget {widgetId}";
    }
}
