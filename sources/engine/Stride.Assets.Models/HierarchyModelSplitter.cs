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
    /// Splits a model into per-node <see cref="ModelAsset"/>s and generates a <see cref="PrefabAsset"/> mirroring the scene hierarchy.
    /// </summary>
    public static class HierarchyModelSplitter
    {
        public class SplitResult
        {
            public List<AssetItem> ModelAssets { get; } = new List<AssetItem>();
            public AssetItem PrefabAsset { get; set; }
        }

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

                // Skinned: keep skeleton + separate meshes. Non-skinned: merge meshes, entity transform handles positioning.
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

                // Include all materials so MaterialIndex stays valid; ExportModel compacts unused ones.
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

                var safeName = GetUniqueName(SanitizeName(node.Name), usedNames);
                var modelUrl = new UFile($"{baseName}/{safeName}");
                var assetItem = new AssetItem(modelUrl, modelAsset);
                result.ModelAssets.Add(assetItem);
                nodeIndexToModelAsset[nodeIndex] = assetItem;
            }

            // --- 2. Build the Prefab (only if we created at least one sub-model) ---
                if (nodeIndexToModelAsset.Count > 0)
                {
                    result.PrefabAsset = BuildPrefab(baseName, hierarchy, nodeIndexToModelAsset, rootWrapperIndex);
                }

                return result;
        }

        // Returns the index of a meshless, near-identity root wrapper node, or -1.
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

            // Keep the root if it has a meaningful transform (rotation, scale, offset)
            if (!IsNearIdentityTransform(root))
                return -1;

            // Check that no other nodes are also at root level
            for (int i = 1; i < hierarchy.Nodes.Count; i++)
            {
                if (hierarchy.Nodes[i].ParentIndex == -1)
                    return -1; // Multiple root nodes — no single wrapper
            }

            return 0;
        }

        private const float NearIdentityEpsilon = 1e-4f;

        private static bool IsNearIdentityTransform(NodeInfo node)
        {
            if (node.LocalPosition.Length() > NearIdentityEpsilon)
                return false;

            if ((node.LocalScale - Vector3.One).Length() > NearIdentityEpsilon)
                return false;

            // Quaternion q and -q represent the same rotation; use absolute dot
            var dot = Quaternion.Dot(node.LocalRotation, Quaternion.Identity);
            if (MathF.Abs(dot) < 1.0f - NearIdentityEpsilon)
                return false;

            return true;
        }

        // Builds a prefab mirroring the scene hierarchy, attaching ModelComponents to mesh-bearing nodes.
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
                var entityName = string.IsNullOrWhiteSpace(node.Name) ? $"Node_{i}" : node.Name;
                var entity = new Entity(entityName, node.LocalPosition, node.LocalRotation, node.LocalScale);

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

        private static readonly HashSet<string> ReservedDeviceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

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

            var result = new string(chars).Trim().Trim('.');

            if (string.IsNullOrWhiteSpace(result))
                return "Node";

            if (ReservedDeviceNames.Contains(result))
                return $"_{result}_";

            return result;
        }
    }
}
