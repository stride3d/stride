// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Assets.Materials;

namespace Stride.Importer.Common
{
    public class EntityInfo
    {
        public List<string> TextureDependencies;
        public Dictionary<string, MaterialAsset> Materials;
        public List<string> AnimationNodes;
        public List<MeshParameters> Models;
        public List<NodeInfo> Nodes;
        public List<string> MaterialOrder { get; set; }
        public Dictionary<string, List<int>> NodeNameToMaterialIndices { get; set; } = new Dictionary<string, List<int>>(StringComparer.Ordinal);
    }
}
