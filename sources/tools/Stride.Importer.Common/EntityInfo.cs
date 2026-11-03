// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Materials;

namespace Stride.Importer.Common
{
    public class EntityInfo
    {
        public List<string> TextureDependencies;

        /// <summary>
        /// The role of each texture in <see cref="TextureDependencies"/>, by path, when the source format defines one.
        /// Textures without an entry are imported as color textures.
        /// </summary>
        public Dictionary<string, TextureRole> TextureRoles;
        public Dictionary<string, MaterialAsset> Materials;
        public List<string> AnimationNodes;
        public List<MeshParameters> Models;
        public List<NodeInfo> Nodes;
        public List<string> MorphTargetNames;
    }
}
