using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine
{
    [DataContract("InstancingUserBuffer")]
    [Display("UserBuffer")]
    public class InstancingUserBuffer : InstancingBase
    {
        [DataMemberIgnore]
        public Buffer<Matrix> InstanceWorldBuffer;

        [DataMemberIgnore]
        public Buffer<Matrix> InstanceWorldInverseBuffer;

        public override void Update()
        {
            // No op, Assumes the user has done everything.
        }
    }
}
