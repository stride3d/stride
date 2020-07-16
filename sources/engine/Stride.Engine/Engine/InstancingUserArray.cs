// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Processors;

namespace Stride.Engine
{
    [DataContract("InstancingUserArray")]
    [Display("UserArray")]
    public class InstancingUserArray : IInstancing
    {
        [DataMember(10)]
        [Display("Model Transformation Usage")]
        public virtual ModelTransformUsage ModelTransformUsage { get; set; }

        /// <summary>
        /// The instance count
        /// </summary>
        [DataMemberIgnore]
        public virtual int InstanceCount { get; private set; }

        /// <summary>
        /// The bounding box of the world matrices, updated automatically by the <see cref="InstancingProcessor"/>.
        /// </summary>
        [DataMemberIgnore]
        public virtual BoundingBox BoundingBox { get; private set; } = BoundingBox.Empty;

        /// <summary>
        /// The instance transformation matrices.
        /// </summary>
        [DataMemberIgnore]
        public Matrix[] WorldMatrices = Array.Empty<Matrix>();

        /// <summary>
        /// The inverse instance transformation matrices, updated automatically by the <see cref="InstancingProcessor"/>.
        /// </summary>
        [DataMemberIgnore]
        public Matrix[] WorldInverseMatrices = Array.Empty<Matrix>();

        /// <summary>
        /// A flag indicating whether the inverse matrices and bounding box should be calculated this frame.
        /// </summary>
        bool matricesUpdated;

        /// <summary>
        /// Updates the world matrices.
        /// </summary>
        /// <param name="matrices">The matrices.</param>
        /// <param name="instanceCount">The instance count. When set to -1 the length if the matrices array is used</param>
        public void UpdateWorldMatrices(Matrix[] matrices, int instanceCount = -1)
        {
            WorldMatrices = matrices;

            if (WorldMatrices != null)
            {
                InstanceCount = instanceCount < 0 ? WorldMatrices.Length : Math.Min(WorldMatrices.Length, instanceCount);
            }
            else
            {
                InstanceCount = 0;
            }

            matricesUpdated = true;
        }


        public virtual void Update()
        {
            if (matricesUpdated)
            {
                if (WorldMatrices != null)
                {
                    // Local copy of virtual instance count property
                    var instanceCount = InstanceCount;

                    // Make sure inverse matrices array is big enough
                    if (WorldInverseMatrices.Length < instanceCount)
                    {
                        WorldInverseMatrices = new Matrix[instanceCount];
                    }

                    // Invert matrices and update bounding box
                    var bb = BoundingBox.Empty;
                    for (int i = 0; i < instanceCount; i++)
                    {
                        Matrix.Invert(ref WorldMatrices[i], out WorldInverseMatrices[i]);
                        var pos = WorldMatrices[i].TranslationVector;
                        BoundingBox.Merge(ref bb, ref pos, out bb);
                    }
                    BoundingBox = bb;
                }
                else
                {
                    BoundingBox = BoundingBox.Empty;
                }
            }

            matricesUpdated = false;
        }
    }
}
