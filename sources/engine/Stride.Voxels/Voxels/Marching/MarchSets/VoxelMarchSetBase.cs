using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    public class VoxelMarchSetBase
    {
        public IVoxelMarchMethod Marcher { set; get; } = new VoxelMarchConePerMipmap();
        public float Offset { set; get; } = 1.0f;
        public VoxelMarchSetBase()
        {

        }
        public VoxelMarchSetBase(IVoxelMarchMethod marcher)
        {
            Marcher = marcher;
        }

        protected ValueParameterKey<float> OffsetKey;
        public virtual void UpdateMarchingLayout(string compositionName)
        {
            Marcher.UpdateMarchingLayout("Marcher." + compositionName);
        }
        public virtual void ApplyMarchingParameters(ParameterCollection parameters)
        {
            Marcher.ApplyMarchingParameters(parameters);
            parameters.Set(OffsetKey, Offset);
        }
    }
}
