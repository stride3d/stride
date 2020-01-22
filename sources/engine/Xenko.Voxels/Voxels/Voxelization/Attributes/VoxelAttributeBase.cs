using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;
using Xenko.Rendering.Materials;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Voxels
{
    public abstract class VoxelAttributeBase
    {
        [DataMemberIgnore]
        public int BufferOffset { get; set; } = -1;
        [DataMemberIgnore]
        public int LocalSamplerID { get; set; } = -1;

        virtual public bool RequiresColumns()
        {
            return false;
        }
    }
}
