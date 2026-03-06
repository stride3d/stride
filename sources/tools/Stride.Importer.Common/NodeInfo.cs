// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Importer.Common
{
    public class NodeInfo
    {
        public string Name;
        public int Depth;
        public bool Preserve;

        /// <summary>
        /// Index of the parent node in the flat node list, or -1 for the root node.
        /// </summary>
        public int ParentIndex = -1;

        /// <summary>
        /// Indices of meshes directly attached to this node.
        /// </summary>
        public List<int> MeshIndices;

        /// <summary>
        /// Local position relative to parent.
        /// </summary>
        public Vector3 LocalPosition;

        /// <summary>
        /// Local rotation relative to parent.
        /// </summary>
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <summary>
        /// Local scale relative to parent.
        /// </summary>
        public Vector3 LocalScale = Vector3.One;
    }
}
