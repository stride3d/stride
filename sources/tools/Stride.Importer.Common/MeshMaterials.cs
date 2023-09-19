// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Materials;

namespace Stride.Importer.Common
{
    public class MeshMaterials
    {
        public Dictionary<string, MaterialAsset> Materials;
        public List<MeshParameters> Models;
        public List<string> BoneNodes;
    }
}
