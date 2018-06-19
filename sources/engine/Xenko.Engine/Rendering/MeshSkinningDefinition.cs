// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Serialization;

namespace Xenko.Rendering
{
    /// <summary>
    /// Describes skinning for a <see cref="Mesh"/>, through a collection of <see cref="MeshBoneDefinition"/>.
    /// </summary>
    [DataContract]
    public class MeshSkinningDefinition
    {
        /// <summary>
        /// The bones.
        /// </summary>
        public MeshBoneDefinition[] Bones;
    }
}
