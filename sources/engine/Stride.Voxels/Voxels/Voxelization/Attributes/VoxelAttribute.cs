// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;
using Stride.Rendering.Materials;
using Stride.Core.Mathematics;

namespace Stride.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    public abstract class VoxelAttribute
    {
        public abstract void PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage);
        public abstract void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage);
        public abstract void ClearOutputStorage();

        public abstract void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelizationStage stage, bool output);
        public abstract void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output);

        public virtual  bool RequiresColumns() => false;
        public abstract void PostProcess(RenderDrawContext drawContext);

        //Writing
        public abstract ShaderSource GetVoxelizationShader();
        public abstract void UpdateVoxelizationLayout(string compositionName);
        public abstract void ApplyVoxelizationParameters(ParameterCollection parameters);

        [DataMemberIgnore]
        public virtual int BufferOffset { get; set; } = -1;

        //Sampling
        public abstract ShaderSource GetSamplingShader();
        public abstract void UpdateSamplingLayout(string compositionName);
        public abstract void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters);

        [DataMemberIgnore]
        public virtual int LocalSamplerID { get; set; } = -1;
    }
}
