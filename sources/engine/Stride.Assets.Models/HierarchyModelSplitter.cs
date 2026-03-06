// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Engine;
using Stride.Importer.Common;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    /// <summary>
    /// Splits a model source file into multiple <see cref="ModelAsset"/>s — one for each node
    /// that carries meshes — and generates a <see cref="PrefabAsset"/> that mirrors the original
    /// scene hierarchy with <see cref="ModelComponent"/>s on the appropriate entities.
    /// </summary>
    public static class HierarchyModelSplitter
    {
        /// <summary>
        /// Result produced by <see cref="SplitModelByHierarchy"/>.
        /// </summary>
        public class SplitResult
        {
            /// <summary>
            /// Individual model asset items created from the split (one per mesh-bearing node).
            /// </summary>
            public List<AssetItem> ModelAssets { get; } = new List<AssetItem>();

            /// <summary>
            /// The generated prefab asset item that mirrors the original hierarchy.
            /// </summary>
            public AssetItem PrefabAsset { get; set; }
        }

        /// <summary>
        /// Splits the imported scene into per-node <see cref="ModelAsset"/>s and produces a
        /// <see cref="PrefabAsset"/> that reproduces the original hierarchy.
        /// </summary>
        /// <param name="assetSource">Source file path (e.g. the .fbx file).</param>
        /// <param name="localPath">Logical path of the source asset.</param>
        /// <param name="entityInfo">Entity info extracted from the source file.</param>
        /// <param name="existingAssetReferences">Already-imported asset items (materials, textures, skeleton) available for referencing.</param>
        /// <param name="skeletonAsset">Optional skeleton asset, if one was imported.</param>
        /// <returns>A <see cref="SplitResult"/> containing the generated assets.</returns>
        public static SplitResult SplitModelByHierarchy(
            UFile assetSource,
            UFile localPath,
            EntityInfo entityInfo,
            List<AssetItem> existingAssetReferences,
            AssetItem skeletonAsset)
        {
            var result = new SplitResult();
            var hierarchy = entityInfo.SceneHierarchy;

            if (hierarchy == null || hierarchy.Nodes.Count == 0)
                return result;

            var baseName = localPath.GetFileNameWithoutExtension();
            var loadedMaterials = existingAssetReferences.Where(x => x.Asset is MaterialAsset).ToList();

            // Track which nodes got a ModelAsset so we can wire up the prefab
            var nodeIndexToModelAsset = new Dictionary<int, AssetItem>();

            // Track used asset names to avoid duplicate URLs
            var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Detect root wrapper node (common in FBX files — a single root node with no meshes)
            int rootWrapperIndex = DetectRootWrapperNode(hierarchy);

            // --- 1. Create one ModelAsset per mesh-bearing node ---
            for (int nodeIndex = 0; nodeIndex < hierarchy.Nodes.Count; nodeIndex++)
            {
                // Skip the root wrapper node itself (it has no meshes by definition)
                if (nodeIndex == rootWrapperIndex)
                    continue;

                var node = hierarchy.Nodes[nodeIndex];
                if (node.MeshIndices == null || node.MeshIndices.Count == 0)
                    continue;

                var modelAsset = new ModelAsset { Source = assetSource };

                // Set the node filter so the compiler only includes meshes from this node
                modelAsset.NodeFilter = new List<int> { nodeIndex };

                // Detect if any mesh on this node has skinning (bone) data
                bool nodeHasSkinning = false;
                foreach (var meshIndex in node.MeshIndices)
                {
                    if (hierarchy.MeshIndexToHasSkinning.TryGetValue(meshIndex, out var hasSkin) && hasSkin)
                    {
                        nodeHasSkinning = true;
                        break;
                    }
                }

                // For non-skinned sub-models: MergeMeshes = true, no skeleton.
                // The entity transform in the prefab handles positioning. Vertex data stays
                // in node-local space (ExportModel skips baking when NodeFilter is set and
                // skeleton is null).
                //
                // For skinned sub-models: keep skeleton so bone deformation works at runtime.
                // MergeMeshes must be false to preserve per-mesh skinning data.
                if (nodeHasSkinning && skeletonAsset != null)
                {
                    modelAsset.MergeMeshes = false;
                    modelAsset.Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(
                        skeletonAsset.Id, skeletonAsset.Location);
                }
                else
                {
                    modelAsset.MergeMeshes = true;
                    // No skeleton — entity transform handles positioning
                }

                // Include ALL materials from the source file so that Mesh.MaterialIndex
                // (which is a scene-wide Assimp index) remains valid during compilation.
                // ExportModel compacts unused materials at compile time, so the runtime
                // Model only contains the materials this sub-model actually references.
                foreach (var material in entityInfo.Materials)
                {
                    var modelMaterial = new ModelMaterial
                    {
                        Name = material.Key,
                        MaterialInstance = new MaterialInstance()
                    };

                    var foundMaterial = loadedMaterials.FirstOrDefault(x => x.Location == new UFile(material.Key));
                    if (foundMaterial != null)
                    {
                        var reference = AttachedReferenceManager.CreateProxyObject<Material>(
                            foundMaterial.Id, foundMaterial.Location);
                        modelMaterial.MaterialInstance.Material = reference;
                    }

                    modelAsset.Materials.Add(modelMaterial);
                }

                if (modelAsset.Materials.Count == 0)
                {
                    modelAsset.Materials.Add(new ModelMaterial
                    {
                        Name = "Material",
                        MaterialInstance = new MaterialInstance()
                    });
                }

                // Use a sanitised, uniquified node name to avoid file-system and URL collisions
                var safeName = GetUniqueName(SanitizeName(node.Name), usedNames);
                var modelUrl = new UFile($"{baseName}/{safeName}");
                var assetItem = new AssetItem(modelUrl, modelAsset);
                result.ModelAssets.Add(assetItem);
                nodeIndexToModelAsset[nodeIndex] = assetItem;
            }

            // --- 2. Build the Prefab ---
            result.PrefabAsset = BuildPrefab(baseName, hierarchy, nodeIndexToModelAsset, rootWrapperIndex);

            return result;
        }

        /// <summary>
        /// Detects a root wrapper node — a single root node at depth 0 with no meshes.
        /// FBX files typically have a "RootNode" wrapper; other formats may not.
        /// Returns the node index of the wrapper, or -1 if no wrapper was detected.
        /// </summary>
        private static int DetectRootWrapperNode(SceneHierarchyInfo hierarchy)
        {
            if (hierarchy.Nodes.Count == 0)
                return -1;

            var root = hierarchy.Nodes[0];

            // Must be depth 0, have no meshes, and be the only root-level node
            if (root.Depth != 0 || root.ParentIndex != -1)
                return -1;

            if (root.MeshIndices != null && root.MeshIndices.Count > 0)
                return -1;

            // Check that no other nodes are also at root level
            for (int i = 1; i < hierarchy.Nodes.Count; i++)
            {
                if (hierarchy.Nodes[i].ParentIndex == -1)
                    return -1; // Multiple root nodes — no single wrapper
            }

            return 0;
        }

        /// <summary>
        /// Builds a <see cref="PrefabAsset"/> whose entity hierarchy mirrors the scene nodes.
        /// Entities that correspond to mesh-bearing nodes get a <see cref="ModelComponent"/> pointing
        /// at the split <see cref="ModelAsset"/>.
        /// </summary>
        /// <param name="baseName">Base name for the prefab asset URL.</param>
        /// <param name="hierarchy">Scene hierarchy data.</param>
        /// <param name="nodeIndexToModelAsset">Mapping from node index to the created model asset.</param>
        /// <param name="rootWrapperIndex">Index of the root wrapper node to skip, or -1.</param>
        private static AssetItem BuildPrefab(
            string baseName,
            SceneHierarchyInfo hierarchy,
            Dictionary<int, AssetItem> nodeIndexToModelAsset,
            int rootWrapperIndex)
        {
            var prefab = new PrefabAsset();

            // Create entities for every node (except the root wrapper)
            var entities = new Entity[hierarchy.Nodes.Count];

            for (int i = 0; i < hierarchy.Nodes.Count; i++)
            {
                if (i == rootWrapperIndex)
                    continue;

                var node = hierarchy.Nodes[i];
                var entity = new Entity(node.Name, node.LocalPosition, node.LocalRotation, node.LocalScale);

                // Attach a ModelComponent if this node has meshes
                if (nodeIndexToModelAsset.TryGetValue(i, out var modelAssetItem))
                {
                    var modelRef = AttachedReferenceManager.CreateProxyObject<Model>(
                        modelAssetItem.Id, modelAssetItem.Location);
                    entity.Components.Add(new ModelComponent(modelRef));
                }

                entities[i] = entity;
            }

            // Wire up parent-child relationships via TransformComponent
            for (int i = 0; i < hierarchy.Nodes.Count; i++)
            {
                if (i == rootWrapperIndex || entities[i] == null)
                    continue;

                var parentIndex = hierarchy.Nodes[i].ParentIndex;

                // If our parent is the root wrapper, skip up to its parent (or make this a root entity)
                if (parentIndex == rootWrapperIndex)
                    parentIndex = -1;

                if (parentIndex >= 0 && parentIndex < entities.Length && entities[parentIndex] != null)
                {
                    entities[parentIndex].Transform.Children.Add(entities[i].Transform);
                }
            }

            // Register all entities in the prefab
            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] == null)
                    continue;

                var design = new EntityDesign(entities[i]);
                prefab.Hierarchy.Parts.Add(design);

                // Determine effective parent
                var parentIndex = hierarchy.Nodes[i].ParentIndex;
                if (parentIndex == rootWrapperIndex)
                    parentIndex = -1;

                // Root-level entities (no parent, or parent was the skipped wrapper)
                if (parentIndex < 0 || (parentIndex >= 0 && entities[parentIndex] == null))
                {
                    prefab.Hierarchy.RootParts.Add(entities[i]);
                }
            }

            var prefabUrl = new UFile($"{baseName} Prefab");
            return new AssetItem(prefabUrl, prefab);
        }

        /// <summary>
        /// Returns a unique name by appending a numeric suffix if the name has already been used.
        /// </summary>
        private static string GetUniqueName(string baseName, Dictionary<string, int> usedNames)
        {
            if (!usedNames.TryGetValue(baseName, out var count))
            {
                usedNames[baseName] = 1;
                return baseName;
            }

            usedNames[baseName] = count + 1;
            var uniqueName = $"{baseName}_{count}";

            // Ensure the suffixed name is also unique
            while (usedNames.ContainsKey(uniqueName))
            {
                count++;
                usedNames[baseName] = count + 1;
                uniqueName = $"{baseName}_{count}";
            }

            usedNames[uniqueName] = 1;
            return uniqueName;
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Node";

            var chars = name.ToCharArray();
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalidChars, chars[i]) >= 0)
                    chars[i] = '_';
            }
            return new string(chars);
        }
    }
}
