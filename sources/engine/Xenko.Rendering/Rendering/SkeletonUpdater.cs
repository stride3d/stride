// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Rendering
{
    /// <summary>
    /// Performs hierarchical updates for a given <see cref="Model"/>.
    /// </summary>
    [DataContract] // Here for update engine; TODO: better separation and different attribute?
    public class SkeletonUpdater
    {
        private ModelNodeDefinition[] nodes;
        private ModelNodeTransformation[] nodeTransformations;

        private int matrixCounter;

        public ModelNodeDefinition[] Nodes
        {
            get { return nodes; }
        }

        public ModelNodeTransformation[] NodeTransformations
        {
            get { return nodeTransformations; }
        }

        private static ModelNodeDefinition[] GetDefaultNodeDefinitions()
        {
            return new[] { new ModelNodeDefinition { Name = "Root", ParentIndex = -1, Transform = { Scale = Vector3.One }, Flags = ModelNodeFlags.Default } };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkeletonUpdater" /> class.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        public SkeletonUpdater(Skeleton skeleton)
        {
            Initialize(skeleton);
        }

        public void Initialize(Skeleton skeleton)
        {
            var newNodes = skeleton?.Nodes;

            if (this.nodes == newNodes && this.nodes != null)
            {
                return;
            }

            this.nodes = newNodes ?? GetDefaultNodeDefinitions();

            if (nodeTransformations == null || nodeTransformations.Length < this.nodes.Length)
                nodeTransformations = new ModelNodeTransformation[this.nodes.Length];

            for (int index = 0; index < nodes.Length; index++)
            {
                nodeTransformations[index].ParentIndex = nodes[index].ParentIndex;
                nodeTransformations[index].Transform = nodes[index].Transform;
                nodeTransformations[index].Flags = nodes[index].Flags;
                nodeTransformations[index].RenderingEnabledRecursive = true;
                UpdateLocalMatrix(ref nodeTransformations[index]);
            }

            nodeTransformations[0].Flags &= ~ModelNodeFlags.EnableTransform;
        }

        /// <summary>
        /// Resets initial values.
        /// </summary>
        public void ResetInitialValues()
        {
            var nodesLocal = nodes;
            for (int index = 0; index < nodesLocal.Length; index++)
            {
                nodeTransformations[index].Transform = nodesLocal[index].Transform;
            }
        }

        /// <summary>
        /// For each node, updates the world matrices from local matrices.
        /// </summary>
        public void UpdateMatrices()
        {
            // Compute transformations
            var nodesLength = nodes.Length;
            for (int index = 0; index < nodesLength; index++)
            {
                UpdateNode(ref nodeTransformations[index]);
            }
            matrixCounter++;
        }

        public void GetWorldMatrix(int index, out Matrix matrix)
        {
            matrix = nodeTransformations[index].WorldMatrix;
        }

        public void GetLocalMatrix(int index, out Matrix matrix)
        {
            matrix = nodeTransformations[index].LocalMatrix;
        }

        private void UpdateNode(ref ModelNodeTransformation node)
        {
            // Compute LocalMatrix
            if ((node.Flags & ModelNodeFlags.EnableTransform) == ModelNodeFlags.EnableTransform)
            {
                UpdateLocalMatrix(ref node);
            }

            var nodeTransformationsLocal = this.nodeTransformations;

            var parentIndex = node.ParentIndex;

            // Update Enabled
            bool renderingEnabledRecursive = (node.Flags & ModelNodeFlags.EnableRender) == ModelNodeFlags.EnableRender;
            if (parentIndex != -1)
                renderingEnabledRecursive &= nodeTransformationsLocal[parentIndex].RenderingEnabledRecursive;

            node.RenderingEnabledRecursive = renderingEnabledRecursive;

            if (renderingEnabledRecursive && (node.Flags & ModelNodeFlags.OverrideWorldMatrix) != ModelNodeFlags.OverrideWorldMatrix)
            {
                // Compute WorldMatrix
                if (parentIndex != -1)
                {
                    Matrix.Multiply(ref node.LocalMatrix, ref nodeTransformationsLocal[parentIndex].WorldMatrix, out node.WorldMatrix);
                    if (nodeTransformationsLocal[parentIndex].IsScalingNegative)
                        node.IsScalingNegative = !node.IsScalingNegative;
                }
                else
                {
                    node.WorldMatrix = node.LocalMatrix;
                }
            }
        }

        private static void UpdateLocalMatrix(ref ModelNodeTransformation node)
        {
            var scaling = node.Transform.Scale;
            Matrix.Transformation(ref scaling, ref node.Transform.Rotation, ref node.Transform.Position, out node.LocalMatrix);
            node.IsScalingNegative = (scaling.X < 0.0f) ^ (scaling.Y < 0.0f) ^ (scaling.Z < 0.0f);
        }
    }
}
