using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Mathematics;
using Xenko.Rendering;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public class VoxelizationPass
    {
        public RenderView view;
        public IVoxelStorer storer;
        public IVoxelizationMethod method;
        //Stage1
        public List<VoxelAttribute> AttributesTemp = new List<VoxelAttribute>();
        public List<VoxelAttribute> AttributesDirect = new List<VoxelAttribute>();
        public List<VoxelAttribute> AttributesIndirect = new List<VoxelAttribute>();

        public ShaderSource source;

        public bool requireShadows = false;
        public RenderStage renderStage = null;


        public void Add(VoxelAttribute attr, VoxelizationStage stage, bool output, bool shadows)
        {
            if (stage == VoxelizationStage.Initial)
            {
                if (output)
                {
                    AttributesDirect.Add(attr);
                }
                else
                {
                    AttributesTemp.Add(attr);
                }
            }
            else if (stage == VoxelizationStage.Post)
            {
                AttributesIndirect.Add(attr);
            }

            requireShadows |= shadows;
        }
    }
}
