// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Compiler;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Analysis
{
    /// <summary>
    /// Build dependency manager
    /// Basically is a container of BuildAssetNode
    /// </summary>
    public class BuildDependencyManager
    {
        /// <summary>
        /// A structure used as key of the dictionary containing all the build nodes
        /// </summary>
        private struct BuildNodeDesc : IEquatable<BuildNodeDesc>
        {
            public readonly AssetId AssetId;
            public readonly Type CompilationContext;

            public BuildNodeDesc(AssetId assetId, Type compilationContext)
            {
                AssetId = assetId;
                CompilationContext = compilationContext;
            }

            public bool Equals(BuildNodeDesc other)
            {
                return AssetId.Equals(other.AssetId) && ReferenceEquals(CompilationContext, other.CompilationContext);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is BuildNodeDesc && Equals((BuildNodeDesc)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (AssetId.GetHashCode() * 397) ^ (CompilationContext?.GetHashCode() ?? 0);
                }
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
        public static readonly AssetCompilerRegistry AssetCompilerRegistry = new AssetCompilerRegistry();

        private readonly ConcurrentDictionary<BuildNodeDesc, BuildAssetNode> nodes = new ConcurrentDictionary<BuildNodeDesc, BuildAssetNode>();

        /// <summary>
        /// Finds or creates a node, notice that this will not perform an analysis on the node, which must be explicitly called on the node
        /// </summary>
        /// <param name="item">The asset item to find or create</param>
        /// <param name="compilationContext">The context in which the asset is compiled.</param>
        /// <returns>The build node associated with item</returns>
        public BuildAssetNode FindOrCreateNode([NotNull] AssetItem item, [NotNull] Type compilationContext)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (compilationContext == null) throw new ArgumentNullException(nameof(compilationContext));

            var nodeDesc = new BuildNodeDesc(item.Id, compilationContext);

            BuildAssetNode node;
            if (!nodes.TryGetValue(nodeDesc, out node))
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
        //private static void AnalyzeNode([NotNull] BuildAssetNode node, [NotNull] AssetCompilerContext context)
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
        public BuildAssetNode FindNode([NotNull] AssetItem item, [NotNull] Type compilationContext)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (compilationContext == null) throw new ArgumentNullException(nameof(compilationContext));

            var nodeDesc = new BuildNodeDesc(item.Id, compilationContext);

            if (!nodes.TryGetValue(nodeDesc, out var node))
            {
                return null;
            }

            if (!ReferenceEquals(node.AssetItem, item))
            {
                nodes.TryRemove(nodeDesc, out node);
                return null;
            }

            return node;
        }

        /// <summary>
        /// Finds all the nodes associated with the asset
        /// </summary>
        /// <param name="item">The asset item to find</param>
        /// <returns>The build nodes associated with item or null if it was not found</returns>
        public IEnumerable<BuildAssetNode> FindNodes([NotNull] AssetItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return nodes.Where(x => x.Value.AssetItem == item).Select(x => x.Value);
        }

        /// <summary>
        /// Removes the node from the build graph
        /// </summary>
        /// <param name="node">The node to remove</param>
        public void RemoveNode([NotNull] BuildAssetNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var nodeDesc = new BuildNodeDesc(node.AssetItem.Id, node.CompilationContext);
            nodes.TryRemove(nodeDesc, out node);
        }

        /// <summary>
        /// Removes the nodes associated with item from the build graph
        /// </summary>
        /// <param name="item">The item to use to find nodes to remove</param>
        public void RemoveNode([NotNull] AssetItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var assetNodes = FindNodes(item).ToList();
            foreach (var buildAssetNode in assetNodes)
            {
                var nodeDesc = new BuildNodeDesc(item.Id, buildAssetNode.CompilationContext);
                nodes.TryRemove(nodeDesc, out _);
            }
        }
    }
}
