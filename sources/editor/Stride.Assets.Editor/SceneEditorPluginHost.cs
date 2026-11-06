// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Editor;

namespace Stride.Assets.Editor;

/// <summary>
/// The plugin host of the editors that have the scene and prefab editors (both Game Studios): it declares the contribution
/// points of these editors, which the plugins reach through typed methods of their context (<see cref="ScenePluginContextExtensions"/>),
/// and finds the scene view tools that the assemblies of a plugin declare (<see cref="EditorViewToolAttribute"/>).
/// </summary>
public abstract class SceneEditorPluginHost : StrideEditorPluginHost
{
    /// <inheritdoc />
    protected SceneEditorPluginHost(IReadOnlyList<AssetsPlugin>? plugins = null, IDispatcherService? dispatcher = null)
        : base(plugins, dispatcher)
    {
        SceneViewTools = DeclareRegistry<SceneViewToolDescriptor>(SceneViewToolDescriptor.Validate);
    }

    /// <summary>
    /// The tools of the views of the scene and prefab editors that the plugins register, each with its owner: the open views
    /// follow its changes.
    /// </summary>
    public ContributionRegistry<SceneViewToolDescriptor> SceneViewTools { get; }

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
}
