// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Engine;
using Stride.Extensions;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public abstract class EntityGizmo : GizmoBase, IEntityGizmo
    {
        private readonly HashSet<int> componentsIds = new HashSet<int>();

        /// <summary>
        /// Gets the type of components associated with this gizmo
        /// </summary>
        public abstract Type ComponentType { get; }

        /// <summary>
        /// Gets wether this gizmo is a main gizmo.
        /// </summary>
        public bool IsMainGizmo { get { var attribute = GetType().GetCustomAttribute<GizmoComponentAttribute>(false); return attribute != null && attribute.IsMainGizmo; } }

        /// <summary>
        /// Gets the entity that receive scaling of the gizmo. If null, it will be <see cref="GizmoBase.GizmoRootEntity"/>.
        /// </summary>
        public Entity GizmoScalingEntity { get; protected set; }

        /// <summary>
        /// Gets the associated scene entity.
        /// </summary>
        public Entity ContentEntity { get; private set; }

        /// <summary>
        /// Gets or sets whether this gizmo is currently selected.
        /// </summary>
        public virtual bool IsSelected { get; set; }

        private IEditorGameComponentGizmoService gizmos;

        private IEditorGameCameraService camera;

        public void InitializeContentEntity(Entity contentEntity)
        {
            if (contentEntity == null) throw new ArgumentNullException(nameof(contentEntity));
            if (ContentEntity != null) throw new InvalidOperationException("InitializeContentEntity has already been invoked.");
            ContentEntity = contentEntity;
        }

        public override void Initialize(IServiceRegistry services, Scene editorScene)
        {
            base.Initialize(services, editorScene);

            if (GizmoRootEntity != null)
                CollectComponentIds(GizmoRootEntity);

            gizmos = Game.EditorServices.Get<IEditorGameComponentGizmoService>();
            camera = Game.EditorServices.Get<IEditorGameCameraService>();
        }

        /// <inheritdoc/>
        public virtual void Update()
        {
            if (ContentEntity == null || GizmoRootEntity == null || gizmos == null || camera == null)
                return;

            var gizmoScale = SizeFactor;
            if (gizmos.FixedSize && !camera.IsOrthographic)
            {
                // Increase the size of the gizmo as it is moving away from the camera to keep a constant gizmo size
                var distanceToCamera = Vector3.TransformCoordinate(ContentEntity.Transform.Position, camera.ViewMatrix).Z;
                gizmoScale = distanceToCamera * SizeFactor / (gizmos.SceneUnit * 10.0f);
            }

            // calculate the world matrix of the gizmo so that it is positioned exactly as the corresponding scene entity
            // except the scale that is re-adjusted to the gizmo desired size (gizmo are insert at scene root so LocalMatrix = WorldMatrix)
            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rotation, out translation);
            scale = new Vector3(gizmoScale * SizeAdjustmentFactor * gizmos.SceneUnit); // re-adjust the size of the gizmo

            // force the update of the world matrix
            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Rotation = rotation;
            (GizmoScalingEntity ?? GizmoRootEntity).Transform.Scale = scale; // Apply scaling on GizmoScalingEntity if it exists
            GizmoRootEntity.Transform.UpdateWorldMatrix();
        }

        private void CollectComponentIds(Entity entity)
        {
            foreach (var component in entity.Components)
            {
                componentsIds.Add(RuntimeIdHelper.ToRuntimeId(component));
            }

            foreach (var child in entity.GetChildren())
            {
                CollectComponentIds(child);
            }
        }

        public override bool IsUnderMouse(int pickedComponentId)
        {
            return componentsIds.Contains(pickedComponentId);
        }
    }

    public abstract class EntityGizmo<TComponent> : EntityGizmo where TComponent : EntityComponent
    {
        protected EntityGizmo(EntityComponent component)
        {
            Component = (TComponent)component;
        }

        /// <summary>
        /// Gets the scene entity component.
        /// </summary>
        protected TComponent Component { get; }

        /// <inheritdoc/>
        public override Type ComponentType => Component.GetType();
    }
}
