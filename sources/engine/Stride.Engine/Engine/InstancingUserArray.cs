using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Graphics;

namespace Stride.Engine
{
    [DataContract("InstancingMany")]
    [Display("Many")]
    public class InstancingUserArray : InstancingManyBase
    {
        /// <summary>
        /// The instance transformation matrices.
        /// </summary>
        [DataMemberIgnore]
        public Matrix[] WorldMatrices = new Matrix[0];

        /// <summary>
        /// The inverse instance transformation matrices, updated automatically by the <see cref="InstancingProcessor"/>.
        /// </summary>
        [DataMemberIgnore]
        public Matrix[] WorldInverseMatrices = new Matrix[0];

        /// <summary>
        /// A flag indicating whether the inverse matrices and bounding box should be calculated this frame.
        /// </summary>
        bool matricesUpdated;

        /// <summary>
        /// Updates the world matrices.
        /// </summary>
        /// <param name="matrices">The matrices.</param>
        /// <param name="instanceCount">The instance count. When set to -1 the lenght if the matrices array is used</param>
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


        public override void Update()
        {
            base.Update();

            if (matricesUpdated)
            {
                if (WorldMatrices != null)
                {
                    // Make sure inverse matrices are big enough
                    if (WorldInverseMatrices.Length < InstanceCount)
                    {
                        WorldInverseMatrices = new Matrix[InstanceCount];
                    }

                    // Invert matrices and update bounding box
                    var bb = BoundingBox.Empty;
                    for (int i = 0; i < InstanceCount; i++)
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
