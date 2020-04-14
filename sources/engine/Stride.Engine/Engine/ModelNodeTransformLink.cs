// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Engine
{
    public class ModelNodeTransformLink : TransformLink
    {
        private readonly ModelComponent parentModelComponent;
        private SkeletonUpdater skeleton;
        private int nodesLength;
        private string nodeName;
        private int nodeIndex = int.MaxValue;

        public ModelNodeTransformLink(ModelComponent parentModelComponent, string nodeName)
        {
            this.parentModelComponent = parentModelComponent;
            this.nodeName = nodeName;
        }

        public TransformTRS Transform;

        /// <inheritdoc/>
        public override void ComputeMatrix(bool recursive, out Matrix matrix)
        {
            if (recursive)
            {
                parentModelComponent.Entity.Transform.UpdateWorldMatrix();
            }

            if (parentModelComponent.Skeleton != skeleton || (parentModelComponent.Skeleton != null && parentModelComponent.Skeleton.Nodes.Length != nodesLength))
            {
                skeleton = parentModelComponent.Skeleton;               
                if (skeleton != null)
                {
                    nodesLength = parentModelComponent.Skeleton.Nodes.Length;

                    // Find our node index
                    nodeIndex = int.MaxValue;
                    for (int index = 0; index < skeleton.Nodes.Length; index++)
                    {
                        var node = skeleton.Nodes[index];
                        if (node.Name == nodeName)
                        {
                            nodeIndex = index;
                        }
                    }
                }
            }

            // Updated? (rare slow path)
            if (skeleton != null)
            {
                var nodes = skeleton.Nodes;
                var nodeTransformations = skeleton.NodeTransformations;
                if (nodeIndex < nodes.Length)
                {
                    // Compute
                    matrix = nodeTransformations[nodeIndex].WorldMatrix;
                    return;
                }
            }

            // Fallback to TransformComponent
            matrix = parentModelComponent.Entity.Transform.WorldMatrix;
        }

        public bool NeedsRecreate(Entity parentEntity, string targetNodeName)
        {
            return parentModelComponent.Entity != parentEntity
                || !object.ReferenceEquals(nodeName, targetNodeName); // note: supposed to use same string instance so no need to compare content
        }
    }
}
