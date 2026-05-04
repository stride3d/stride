// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Importer.Common
{
    /// <summary>
    /// Represents the full scene hierarchy extracted from a model file (FBX, glTF, etc.).
    /// Contains the node tree with parent-child relationships, per-node mesh assignments,
    /// and local transforms — everything needed to split a model into per-node assets
    /// and reconstruct the hierarchy as a prefab.
    /// </summary>
    public class SceneHierarchyInfo
    {
        /// <summary>
        /// Flat list of all nodes in the scene, ordered by depth-first traversal.
        /// Each node stores its parent index, local transform, and associated mesh indices.
        /// </summary>
        public List<NodeInfo> Nodes { get; set; } = new List<NodeInfo>();

        /// <summary>
        /// Mapping from mesh index (in the source file) to the name of the material assigned to that mesh.
        /// </summary>
        public Dictionary<int, string> MeshIndexToMaterialName { get; set; } = new Dictionary<int, string>();

        /// <summary>
        /// Mapping from mesh index (in the source file) to the mesh name.
        /// </summary>
        public Dictionary<int, string> MeshIndexToMeshName { get; set; } = new Dictionary<int, string>();

        /// <summary>
        /// Mapping from mesh index (in the source file) to the Assimp material index.
        /// This is the ground truth for material ordering, ensuring MaterialIndex on compiled
        /// meshes stays consistent regardless of how materials are listed in EntityInfo.
        /// </summary>
        public Dictionary<int, int> MeshIndexToMaterialIndex { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Mapping from mesh index (in the source file) to whether the mesh has bone/skinning data.
        /// </summary>
        public Dictionary<int, bool> MeshIndexToHasSkinning { get; set; } = new Dictionary<int, bool>();

        /// <summary>
        /// Returns the indices of all child nodes for the given node index.
        /// </summary>
        public List<int> GetChildIndices(int parentIndex)
        {
            var children = new List<int>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].ParentIndex == parentIndex)
                    children.Add(i);
            }
            return children;
        }

        /// <summary>
        /// Returns whether the given node or any of its descendants have meshes attached.
        /// </summary>
        public bool HasMeshesInSubtree(int nodeIndex)
        {
            var node = Nodes[nodeIndex];
            if (node.MeshIndices != null && node.MeshIndices.Count > 0)
                return true;

            foreach (var childIndex in GetChildIndices(nodeIndex))
            {
                if (HasMeshesInSubtree(childIndex))
                    return true;
            }
            return false;
        }
    }
}
