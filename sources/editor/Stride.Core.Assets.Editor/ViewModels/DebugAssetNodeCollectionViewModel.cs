// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModels;

public class DebugAssetNodeCollectionViewModel : DispatcherViewModel, IDebugPage
{
    private static readonly FieldInfo FieldInfoListener;
    private static readonly FieldInfo FieldInfoRegisteredNodes;
    private readonly SessionViewModel session;

    private object? selectedNode;
    private static readonly Dictionary<Guid, AssetViewModel> NodeToAssetMap = new();

    static DebugAssetNodeCollectionViewModel()
    {
        // We use reflection to access a non-public field.
        var fieldInfoListener = typeof(AssetPropertyGraph).GetField("nodeListener", BindingFlags.Instance | BindingFlags.NonPublic);
        var fieldInfoRegisteredNodes = typeof(GraphNodeChangeListener).GetField("RegisteredNodes", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfoListener = fieldInfoListener ?? throw new MissingFieldException("AssetPropertyGraph is missing the nodeListener private member.");
        FieldInfoRegisteredNodes = fieldInfoRegisteredNodes ?? throw new MissingFieldException("GraphNodeChangeListener is missing the RegisteredNodes private member.");
    }

    public DebugAssetNodeCollectionViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        this.session = session;

        RefreshQuantumNodesCommand = new AnonymousCommand(ServiceProvider, RefreshQuantumViewModel);
    }

    public ObservableList<DebugAssetRootNodeViewModel> AssetNodes { get; } = [];

    public object? SelectedNode
    {
        get => selectedNode;
        set => SetValue(ref selectedNode, value);
    }

    public string Title { get; init; } = string.Empty;

    public ICommandBase RefreshQuantumNodesCommand { get; }

    public static AssetViewModel? FindAssetForNode(Guid nodeId)
    {
        NodeToAssetMap.TryGetValue(nodeId, out var asset);
        return asset;
    }

    private void RefreshQuantumViewModel()
    {
        RefreshNodeToAssetMap();
        AssetNodes.Clear();
        foreach (var asset in session.LocalPackages.SelectMany(x => x.Assets))
        {
            var nodes = GetRegisterNodes(asset.PropertyGraph);
            if (nodes == null)
                continue;

            var rootNode = new DebugAssetRootNodeViewModel(ServiceProvider, asset.Url, asset.AssetRootNode, nodes);
            AssetNodes.Add(rootNode);
        }
    }

    private void RefreshNodeToAssetMap()
    {
        foreach (var asset in session.LocalPackages.SelectMany(x => x.Assets))
        {
            var nodes = GetRegisterNodes(asset.PropertyGraph);
            if (nodes == null)
                continue;

            foreach (var node in nodes)
            {
                NodeToAssetMap[node.Guid] = asset;
            }
        }
    }

    private static HashSet<IGraphNode>? GetRegisterNodes(AssetPropertyGraph? propertyGraph)
    {
        if (propertyGraph == null)
            return null;

        var listener = (GraphNodeChangeListener?)FieldInfoListener.GetValue(propertyGraph);
        return (HashSet<IGraphNode>?)FieldInfoRegisteredNodes.GetValue(listener);
    }
}
