// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Quantum;

public class AssetPropertyGraphContainer
{
    private readonly Dictionary<AssetId, AssetPropertyGraph> registeredGraphs = [];

    public AssetPropertyGraphContainer(AssetNodeContainer nodeContainer)
    {
        NodeContainer = nodeContainer ?? throw new ArgumentNullException(nameof(nodeContainer));
    }

    public AssetNodeContainer NodeContainer { get; }

    public bool PropagateChangesFromBase { get; set; } = true;

    public AssetPropertyGraph? InitializeAsset(AssetItem assetItem, ILogger logger)
    {
        // SourceCodeAssets have no property
        if (assetItem.Asset is SourceCodeAsset)
            return null;

        var graph = AssetQuantumRegistry.ConstructPropertyGraph(this, assetItem, logger);
        RegisterGraph(graph);
        return graph;
    }

    public AssetPropertyGraph? TryGetGraph(AssetId assetId)
    {
        registeredGraphs.TryGetValue(assetId, out var graph);
        return graph;
    }

    public void RegisterGraph(AssetPropertyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        registeredGraphs.Add(graph.Id, graph);
    }

    public bool UnregisterGraph(AssetId assetId)
    {
        return registeredGraphs.Remove(assetId);
    }
}
