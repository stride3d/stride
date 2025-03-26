// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Quantum;
using Stride.Core.Quantum.References;

namespace Stride.Core.Assets.Quantum.Visitors;

/// <summary>
/// A visitor that collects all assets from an object containing multiple assets
/// </summary>
public class AssetCollector : GraphVisitorBase
{
    private readonly Dictionary<GraphNodePath, Asset> assets = [];

    private AssetCollector()
    {
    }

    /// <summary>
    /// Collects all assets referenced by the object in the given node.
    /// </summary>
    /// <param name="root">The root node to visit to collect assets.</param>
    /// <returns>A collection containing all assets found by visiting the given root.</returns>
    public static IReadOnlyDictionary<GraphNodePath, Asset> Collect(IObjectNode root)
    {
        var visitor = new AssetCollector();
        visitor.Visit(root);
        return visitor.assets;
    }

    /// <inheritdoc/>
    protected override void VisitReference(IGraphNode referencer, ObjectReference reference)
    {
        if (reference.TargetNode?.Retrieve() is Asset asset)
        {
            assets.Add(CurrentPath.Clone(), asset);
        }
        // Don't continue the visit once we found an asset, we cannot have nested assets.
        else
        {
            base.VisitReference(referencer, reference);
        }
    }
}
