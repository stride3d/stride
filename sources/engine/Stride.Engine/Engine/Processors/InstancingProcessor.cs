using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine.Processors
{
    public class InstancingProcessor : EntityProcessor<InstancingComponent, InstancingProcessor.InstancingData>
    {
        public class InstancingData
        {
            public TransformComponent TransformComponent;
            public ModelComponent ModelComponent;
        }

        public InstancingProcessor() 
            : base (typeof(TransformComponent), typeof(ModelComponent)) // Requires TransformComponent and ModelComponent
        {
            // After TransformProcessor but before ModelRenderProcessor
            Order = -100;
        }

        public override void Draw(RenderContext context)
        {
            Dispatcher.ForEach(ComponentDatas, entity =>
            {
                var instancingComponent = entity.Key;
                var instancingData = entity.Value;

                UpdateInstancingData(instancingComponent, instancingData);
            });
        }

        private void UpdateInstancingData(InstancingComponent instancingComponent, InstancingData instancingData)
        {
            if (instancingComponent.Enabled && instancingComponent.InstanceCount > 0)
            {
                instancingComponent.Process();

                if (instancingData.TransformComponent != null && instancingData.ModelComponent != null)
                {
                    // Bounding box
                    foreach (var meshInfo in instancingData.ModelComponent.MeshInfos)
                    {
                        var ibb = new BoundingBoxExt(instancingComponent.BoundingBox);
                        ibb.Transform(instancingData.TransformComponent.WorldMatrix);
                        var center = meshInfo.BoundingBox.Center + ibb.Center - instancingData.TransformComponent.WorldMatrix.TranslationVector;
                        var extend = meshInfo.BoundingBox.Extent + ibb.Extent;
                        meshInfo.BoundingBox = new BoundingBox(center - extend, center + extend);
                    } 
                }
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData data)
        {
            data.TransformComponent = component.Entity.Get<TransformComponent>();
            data.ModelComponent = component.Entity.Get<ModelComponent>();
        }

        // Instancing data per InstancingComponent
        protected override InstancingData GenerateComponentData([NotNull] Entity entity, [NotNull] InstancingComponent component)
        {
            return new InstancingData();
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData associatedData)
        {
            return true;
        }
    }
}
