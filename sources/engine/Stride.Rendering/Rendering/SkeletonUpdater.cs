// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Rendering
{
    /// <summary>
    /// Performs hierarchical updates for a given <see cref="Model"/>.
    /// </summary>
    [DataContract] // Here for update engine; TODO: better separation and different attribute?
    public class SkeletonUpdater
    {
        private int matrixCounter;

        [DataMember]
        public ModelNodeDefinition[] Nodes { get; private set; }

        [DataMember]
        public ModelNodeTransformation[] NodeTransformations { get; private set; }

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

            if (this.Nodes == newNodes && this.Nodes != null)
            {
                return;
            }

            this.Nodes = newNodes ?? GetDefaultNodeDefinitions();

            if (NodeTransformations == null || NodeTransformations.Length < this.Nodes.Length)
                NodeTransformations = new ModelNodeTransformation[this.Nodes.Length];

            for (int index = 0; index < Nodes.Length; index++)
            {
                NodeTransformations[index].ParentIndex = Nodes[index].ParentIndex;
                NodeTransformations[index].Transform = Nodes[index].Transform;
                NodeTransformations[index].Flags = Nodes[index].Flags;
                NodeTransformations[index].RenderingEnabledRecursive = true;
                UpdateLocalMatrix(ref NodeTransformations[index]);
            }

            NodeTransformations[0].Flags &= ~ModelNodeFlags.EnableTransform;
        }

        /// <summary>
        /// Resets initial values.
        /// </summary>
        public void ResetInitialValues()
        {
            var nodesLocal = Nodes;
            for (int index = 0; index < nodesLocal.Length; index++)
            {
                NodeTransformations[index].Transform = nodesLocal[index].Transform;
            }
        }

        /// <summary>
        /// For each node, updates the world matrices from local matrices.
        /// </summary>
        public void UpdateMatrices()
        {
            // Compute transformations
            var nodesLength = Nodes.Length;
            for (int index = 0; index < nodesLength; index++)
            {
                UpdateNode(ref NodeTransformations[index]);
            }
            matrixCounter++;
        }

        public void GetWorldMatrix(int index, out Matrix matrix)
        {
            matrix = NodeTransformations[index].WorldMatrix;
        }

        public void GetLocalMatrix(int index, out Matrix matrix)
        {
            matrix = NodeTransformations[index].LocalMatrix;
        }

        private void UpdateNode(ref ModelNodeTransformation node)
        {
            // Compute LocalMatrix
            if ((node.Flags & ModelNodeFlags.EnableTransform) == ModelNodeFlags.EnableTransform)
            {
                UpdateLocalMatrix(ref node);
            }

            var nodeTransformationsLocal = this.NodeTransformations;

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
