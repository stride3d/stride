// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Shaders;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Rendering.Voxels;
using Stride.Core.Extensions;
using System.Linq;

namespace Stride.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    public class VoxelRenderer : IVoxelRenderer
    {
        [DataMemberIgnore]
        public static readonly PropertyKey<Dictionary<VoxelVolumeComponent, DataVoxelVolume>> CurrentRenderVoxelVolumes = new PropertyKey<Dictionary<VoxelVolumeComponent, DataVoxelVolume>>("VoxelRenderer.CurrentRenderVoxelVolumes", typeof(VoxelRenderer));
        [DataMemberIgnore]
        public static readonly PropertyKey<Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume>> CurrentProcessedVoxelVolumes = new PropertyKey<Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume>>("VoxelRenderer.CurrentProcessedVoxelVolumes", typeof(VoxelRenderer));
        private Dictionary<VoxelVolumeComponent, DataVoxelVolume> renderVoxelVolumes;
        private Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume> renderVoxelVolumeData;


        readonly ProfilingKey PassesVoxelizationProfilingKey = new ProfilingKey("Voxelization: Passes");
        readonly ProfilingKey FragmentVoxelizationProfilingKey = new ProfilingKey("Voxelization: Passes - Fragments");
        readonly ProfilingKey BufferProcessingVoxelizationProfilingKey = new ProfilingKey("Voxelization: Arrangement");
        readonly ProfilingKey MipmappingVoxelizationProfilingKey = new ProfilingKey("Voxelization: Mipmapping");

        public List<RenderStage> VoxelStages { get; set; } = new List<RenderStage>();

        public Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume> GetProcessedVolumes()
        {
            return renderVoxelVolumeData;
        }
        public virtual void Collect(RenderContext Context, Shadows.IShadowMapRenderer ShadowMapRenderer)
        {
            renderVoxelVolumes = Context.VisibilityGroup.Tags.Get(CurrentRenderVoxelVolumes);
            renderVoxelVolumeData = Context.VisibilityGroup.Tags.Get(CurrentProcessedVoxelVolumes);

            if (renderVoxelVolumes == null || renderVoxelVolumes.Count == 0)
                return;

            if (Context.RenderSystem.GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_11_0)
            {
                throw new ArgumentOutOfRangeException("Graphics Profile Level 11 or higher required for Voxelization.");
            }

            //Setup per volume passes and texture allocations
            foreach ( var pair in renderVoxelVolumes )
            {
                var dataVolume = pair.Value;
                var bounds = dataVolume.VolumeSize;

                ProcessedVoxelVolume processedVolume;
                if (!renderVoxelVolumeData.TryGetValue(pair.Key, out processedVolume))
                {
                    processedVolume = new ProcessedVoxelVolume();
                    renderVoxelVolumeData.Add(pair.Key, processedVolume);
                }


                //Setup matrix
                Vector3 matScale = dataVolume.VolumeSize;
                Vector3 matTrans = dataVolume.VolumeTranslation;
                
                Matrix corMatrix = Matrix.Scaling(matScale) * Matrix.Translation(matTrans);
                VoxelStorageContext storageContext = new VoxelStorageContext
                {
                    device = Context.GraphicsDevice,
                    Extents = bounds,
                    VoxelSize = dataVolume.AproxVoxelSize,
                    Matrix = corMatrix
                };

                if (dataVolume.VoxelGridSnapping)
                {
                    matTrans /= storageContext.RealVoxelSize();
                    matTrans.X = (float)Math.Floor(matTrans.X);
                    matTrans.Y = (float)Math.Floor(matTrans.Y);
                    matTrans.Z = (float)Math.Floor(matTrans.Z);
                    matTrans *= storageContext.RealVoxelSize();

                    corMatrix = Matrix.Scaling(matScale) * Matrix.Translation(matTrans);
                    storageContext.Matrix = corMatrix;
                }
                storageContext.Translation = matTrans;
                storageContext.VoxelSpaceTranslation = matTrans / storageContext.RealVoxelSize();

                //Update storage
                dataVolume.Storage.UpdateFromContext(storageContext);

                //Transfer voxelization info
                processedVolume.VisualizeVoxels = dataVolume.VisualizeVoxels;
                processedVolume.Storage = dataVolume.Storage;
                processedVolume.StorageContext = storageContext;
                processedVolume.VoxelizationMethod = dataVolume.VoxelizationMethod;
                processedVolume.VoxelVisualization = dataVolume.VoxelVisualization;
                processedVolume.VisualizationAttribute = dataVolume.VisualizationAttribute;
                processedVolume.Voxelize = dataVolume.Voxelize;
                processedVolume.OutputAttributes = dataVolume.Attributes;

                processedVolume.Attributes.Clear();
                processedVolume.passList.Clear();
                processedVolume.groupedPasses.Clear();

                processedVolume.passList.defaultVoxelizationMethod = dataVolume.VoxelizationMethod;


                //Create final list of attributes (including temporary ones)
                foreach (var attr in dataVolume.Attributes)
                {
                    attr.CollectAttributes(processedVolume.Attributes, VoxelizationStage.Initial, true);
                }

                //Allocate textures and space in the temporary buffer
                foreach (var attr in processedVolume.Attributes)
                {
                    attr.Attribute.PrepareLocalStorage(storageContext, dataVolume.Storage);
                    if (attr.Output)
                    {
                        attr.Attribute.PrepareOutputStorage(storageContext, dataVolume.Storage);
                    }
                    else
                    {
                        attr.Attribute.ClearOutputStorage();
                    }
                }
                dataVolume.Storage.UpdateTempStorage(storageContext);

                //Create list of voxelization passes that need to be done
                dataVolume.Storage.CollectVoxelizationPasses(processedVolume, storageContext);

                //Group voxelization passes where the RenderStage can be shared
                //TODO: Group identical attributes
                for (int i = 0; i < processedVolume.passList.passes.Count; i++)
                {
                    bool added = false;
                    var passA = processedVolume.passList.passes[i];
                    for (int group = 0; group < processedVolume.groupedPasses.Count; group++)
                    {
                        var passB = processedVolume.groupedPasses[group][0];
                        if (
                           passB.storer.CanShareRenderStage(passA.storer)
                        && passB.method.CanShareRenderStage(passA.method)
                        && passB.AttributesDirect.SequenceEqual(passA.AttributesDirect)
                        && passB.AttributesIndirect.SequenceEqual(passA.AttributesIndirect)
                        && passB.AttributesTemp.SequenceEqual(passA.AttributesTemp)
                        )
                        {
                            processedVolume.groupedPasses[group].Add(passA);
                            added = true;
                            break;
                        }
                    }
                    if (!added)
                    {
                        List<VoxelizationPass> newGroup = new List<VoxelizationPass>
                        {
                            passA
                        };
                        processedVolume.groupedPasses.Add(newGroup);
                    }
                }

                if (VoxelStages.Count < processedVolume.groupedPasses.Count)
                {
                    throw new ArgumentOutOfRangeException(processedVolume.groupedPasses.Count.ToString() + " Render Stages required for voxelization, only " + VoxelStages.Count.ToString() + " provided.");
                }

                //Finish preparing the passes, collecting views and setting up shader sources and shadows
                for (int group = 0; group < processedVolume.groupedPasses.Count; group++)
                {
                    foreach(var pass in processedVolume.groupedPasses[group])
                    {
                        pass.renderStage = VoxelStages[group];
                        pass.source = pass.storer.GetVoxelizationShader(pass, processedVolume);
                        pass.view.RenderStages.Add(pass.renderStage);

                        Context.RenderSystem.Views.Add(pass.view);
                        Context.VisibilityGroup.TryCollect(pass.view);

                        if (pass.requireShadows)
                        {
                            ShadowMapRenderer?.RenderViewsWithShadows.Add(pass.view);
                        }
                    }
                }
            }
        }
        public virtual void Draw(RenderDrawContext drawContext, Shadows.IShadowMapRenderer ShadowMapRenderer)
        {
            if (renderVoxelVolumes == null || renderVoxelVolumes.Count == 0)
                return;

            if (drawContext.GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_11_0)
                return;

            var context = drawContext;

            using (drawContext.PushRenderTargetsAndRestore())
            {
                // Draw all shadow views generated for the current view
                foreach (var processedVolumeKeyValue in renderVoxelVolumeData)
                {
                    var processedVolume = processedVolumeKeyValue.Value;
                    if (!processedVolume.Voxelize) continue;

                    VoxelStorageContext storageContext = processedVolume.StorageContext;

                    using (drawContext.QueryManager.BeginProfile(Color.Black, PassesVoxelizationProfilingKey))
                    {
                        foreach (VoxelizationPass pass in processedVolume.passList.passes)
                        {
                            RenderView voxelizeRenderView = pass.view;

                            if (pass.requireShadows)
                            {
                                //Render Shadow Maps
                                RenderView oldView = drawContext.RenderContext.RenderView;

                                drawContext.RenderContext.RenderView = voxelizeRenderView;
                                    ShadowMapRenderer.Draw(drawContext);
                                drawContext.RenderContext.RenderView = oldView;
                            }

                            //Render/Collect voxel fragments
                            using (drawContext.QueryManager.BeginProfile(Color.Black, FragmentVoxelizationProfilingKey))
                            {
                                using (drawContext.PushRenderTargetsAndRestore())
                                {
                                    pass.method.Render(storageContext, context, pass.view);
                                }
                            }
                        }
                        foreach (VoxelizationPass pass in processedVolume.passList.passes)
                        {
                            pass.method.Reset();
                        }
                    }

                    //Fill and write to voxel volume
                    using (drawContext.QueryManager.BeginProfile(Color.Black, BufferProcessingVoxelizationProfilingKey))
                    {
                        processedVolume.Storage.PostProcess(storageContext, context, processedVolume);
                    }

                    //Mipmap
                    using (drawContext.QueryManager.BeginProfile(Color.Black, MipmappingVoxelizationProfilingKey))
                    {
                        foreach (var attr in processedVolume.Attributes)
                        {
                            if (attr.Output)
                            {
                                attr.Attribute.PostProcess(context);
                            }
                        }
                    }
                }
            }
        }
    }
}
