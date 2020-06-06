using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceWire.TcpIp;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine.Processors
{
    public class InstancingProcessor : EntityProcessor<InstancingComponent, InstancingProcessor.InstancingData>
    {
        public enum InstancingType
        {
            EntityTyransform,
            UserArray,
            UserBuffer
        }

        public class InstancingData
        {
            public InstancingType Type;
            public bool IsSingleWithModelComponent;
            public TransformComponent TransformComponent;
            public ModelComponent ModelComponent;
        }

        public class InstancingGroupInfo
        {
            public ModelComponent ModelComponent;
            public KeyValuePair<InstancingComponent, InstancingData> MasterInstancing;
            public List<KeyValuePair<InstancingComponent, InstancingData>> Components = new List<KeyValuePair<InstancingComponent, InstancingData>>();
        }

        public InstancingProcessor() 
            : base (typeof(TransformComponent)) // Requires TransformComponent
        {
            // After TransformProcessor but before ModelRenderProcessor
            Order = -100;
        }

        ArrayPool<Matrix> ArrayPool = ArrayPool<Matrix>.Create();
        Dictionary<ModelComponent, InstancingGroupInfo> SingleInstanceGroups = new Dictionary<ModelComponent, InstancingGroupInfo>();
        List<KeyValuePair<InstancingComponent, InstancingData>> ManyInstancing = new List<KeyValuePair<InstancingComponent, InstancingData>>();

        public override void Draw(RenderContext context)
        {
            // Return array memory
            foreach (var group in SingleInstanceGroups)
            {
                ArrayPool.Return(((InstancingEntityTransform)group.Value.MasterInstancing.Key.Type).WorldMatrices);
            }

            // Reset instancing collections
            SingleInstanceGroups.Clear();
            ManyInstancing.Clear();

            // Build groups by model component and instancing type
            foreach (var componentData in ComponentDatas)
            {
                var data = componentData.Value;

                if (data.ModelComponent == null)
                    continue;

                if (data.Type == InstancingType.EntityTyransform)
                {
                    if (SingleInstanceGroups.TryGetValue(data.ModelComponent, out var groupInfo))
                    {
                        groupInfo.Components.Add(componentData);
                        if (data.IsSingleWithModelComponent)
                            groupInfo.MasterInstancing = componentData;
                    }
                    else // FIXME: should be allocation free
                    {
                        var newGroupInfo = new InstancingGroupInfo();
                        newGroupInfo.Components.Add(componentData);
                        if (data.IsSingleWithModelComponent)
                            newGroupInfo.MasterInstancing = componentData;
                        SingleInstanceGroups[data.ModelComponent] = newGroupInfo;
                    }
                }
                else // UserArray or UserBuffer
                {
                    ManyInstancing.Add(componentData);
                }
            }

            // Build matrix array
            foreach (var item in SingleInstanceGroups)
            {
                var group = item.Value;
                var groupCount = group.Components.Count;
                var instancing = (InstancingEntityTransform)group.MasterInstancing.Key.Type;
                instancing.ClearEntities();

                var matrices = ArrayPool.Rent(groupCount);
                for (int i = 0; i < groupCount; i++)
                {
                    var data = group.Components[i].Value.TransformComponent;
                    matrices[i] = data.WorldMatrix;
                    instancing.AddInstanceEntity(data.Entity);
                }

                // Assign matrix array
                instancing.UpdateWorldMatrices(matrices, groupCount);
                ManyInstancing.Add(group.MasterInstancing);
            }

            // Process the components
            Dispatcher.ForEach(ManyInstancing, entity =>
            {
                var instancingComponent = entity.Key;
                var instancingData = entity.Value;

                if (instancingComponent.Type is IInstancingMany instancingMany)
                    UpdateInstancingDataMany(instancingComponent, instancingMany, instancingData);
            });
        }

        private void UpdateInstancingDataMany(InstancingComponent instancingComponent, IInstancingMany instancingMany, InstancingData instancingData)
        {
            if (instancingComponent.Enabled && instancingMany.InstanceCount > 0)
            {
                // Calculate inverse world and bounding box
                instancingMany.Update();

                if (instancingData.TransformComponent != null && instancingData.ModelComponent != null)
                {
                    // Bounding box
                    foreach (var meshInfo in instancingData.ModelComponent.MeshInfos)
                    {
                        var ibb = new BoundingBoxExt(instancingMany.BoundingBox);
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
            var type = InstancingType.UserArray;
            if (component.Type is InstancingEntityTransform)
                type = InstancingType.EntityTyransform;
            else if (component.Type is InstancingUserBuffer)
                type = InstancingType.UserBuffer;

            data.Type = type;

            if (type == InstancingType.EntityTyransform)
            {
                var entityModelComponent = component.Entity.Get<ModelComponent>();
                var linkedModelComponent = (component.Type as InstancingEntityTransform)?.Master ?? entityModelComponent;
                data.IsSingleWithModelComponent = entityModelComponent != null && linkedModelComponent == entityModelComponent;
                data.TransformComponent = component.Entity.Get<TransformComponent>();
                data.ModelComponent = linkedModelComponent;
            }
            else
            {
                data.TransformComponent = component.Entity.Get<TransformComponent>();
                data.ModelComponent = component.Entity.Get<ModelComponent>();
            }

        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData data)
        {
            if (data.IsSingleWithModelComponent)
            {
                var matrices = ((InstancingEntityTransform)component.Type)?.WorldMatrices;
                if (matrices != null)
                    ArrayPool.Return(matrices);
            }
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
