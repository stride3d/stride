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
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Emission+Opacity")]
    public class VoxelAttributeEmissionOpacity : VoxelAttribute
    {
        public enum LightFalloffs
        {
            [Display("Sharp")] Sharp,
            [Display("Physically Based")] PhysicallyBased,
            [Display("Physically Based + Shadowing Heuristic")] Heuristic,
        }

        [NotNull]
        public IVoxelLayout VoxelLayout { get; set; } = new VoxelLayoutIsotropic();

        public List<VoxelModifierEmissionOpacity> Modifiers { get; set; } = new List<VoxelModifierEmissionOpacity>();

        public LightFalloffs LightFalloff { get; set; } = LightFalloffs.Heuristic;


        public override void PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            BufferOffset = VoxelLayout.PrepareLocalStorage(context, storage);
        }
        public override void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            VoxelLayout.PrepareOutputStorage(context, storage);
        }
        public override void ClearOutputStorage()
        {
            VoxelLayout.ClearOutputStorage();
        }




        public override void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelizationStage stage, bool output)
        {
            passList.defaultVoxelizationMethod.CollectVoxelizationPasses(passList, storer, view, resolution, this, stage, output, true);
        }
        public override void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            foreach (VoxelModifierEmissionOpacity modifier in Modifiers)
            {
                if (!modifier.Enabled) continue;

                modifier.CollectAttributes(attributes, VoxelizationStage.Post, false);
            }
            attributes.Add(new AttributeStream(this, VoxelizationStage.Post, output));
        }

        public override bool RequiresColumns()
        {
            foreach (VoxelModifierEmissionOpacity modifier in Modifiers)
            {
                if (!modifier.Enabled) continue;

                if (modifier.RequiresColumns())
                    return true;
            }
            return false;
        }
        public override void PostProcess(RenderDrawContext drawContext)
        {
            VoxelLayout.PostProcess(drawContext, LightFalloff);
        }




        ShaderClassSource source = new ShaderClassSource("VoxelAttributeEmissionOpacityShader");

        public override ShaderSource GetVoxelizationShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(source);
            mixin.AddComposition("layout", VoxelLayout.GetVoxelizationShader(Modifiers));
            return mixin;
        }
        public override void UpdateVoxelizationLayout(string compositionName)
        {
            int i = 0;
            foreach (VoxelModifierEmissionOpacity modifier in Modifiers)
            {
                if (!modifier.Enabled) continue;

                modifier.UpdateVoxelizationLayout($"Modifiers[{i}].layout.{compositionName}");
                i++;
            }
            VoxelLayout.UpdateVoxelizationLayout("layout." + compositionName, Modifiers);
        }
        public override void ApplyVoxelizationParameters(ParameterCollection parameters)
        {
            foreach (VoxelModifierEmissionOpacity modifier in Modifiers)
            {
                if (!modifier.Enabled) continue;

                modifier.ApplyVoxelizationParameters(parameters);
            }
            VoxelLayout.ApplyVoxelizationParameters(parameters, Modifiers);
        }




        public override ShaderSource GetSamplingShader()
        {
            return VoxelLayout.GetSamplingShader();
        }
        public override void UpdateSamplingLayout(string compositionName)
        {
            VoxelLayout.UpdateSamplingLayout(compositionName);
        }
        public override void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            VoxelLayout.ApplySamplingParameters(viewContext, parameters);
        }
    }
}
