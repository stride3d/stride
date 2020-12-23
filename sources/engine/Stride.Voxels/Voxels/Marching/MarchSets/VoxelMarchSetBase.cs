// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
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
