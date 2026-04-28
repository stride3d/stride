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

        // -1 for root nodes
        public int ParentIndex = -1;

        public List<int> MeshIndices;

        public Vector3 LocalPosition;
        public Quaternion LocalRotation = Quaternion.Identity;
        public Vector3 LocalScale = Vector3.One;
    }
}
