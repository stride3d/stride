using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine
{
    [DataContract("InstancingUserBuffer")]
    [Display("UserBuffer")]
    public class InstancingUserBuffer : IInstancing
    {
        [DataMember(10)]
        [Display("Model Transformation Usage")]
        public virtual ModelTransformUsage ModelTransformUsage { get; set; }

        /// <summary>
        /// The instance count
        /// </summary>
        [DataMemberIgnore]
        public virtual int InstanceCount { get; set; }

        /// <summary>
        /// The bounding box of the world matrices, updated automatically by the <see cref="InstancingProcessor"/>.
        /// </summary>
        [DataMemberIgnore]
        public virtual BoundingBox BoundingBox { get; set; } = BoundingBox.Empty;

        [DataMemberIgnore]
        public Buffer<Matrix> InstanceWorldBuffer;

        [DataMemberIgnore]
        public Buffer<Matrix> InstanceWorldInverseBuffer;

        public void Update()
        {
            // No op, Assumes the user has done everything.
        }
    }
}
