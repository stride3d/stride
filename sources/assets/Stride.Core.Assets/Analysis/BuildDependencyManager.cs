// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Assets.Compiler;

namespace Stride.Core.Assets.Analysis;

/// <summary>
/// Build dependency manager
/// Basically is a container of BuildAssetNode
/// </summary>
public class BuildDependencyManager
{
    /// <summary>
    /// A structure used as key of the dictionary containing all the build nodes
    /// </summary>
    private readonly struct BuildNodeDesc : IEquatable<BuildNodeDesc>
    {
        public readonly AssetId AssetId;
        public readonly Type CompilationContext;

        public BuildNodeDesc(AssetId assetId, Type compilationContext)
        {
            AssetId = assetId;
            CompilationContext = compilationContext;
        }

        public readonly bool Equals(BuildNodeDesc other)
        {
            return AssetId.Equals(other.AssetId) && ReferenceEquals(CompilationContext, other.CompilationContext);
        }

        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BuildNodeDesc desc && Equals(desc);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(AssetId, CompilationContext);
        }

        public static bool operator ==(BuildNodeDesc left, BuildNodeDesc right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BuildNodeDesc left, BuildNodeDesc right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// The AssetCompilerRegistry, here mostly for ease of access
    /// </summary>
    public static readonly AssetCompilerRegistry AssetCompilerRegistry = new();

    private readonly ConcurrentDictionary<BuildNodeDesc, BuildAssetNode> nodes = new();

    /// <summary>
    /// Finds or creates a node, notice that this will not perform an analysis on the node, which must be explicitly called on the node
    /// </summary>
    /// <param name="item">The asset item to find or create</param>
    /// <param name="compilationContext">The context in which the asset is compiled.</param>
    /// <returns>The build node associated with item</returns>
    public BuildAssetNode FindOrCreateNode(AssetItem item, Type compilationContext)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(compilationContext);

        var nodeDesc = new BuildNodeDesc(item.Id, compilationContext);

        if (!nodes.TryGetValue(nodeDesc, out var node))
        {
            node = new BuildAssetNode(item, compilationContext, this);
            nodes.TryAdd(nodeDesc, node);
        }
        else if (!ReferenceEquals(node.AssetItem, item))
        {
            node = new BuildAssetNode(item, compilationContext, this);
            nodes[nodeDesc] = node;
        }

        return node;
    }

    // TODO: this should be reimplemented at the service level (that consumes the build graph - thumbnails, preview, scene editors...)
    //private static void AnalyzeNode(BuildAssetNode node, AssetCompilerContext context)
    //{
    //    if (node == null) throw new ArgumentNullException(nameof(node));
    //    if (context == null) throw new ArgumentNullException(nameof(context));
    //    node.Analyze(context);
    //    foreach (var reference in node.References)
    //    {
    //        AnalyzeNode(reference.Target, context);
    //    }
    //}

    //public void AssetChanged(AssetItem sender)
    //{
    //    //var node = FindOrCreateNode(sender, typeof(AssetCompilationContext)); // update only runtime ones ( as they are root )
    //    //var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
    //    //AnalyzeNode(node, context);
    //}

    /// <summary>
    /// Finds a node, notice that this will not perform an analysis on the node, which must be explicitly called on the node
    /// </summary>
    /// <param name="item">The asset item to find</param>
    /// <param name="compilationContext">The context in which the asset is compiled.</param>
    /// <returns>The build node associated with item or null if it was not found</returns>
    public BuildAssetNode? FindNode(AssetItem item, Type compilationContext)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(compilationContext);

        var nodeDesc = new BuildNodeDesc(item.Id, compilationContext);

        if (!nodes.TryGetValue(nodeDesc, out var node))
        {
            return null;
        }

        if (!ReferenceEquals(node.AssetItem, item))
        {
            nodes.TryRemove(nodeDesc, out _);
            return null;
        }

        return node;
    }

    /// <summary>
    /// Finds all the nodes associated with the asset
    /// </summary>
    /// <param name="item">The asset item to find</param>
    /// <returns>The build nodes associated with item or null if it was not found</returns>
    public IEnumerable<BuildAssetNode> FindNodes(AssetItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return nodes.Where(x => x.Value.AssetItem == item).Select(x => x.Value);
    }

    /// <summary>
    /// Removes the node from the build graph
    /// </summary>
    /// <param name="node">The node to remove</param>
    public void RemoveNode(BuildAssetNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var nodeDesc = new BuildNodeDesc(node.AssetItem.Id, node.CompilationContext);
        nodes.TryRemove(nodeDesc, out _);
    }

    /// <summary>
    /// Removes the nodes associated with item from the build graph
    /// </summary>
    /// <param name="item">The item to use to find nodes to remove</param>
    public void RemoveNode(AssetItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var assetNodes = FindNodes(item).ToList();
        foreach (var buildAssetNode in assetNodes)
        {
            var nodeDesc = new BuildNodeDesc(item.Id, buildAssetNode.CompilationContext);
            nodes.TryRemove(nodeDesc, out _);
        }
    }
}
