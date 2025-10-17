// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Importer.Common
{
    public class MeshParameters
    {
        public List<string> MaterialNames { get; set; } = new();
        public List<int> MaterialIndices { get; set; } = new(); 

        public string MeshName;
        public string NodeName;
        public HashSet<string> BoneNodes;
    }
}
