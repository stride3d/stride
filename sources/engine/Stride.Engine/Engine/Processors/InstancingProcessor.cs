// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Engine.Processors
{
    public class InstancingProcessor : EntityProcessor<InstancingComponent, InstancingProcessor.InstancingData>, IEntityComponentRenderProcessor
    {
        private readonly Dictionary<RenderModel, RenderInstancing> modelInstancingMap = new Dictionary<RenderModel, RenderInstancing>();
        // The model render processor of this processor's visibility group
        private ModelRenderProcessor modelRenderProcessor;
        // Whether a component waits for its render model, which its group's model render processor has not made yet
        private bool hasUnmappedComponents;

        public class InstancingData
        {
            public TransformComponent TransformComponent;
            public ModelComponent ModelComponent;
            public RenderModel RenderModel;
            public RenderInstancing RenderInstancing = new RenderInstancing();
        }

        public InstancingProcessor()
            : base(typeof(TransformComponent), typeof(ModelComponent)) // Requires TransformComponent and ModelComponent
        {
            // After the ModelRenderProcessor of every group (order 0), whose render meshes this processor widens by the instances
            Order = 100;
        }

        public VisibilityGroup VisibilityGroup { get; set; }

        public override void Draw(RenderContext context)
        {
            if (hasUnmappedComponents)
                MapUnmappedComponents();

            // Process the components
            Dispatcher.ForEach(ComponentDatas, item =>
            {
                UpdateInstancing(item.Key, item.Value);
                TransferData(context, item.Key, item.Value.RenderInstancing);
            });
        }

        private void MapUnmappedComponents()
        {
            hasUnmappedComponents = false;
            foreach (var data in ComponentDatas.Values)
            {
                if (data.RenderModel == null && !TryMapRenderModel(data))
                    hasUnmappedComponents = true;
            }
        }

        /// <summary>
        /// Maps the render model of <paramref name="data"/>'s model component, as the model render processor of this
        /// processor's visibility group made it, to the component's instancing.
        /// </summary>
        /// <returns><c>false</c> when that render model does not exist yet.</returns>
        private bool TryMapRenderModel(InstancingData data)
        {
            if (data.ModelComponent == null)
                return true;

            var processor = GetModelRenderProcessor();
            if (processor == null || !processor.RenderModels.TryGetValue(data.ModelComponent, out var renderModel))
                return false;

            modelInstancingMap[renderModel] = data.RenderInstancing;
            data.RenderModel = renderModel;
            return true;
        }

        private ModelRenderProcessor GetModelRenderProcessor()
        {
            if (modelRenderProcessor == null)
            {
                foreach (var processor in EntityManager.Processors)
                {
                    if (processor is ModelRenderProcessor candidate && candidate.VisibilityGroup == VisibilityGroup)
                    {
                        modelRenderProcessor = candidate;
                        break;
                    }
                }
            }
            return modelRenderProcessor;
        }

        private static void TransferData(RenderContext context, InstancingComponent instancingComponent, RenderInstancing renderInstancing)
        {
            var instancing = instancingComponent.Type;
            // Get final instance count
            var instanceCount = instancingComponent.Enabled ? instancing.InstanceCount : 0;
            renderInstancing.InstanceCount = instanceCount;
            renderInstancing.ModelTransformUsage = (int)instancing.ModelTransformUsage;

            if (renderInstancing.InstanceCount > 0)
            {
                if (instancing is InstancingUserArray instancingUserArray)
                {
                    renderInstancing.BuffersManagedByUser = false;
                    renderInstancing.WorldMatrices = instancingUserArray.WorldMatrices;
                    renderInstancing.WorldInverseMatrices = instancingUserArray.WorldInverseMatrices;

                    if (renderInstancing.InstanceWorldBuffer == null || renderInstancing.InstanceWorldBuffer.ElementCount < instanceCount)
                    {
                        renderInstancing.InstanceWorldBuffer?.Dispose();
                        renderInstancing.InstanceWorldInverseBuffer?.Dispose();

                        renderInstancing.InstanceWorldBuffer = CreateMatrixBuffer(context.GraphicsDevice, instanceCount);
                        renderInstancing.InstanceWorldInverseBuffer = CreateMatrixBuffer(context.GraphicsDevice, instanceCount);
                    }

                }
                else if (instancing is InstancingUserBuffer instancingUserBuffer)
                {
                    renderInstancing.BuffersManagedByUser = true;
                    renderInstancing.InstanceWorldBuffer = instancingUserBuffer.InstanceWorldBuffer;
                    renderInstancing.InstanceWorldInverseBuffer = instancingUserBuffer.InstanceWorldInverseBuffer;
                }
            }
        }

        private static Buffer<Matrix> CreateMatrixBuffer(GraphicsDevice graphicsDevice, int elementCount)
        {
            return Buffer.New<Matrix>(graphicsDevice, elementCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
        }

        private void UpdateInstancing(InstancingComponent instancingComponent, InstancingData instancingData)
        {
            if (instancingComponent.Enabled && instancingComponent.Type != null)
            {
                var instancing = instancingComponent.Type;

                // Calculate inverse world and bounding box
                instancing.Update();

                var modelComponent = instancingData.ModelComponent;
                var renderModel = instancingData.RenderModel;
                // A model set to null leaves the render model's meshes in place until a model is set again
                if (modelComponent != null && modelComponent.Enabled && renderModel?.Model != null && renderModel.Meshes != null && instancing.InstanceCount > 0)
                {
                    // Bounding box: the render meshes of this group's render model take the mesh box widened by the instances. The
                    // model component's own box (MeshInfo.BoundingBox) stays as its transform computed it, so a scene instance with
                    // several visibility groups, an instancing processor each, widens a mesh once per group and not once per processor.
                    for (int sourceMeshIndex = 0; sourceMeshIndex < renderModel.Materials.Length; sourceMeshIndex++)
                    {
                        var mesh = renderModel.Model.Meshes[sourceMeshIndex];
                        var meshInfo = modelComponent.MeshInfos[sourceMeshIndex];

                        // This must reflect the transformations in the shaders
                        // This is currently not entirely correct, it ignores cases with extreme scalings
                        var boundingBox = instancing.ModelTransformUsage switch
                        {
                            ModelTransformUsage.Ignore => BoundingBoxIgnoreWorld(instancingData, instancing, meshInfo, mesh),
                            ModelTransformUsage.PreMultiply => BoundingBoxPreMultiplyWorld(instancing, meshInfo),
                            ModelTransformUsage.PostMultiply => BoundingBoxPostMultiplyWorld(instancingData, instancing, meshInfo),
                            _ => meshInfo.BoundingBox,
                        };

                        // Note: indices in RenderModel.Meshes and Model.Meshes are different (due to multipass materials)
                        var renderBoundingBox = new BoundingBoxExt(boundingBox);
                        var meshIndex = renderModel.Materials[sourceMeshIndex].MeshStartIndex;
                        for (int pass = 0; pass < renderModel.Materials[sourceMeshIndex].MeshCount; ++pass, ++meshIndex)
                            renderModel.Meshes[meshIndex].BoundingBox = renderBoundingBox;
                    }
                }
            }
        }

        private static BoundingBox BoundingBoxIgnoreWorld(InstancingData instancingData, IInstancing instancing, ModelComponent.MeshInfo meshInfo, Mesh mesh)
        {
            // We need to remove the world transformation component
            if (instancingData.ModelComponent.Skeleton != null)
            {
                var ibb = instancing.BoundingBox;
                var mbb = new BoundingBoxExt(meshInfo.BoundingBox);

                Matrix.Invert(ref instancingData.ModelComponent.Skeleton.NodeTransformations[0].LocalMatrix, out var invWorld);
                mbb.Transform(invWorld);

                var center = ibb.Center;
                var extend = ibb.Extent + mbb.Extent;
                return new BoundingBox(center - extend, center + extend);
            }
            else // Just take the original mesh one
            {
                var ibb = instancing.BoundingBox;

                var center = ibb.Center;
                var extend = ibb.Extent + mesh.BoundingBox.Extent;
                return new BoundingBox(center - extend, center + extend);
            }
        }

        private static BoundingBox BoundingBoxPreMultiplyWorld(IInstancing instancing, ModelComponent.MeshInfo meshInfo)
        {
            var ibb = instancing.BoundingBox;

            var center = meshInfo.BoundingBox.Center;
            var extend = ibb.Extent + meshInfo.BoundingBox.Extent;
            return new BoundingBox(center - extend, center + extend);
        }

        private static BoundingBox BoundingBoxPostMultiplyWorld(InstancingData instancingData, IInstancing instancing, ModelComponent.MeshInfo meshInfo)
        {
            var ibb = new BoundingBoxExt(instancing.BoundingBox);
            ibb.Transform(instancingData.TransformComponent.WorldMatrix);
            var center = meshInfo.BoundingBox.Center + ibb.Center - instancingData.TransformComponent.WorldMatrix.TranslationVector; // World translation was applied twice now
            var extend = meshInfo.BoundingBox.Extent + ibb.Extent;
            return new BoundingBox(center - extend, center + extend);
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData data)
        {
            data.TransformComponent = component.Entity.Transform;
            data.ModelComponent = component.Entity.Get<ModelComponent>();

            if (!TryMapRenderModel(data))
                hasUnmappedComponents = true;
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData data)
        {
            if (data.RenderModel != null)
            {
                modelInstancingMap.Remove(data.RenderModel);
            }

            if (component.Type is InstancingUserArray)
            {
                data.RenderInstancing?.InstanceWorldBuffer?.Dispose();
                data.RenderInstancing?.InstanceWorldInverseBuffer?.Dispose();
            }
        }

        // Instancing data per InstancingComponent
        protected override InstancingData GenerateComponentData([NotNull] Entity entity, [NotNull] InstancingComponent component)
        {
            return new InstancingData();
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData associatedData)
        {
            // A model or transform component replaced in place (the entity's collection set at its index) makes the data stale:
            // the render model it maps belongs to the component that is gone
            return associatedData.TransformComponent == entity.Transform
                && associatedData.ModelComponent == entity.Get<ModelComponent>();
        }

        protected internal override void OnSystemAdd()
        {
            base.OnSystemAdd();
            VisibilityGroup.Tags.Set(InstancingRenderFeature.ModelToInstancingMap, modelInstancingMap);

            // A scene instance makes the model render processor of each visibility group itself, possibly after this one
            if (GetModelRenderProcessor() == null && EntityManager is not SceneInstance)
            {
                modelRenderProcessor = new ModelRenderProcessor { VisibilityGroup = VisibilityGroup };
                EntityManager.Processors.Add(modelRenderProcessor);
            }
        }

        protected internal override void OnSystemRemove()
        {
            VisibilityGroup.Tags.Remove(InstancingRenderFeature.ModelToInstancingMap);
            modelRenderProcessor = null;
            base.OnSystemRemove();
        }
    }
}
