// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;

namespace Stride.Rendering
{
    /// <summary>
    /// Describes a bone cluster inside a <see cref="Mesh"/>.
    /// </summary>
    [DataContract]
    public struct MeshBoneDefinition
    {
        /// <summary>
        /// The node index in <see cref="SkeletonUpdater.NodeTransformations"/>.
        /// </summary>
        public int NodeIndex;
        
        /// <summary>
        /// The matrix to transform from mesh space to local space of this bone.
        /// </summary>
        public Matrix LinkToMeshMatrix;
    }
}
