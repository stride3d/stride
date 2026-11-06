// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
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
    public static IPluginRegistry<SceneViewToolDescriptor> SceneViewTools(this IEditorPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new RefusingRegistry(context.GetRegistry<SceneViewToolDescriptor>(), context);
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
    public static string SceneViewTool(Type toolType)
    {
        ArgumentNullException.ThrowIfNull(toolType);
        return $"SceneViewTool {toolType.FullName}";
    }
}
