// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Storage;
using Xenko.Core.Threading;
using Xenko.Graphics;
using Xenko.Rendering.Shadows;
using Xenko.Rendering.Voxels;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    /// <summary>
    /// A render feature that computes and uploads info for voxelization
    /// </summary>
    public class VoxelRenderFeature : SubRenderFeature
    {
        [DataMemberIgnore]
        public static readonly PropertyKey<Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume>> CurrentProcessedVoxelVolumes = new PropertyKey<Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume>>("VoxelRenderFeature.CurrentProcessedVoxelVolumes", typeof(VoxelRenderFeature));

        private Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume> renderVoxelVolumeData;
        
        private LogicalGroupReference VoxelizerStorerCasterKey;
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        protected override void InitializeCore()
        {
            base.InitializeCore();
            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
            VoxelizerStorerCasterKey = ((RootEffectRenderFeature)RootRenderFeature).CreateViewLogicalGroup("VoxelizerStorer");
        }

        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            renderVoxelVolumeData = Context.VisibilityGroup.Tags.Get(CurrentProcessedVoxelVolumes);
            if (renderVoxelVolumeData == null) return;


            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;


            var rootEffectRenderFeature = ((RootEffectRenderFeature)RootRenderFeature);

            if (rootEffectRenderFeature == null) return;

            foreach (var processedVolumeKeyValue in renderVoxelVolumeData)
            {
                var processedVolume = processedVolumeKeyValue.Value;

                foreach (VoxelizationPass pass in processedVolume.passList.passes)
                {
                    pass.storer.UpdateVoxelizationLayout("Storage");
                    for (int i = 0; i < pass.AttributesIndirect.Count; i++)
                    {
                        var attr = pass.AttributesIndirect[i];
                        attr.UpdateVoxelizationLayout($"AttributesIndirect[{i}]");
                    }
                }
                foreach (var group in processedVolume.groupedPasses)
                {
                    //Each pass in a group should have identical shaders
                    var pass = group[0];

                    Dispatcher.ForEach(RootRenderFeature.RenderObjects, renderObject =>
                    {
                        var renderMesh = (RenderMesh)renderObject;

                        var staticObjectNode = renderMesh.StaticObjectNode;


                        var effectSlot = rootEffectRenderFeature.GetEffectPermutationSlot(RenderSystem.RenderStages[pass.view.RenderStages[0].Index]);
                        {

                            var staticEffectObjectNode = staticObjectNode * effectSlotCount + effectSlot.Index;
                            var renderEffect = renderEffects[staticEffectObjectNode];

                            // Skip effects not used during this frame
                            if (renderEffect != null)
                            {
                                renderEffect.EffectValidator.ValidateParameter(VoxelizeToFragmentsKeys.Storage, pass.source);
                                renderEffect.EffectValidator.ValidateParameter(VoxelizeToFragmentsKeys.RequireGeometryShader, pass.storer.RequireGeometryShader() || pass.method.RequireGeometryShader());
                                renderEffect.EffectValidator.ValidateParameter(VoxelizeToFragmentsKeys.GeometryShaderMaxVertexCount, pass.storer.GeometryShaderOutputCount() * pass.method.GeometryShaderOutputCount());
                            }
                        }
                    });
                }
            }
        }
        public override void Prepare(RenderDrawContext context)
        {
            renderVoxelVolumeData = Context.VisibilityGroup.Tags.Get(CurrentProcessedVoxelVolumes);
            if (renderVoxelVolumeData == null) return;

            foreach (var processedVolumeKeyValue in renderVoxelVolumeData)
            {
                var processedVolume = processedVolumeKeyValue.Value;
                foreach (VoxelizationPass pass in processedVolume.passList.passes)
                {
                    var viewFeature = pass.view.Features[RootRenderFeature.Index];


                    // Find a PerView layout from an effect in normal state
                    ViewResourceGroupLayout firstViewLayout = null;
                    foreach (var viewLayout in viewFeature.Layouts)
                    {
                        // Only process view layouts in normal state
                        if (viewLayout.State != RenderEffectState.Normal)
                            continue;

                        var viewLighting = viewLayout.GetLogicalGroup(VoxelizerStorerCasterKey);
                        if (viewLighting.Hash != ObjectId.Empty)
                        {
                            firstViewLayout = viewLayout;
                            break;
                        }
                    }

                    // Nothing found for this view (no effects in normal state)
                    if (firstViewLayout == null)
                        continue;

                    var viewParameters = new ParameterCollection();

                    var firstViewLighting = firstViewLayout.GetLogicalGroup(VoxelizerStorerCasterKey);

                    // Prepare layout (should be similar for all PerView)
                    {

                        // Generate layout
                        var viewParameterLayout = new ParameterCollectionLayout();
                        viewParameterLayout.ProcessLogicalGroup(firstViewLayout, ref firstViewLighting);

                        viewParameters.UpdateLayout(viewParameterLayout);
                    }



                    ParameterCollection VSViewParameters = viewParameters;

                    pass.storer.ApplyVoxelizationParameters(VSViewParameters);
                    foreach (var attr in processedVolume.Attributes)
                    {
                        attr.Attribute.ApplyVoxelizationParameters(VSViewParameters);
                    }

                    foreach (var viewLayout in viewFeature.Layouts)
                    {
                        if (viewLayout.State != RenderEffectState.Normal)
                            continue;

                        var voxelizerStorer = viewLayout.GetLogicalGroup(VoxelizerStorerCasterKey);
                        if (voxelizerStorer.Hash == ObjectId.Empty)
                            continue;

                        if (voxelizerStorer.Hash != firstViewLighting.Hash)
                            throw new InvalidOperationException("PerView VoxelizerStorer layout differs between different RenderObject in the same RenderView");


                        var resourceGroup = viewLayout.Entries[pass.view.Index].Resources;
                        resourceGroup.UpdateLogicalGroup(ref voxelizerStorer, VSViewParameters);
                    }
                }
            }
        }
    }
}
