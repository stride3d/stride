// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(LightShaftBoundingVolumeComponent), false)]
    public class LightShaftBoundingVolumeGizmo : EntityGizmo<LightShaftBoundingVolumeComponent>
    {
        private Model lastModel;
        private ModelComponent modelComponent;

        public LightShaftBoundingVolumeGizmo(EntityComponent component) : base(component)
        {
            RenderGroup = LightShaftsGroup;
        }

        protected override Entity Create()
        {
            var entity = new Entity("Light shaft bounding volume Gizmo Root Entity (id={0})".ToFormat(ContentEntity.Id));
            entity.Add(modelComponent = new ModelComponent { RenderGroup = RenderGroup });
            return entity;
        }

        public override void Update()
        {
            if (ContentEntity == null || GizmoRootEntity == null)
                return;

            if (lastModel != Component.Model)
            {
                UpdateModel(Component.Model);
                lastModel = Component.Model;
            }
            modelComponent.Enabled = Component.Enabled;
            GizmoRootEntity.Transform.UseTRS = false;
            GizmoRootEntity.Transform.LocalMatrix = Component.Entity.Transform.WorldMatrix;
            GizmoRootEntity.Transform.UpdateWorldMatrix();
        }

        void UpdateModel(Model model)
        {
            modelComponent.Model = model;
        }
    }
}
