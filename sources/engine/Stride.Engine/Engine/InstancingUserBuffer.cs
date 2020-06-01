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
    [DataContract("InstancingUserBuffer")]
    [Display("UserBuffer")]
    public class InstancingUserBuffer : InstancingManyBase
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
