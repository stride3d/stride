// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Stride.Importer.Common;

namespace Stride.Assets.Models
{
    /// <summary>
    /// Describes a single per-mesh model produced by a hierarchical model import.
    /// </summary>
    public sealed class HierarchicalModelEntry
    {
        /// <summary>The asset name of the generated model (unique inside the import).</summary>
        public string ModelName { get; init; }

        /// <summary>The name of the source mesh this model was generated from.</summary>
        public string MeshName { get; init; }

        /// <summary>The name of the source node that hosts the mesh.</summary>
        public string NodeName { get; init; }

        /// <summary>The material names used by this mesh only.</summary>
        public IReadOnlyList<string> MaterialNames { get; init; }
    }

    /// <summary>
    /// A node of the prefab that mirrors the imported model node hierarchy.
    /// </summary>
    public sealed class HierarchicalPrefabNode
    {
        /// <summary>The name of the source node, used as the generated entity name.</summary>
        public string Name { get; init; }

        /// <summary>The generated per-mesh model names hosted by this node (empty when the node has no mesh).</summary>
        public IReadOnlyList<string> ModelNames { get; set; } = Array.Empty<string>();

        /// <summary>The child nodes, in source order.</summary>
        public List<HierarchicalPrefabNode> Children { get; } = new List<HierarchicalPrefabNode>();
    }

    /// <summary>
    /// The result of planning a hierarchical model import: the per-mesh models and the prefab hierarchy.
    /// </summary>
    public sealed class HierarchicalImportPlan
    {
        /// <summary>The name of the generated prefab asset.</summary>
        public string PrefabName { get; init; }

        /// <summary>One entry per mesh of the source model.</summary>
        public List<HierarchicalModelEntry> Models { get; } = new List<HierarchicalModelEntry>();

        /// <summary>The root of the prefab hierarchy that mirrors the source model, or null when there are no nodes.</summary>
        public HierarchicalPrefabNode Root { get; set; }
    }

    /// <summary>
    /// Splits an imported model into one model per mesh and builds a plan for a prefab that mirrors the
    /// source node hierarchy. This is the algorithmic core of the hierarchical FBX import feature.
    /// </summary>
    /// <remarks>
    /// The input is the same intermediate representation the Assimp importer exposes through
    /// <see cref="EntityInfo"/>: a depth-first list of nodes (<see cref="NodeInfo"/>) and the meshes
    /// attached to them (<see cref="MeshParameters"/>). The algorithm is pure: it keeps no state between
    /// calls and is therefore safe to use concurrently.
    /// </remarks>
    public static class HierarchicalModelImporter
    {
        // Characters that are invalid in a file name on at least one supported platform. The set is fixed
        // (instead of Path.GetInvalidFileNameChars) so the generated names do not depend on the host OS.
        private static readonly char[] InvalidNameChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0' };

        /// <summary>
        /// Creates an import plan that splits <paramref name="meshes"/> into one model each and mirrors
        /// the <paramref name="nodes"/> hierarchy into a prefab.
        /// </summary>
        /// <param name="modelName">The name of the imported model (usually the source file name without extension).</param>
        /// <param name="nodes">The node hierarchy, in depth-first pre-order, as produced by the importer.</param>
        /// <param name="meshes">The meshes of the model, as produced by the importer.</param>
        /// <returns>The import plan.</returns>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="modelName"/> is empty or whitespace, the node list is empty, a node has a negative
        /// depth, the hierarchy does not start at depth 0, or a node skips a depth level.
        /// </exception>
        public static HierarchicalImportPlan CreatePlan(string modelName, IReadOnlyList<NodeInfo> nodes, IReadOnlyList<MeshParameters> meshes)
        {
            if (modelName == null)
                throw new ArgumentNullException(nameof(modelName));
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes));

            if (modelName.Trim().Length == 0)
                throw new ArgumentException("The model name must not be empty or whitespace.", nameof(modelName));
            if (nodes.Count == 0)
                throw new ArgumentException("The node hierarchy must contain at least the root node.", nameof(nodes));

            var safeModelName = Sanitize(modelName.Trim());
            var plan = new HierarchicalImportPlan { PrefabName = Sanitize(safeModelName + " Prefab") };

            // Build the node tree. Nodes come in depth-first pre-order and carry their depth, so a stack of
            // the most recent node at each level reconstructs the parent/child links.
            var entities = new List<HierarchicalPrefabNode>(nodes.Count);
            var path = new List<HierarchicalPrefabNode>(8);
            for (var i = 0; i < nodes.Count; i++)
            {
                var depth = nodes[i].Depth;
                if (depth < 0)
                    throw new ArgumentException($"The node '{nodes[i].Name}' has an invalid depth ({depth}); depth must not be negative.", nameof(nodes));
                if (i == 0 && depth != 0)
                    throw new ArgumentException($"The node hierarchy must start at depth 0 but starts at depth {depth}.", nameof(nodes));
                if (depth > path.Count)
                    throw new ArgumentException($"The node '{nodes[i].Name}' is at depth {depth} but its parent is missing (the previous node was at depth {path.Count - 1}).", nameof(nodes));

                // Drop the nodes that are no longer ancestors of this node.
                if (path.Count > depth)
                    path.RemoveRange(depth, path.Count - depth);

                var entity = new HierarchicalPrefabNode { Name = nodes[i].Name };
                if (path.Count > 0)
                    path[path.Count - 1].Children.Add(entity);
                path.Add(entity);
                entities.Add(entity);
            }

            var root = path.Count > 0 ? path[0] : entities[0];
            return BuildPlan(plan, root, entities, safeModelName, meshes);
        }

        private static HierarchicalImportPlan BuildPlan(HierarchicalImportPlan plan, HierarchicalPrefabNode root,
            List<HierarchicalPrefabNode> entities, string safeModelName, IReadOnlyList<MeshParameters> meshes)
        {
            // Attach the meshes to their node. A mesh whose node cannot be resolved falls back to the root so
            // no geometry is silently dropped.
            var nodeByName = new Dictionary<string, HierarchicalPrefabNode>(StringComparer.Ordinal);
            foreach (var entity in entities)
                nodeByName.TryAdd(entity.Name ?? string.Empty, entity);

            var modelNamesPerNode = new Dictionary<HierarchicalPrefabNode, List<string>>();
            var usedModelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mesh in meshes)
            {
                HierarchicalPrefabNode host = null;
                if (!string.IsNullOrEmpty(mesh.NodeName))
                    nodeByName.TryGetValue(mesh.NodeName, out host);
                host ??= root;

                var baseName = Sanitize(safeModelName + "-" + Sanitize(string.IsNullOrEmpty(mesh.MeshName) ? "Mesh" : mesh.MeshName));
                var modelName = MakeUnique(baseName, usedModelNames);
                plan.Models.Add(new HierarchicalModelEntry
                {
                    ModelName = modelName,
                    MeshName = mesh.MeshName,
                    NodeName = host.Name,
                    MaterialNames = string.IsNullOrEmpty(mesh.MaterialName) ? Array.Empty<string>() : new[] { mesh.MaterialName },
                });

                if (!modelNamesPerNode.TryGetValue(host, out var modelNames))
                    modelNamesPerNode[host] = modelNames = new List<string>();
                modelNames.Add(modelName);
            }

            // Materialize the per-node model name lists on the tree.
            AssignModelNames(root, modelNamesPerNode);

            plan.Root = root;
            return plan;
        }

        private static void AssignModelNames(HierarchicalPrefabNode node, Dictionary<HierarchicalPrefabNode, List<string>> modelNamesPerNode)
        {
            if (modelNamesPerNode.TryGetValue(node, out var modelNames))
                node.ModelNames = modelNames;
            foreach (var child in node.Children)
                AssignModelNames(child, modelNamesPerNode);
        }

        private static string MakeUnique(string baseName, HashSet<string> usedNames)
        {
            if (usedNames.Add(baseName))
                return baseName;

            for (var suffix = 1; ; suffix++)
            {
                var candidate = baseName + "-" + suffix;
                if (usedNames.Add(candidate))
                    return candidate;
            }
        }

        private static string Sanitize(string name)
        {
            var builder = new StringBuilder(name.Length);
            foreach (var c in name)
                builder.Append(Array.IndexOf(InvalidNameChars, c) >= 0 ? '_' : c);
            return builder.ToString().Trim();
        }
    }
}
