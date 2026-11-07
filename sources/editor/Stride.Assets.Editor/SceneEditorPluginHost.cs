// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Widgets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Editor;

namespace Stride.Assets.Editor;

/// <summary>
/// The plugin host of the editors that have the scene and prefab editors (both Game Studios): it declares the contribution
/// points of these editors (tools and widgets), which the plugins reach through typed methods of their context (<see cref="ScenePluginContextExtensions"/>),
/// and finds the scene view tools that the assemblies of a plugin declare (<see cref="EditorViewToolAttribute"/>).
/// </summary>
public abstract class SceneEditorPluginHost : StrideEditorPluginHost
{
    /// <inheritdoc />
    protected SceneEditorPluginHost(IReadOnlyList<AssetsPlugin>? plugins = null, IDispatcherService? dispatcher = null)
        : base(plugins, dispatcher)
    {
        SceneViewTools = DeclareRegistry<SceneViewToolDescriptor>(SceneViewToolDescriptor.Validate);
        SceneViewWidgets = DeclareRegistry<SceneViewWidgetDescriptor>(ValidateSceneViewWidget);
    }

    /// <summary>
    /// The tools of the views of the scene and prefab editors that the plugins register, each with its owner: the open views
    /// follow its changes.
    /// </summary>
    [Experimental("STREXP004")]
    public ContributionRegistry<SceneViewToolDescriptor> SceneViewTools { get; }

    /// <summary>
    /// The widgets of the views of the scene and prefab editors that the plugins register, each with its owner: the open
    /// editors follow its changes.
    /// </summary>
    [Experimental("STREXP005")]
    public ContributionRegistry<SceneViewWidgetDescriptor> SceneViewWidgets { get; }

    /// <inheritdoc />
    /// <remarks>
    /// The assemblies of a plugin also declare the tools of the scene and prefab views (<see cref="EditorViewToolAttribute"/>):
    /// they are registered as if the plugin registered them, so a type the views cannot use is logged and left out the same way.
    /// </remarks>
    protected override void ScanPlugin(AssetsPlugin plugin, IEditorPluginContext context)
    {
        base.ScanPlugin(plugin, context);

        IPluginRegistry<SceneViewToolDescriptor>? tools = null;
        foreach (var assembly in plugin.ScannedAssemblies)
        {
            foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(EditorViewToolAttribute)))
                (tools ??= context.SceneViewTools()).Register(SceneViewToolDescriptor.FromType(type));
        }
    }

    /// <inheritdoc />
    /// <remarks>The tool goes to <see cref="SceneViewTools"/>, with the same checks as a descriptor.</remarks>
    protected override IDisposable? RegisterEditorViewToolType(AssetsPlugin plugin, Type type)
    {
        var descriptor = SceneViewToolDescriptor.FromType(type);
        if (SceneViewToolDescriptor.GetRefusal(descriptor) is { } refusal)
        {
            PluginsLogger.Error(SceneViewToolDescriptor.DescribeRefusal(descriptor, plugin, refusal));
            return null;
        }
        return SceneViewTools.Register(descriptor, plugin);
    }

    private void ValidateSceneViewWidget(SceneViewWidgetDescriptor widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        if (string.IsNullOrWhiteSpace(widget.Id))
            throw new ArgumentException("A scene view widget needs an identifier.", nameof(widget));
        if (widget.Title is null || widget.CreateViewModel is null)
            throw new ArgumentException($"The scene view widget [{widget.Id}] needs a title and a view model.", nameof(widget));
        if (SceneViewWidgets.Items.Any(x => x.Id == widget.Id))
            throw new ArgumentException($"A scene view widget with the identifier [{widget.Id}] is already registered.", nameof(widget));
    }
}
