// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo used to dispatch work to sub-entity-gizmos.
    /// </summary>
    public abstract class DispatcherGizmo<TComponent> : EntityGizmo<TComponent> where TComponent : EntityComponent, new()
    {
        private EntityGizmo<TComponent> currentGizmo;

        private Type currentGizmoType;

        /// <summary>
        /// Get the type of the gizmo that should be currently used.
        /// </summary>
        /// <returns>The component data</returns>
        protected abstract Type GetGizmoType();

        protected override Entity Create()
        {
            return null;
        }

        public override void Initialize(IServiceRegistry services, Scene editorScene)
        {
            base.Initialize(services, editorScene);
            Update();
        }

        public override bool IsEnabled
        {
            set
            {
                base.IsEnabled = value;
                if (currentGizmo != null)
                    currentGizmo.IsEnabled = value;
            }
        }

        public override bool IsSelected
        {
            set
            {
                base.IsSelected = value;
                if (currentGizmo != null)
                    currentGizmo.IsSelected = value;
            }
        }

        public override float SizeFactor
        {
            set
            {
                base.SizeFactor = value;
                if (currentGizmo != null)
                    currentGizmo.SizeFactor = value;
            }
        }

        public override bool IsUnderMouse(int pickedComponentId)
        {
            return currentGizmo != null && currentGizmo.IsUnderMouse(pickedComponentId);
        }

        public override void Update()
        {
            var gizmoType = GetGizmoType();
            if (currentGizmoType != gizmoType)
            {
                // Delete the old gizmo
                if (currentGizmo != null)
                {
                    currentGizmo.Dispose();
                    currentGizmo = null;
                }

                // Create the new gizmo
                if (gizmoType != null && typeof(IGizmo).IsAssignableFrom(gizmoType))
                {
                    if (gizmoType.GetConstructor(new[] { typeof(EntityComponent) }) != null)
                    {
                        currentGizmo = (EntityGizmo<TComponent>)Activator.CreateInstance(gizmoType, Component);
                    }
                    else
                    {
                        throw new Exception($"Can not construct gizmo of type {gizmoType}, it does not have a constructor taking a single {nameof(EntityComponent)}");
                    }
                }

                // Initialize the new gizmo
                currentGizmo?.InitializeContentEntity(ContentEntity);
                currentGizmo?.Initialize(Services, EditorScene);

                // Set the selected and enabled state of the gizmo
                IsSelected = IsSelected;
                IsEnabled = IsEnabled;
            }
            currentGizmoType = gizmoType;

            // update the current gizmo
            currentGizmo?.Update();
        }

        protected override void Destroy()
        {
            base.Destroy();
            currentGizmo?.Dispose();
        }

        protected DispatcherGizmo(EntityComponent component) : base(component)
        {
        }
    }
}
