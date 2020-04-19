// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.DebugShapes;
using Stride.Engine;
using Stride.Particles.Components;
using Stride.Particles.DebugDraw;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(ParticleSystemComponent), false)]
    public class ParticleSystemGizmo : BillboardingGizmo<ParticleSystemComponent>
    {
        private DebugShapeRenderer shapeRenderer;

        public ParticleSystemGizmo(EntityComponent component)
            : base(component, "ParticleSystem", GizmoResources.ParticleGizmo)
        {
        }

        protected override Entity Create()
        {
            if (shapeRenderer == null)
            {
                shapeRenderer = new DebugShapeRenderer(GraphicsDevice, ((EntityHierarchyEditorGame)Game).EditorScene);
            }

            return base.Create();
        }

        protected override void Destroy()
        {
            shapeRenderer?.SetDebugShape(DebugShapeType.None, ParticlesShapesGroup);

            base.Destroy();
        }

        public override void Update()
        {
            // Update the locator & shape based on which element from the ParticleSystem is currently selected
            //var pos = Component.ParticleSystem.Translation;
            //var scl = Component.ParticleSystem.UniformScale;
            //var rot = Component.ParticleSystem.Rotation;

            // Update some edit-time settings on the particle system
            {
                var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

                var speed = Component.Speed;
                var particleSystemControl = Component.Control;
                particleSystemControl.Update(deltaTime * speed, Component.ParticleSystem);
            }

            if (!IsSelected || !IsEnabled)
            {
                shapeRenderer.SetDebugShape(DebugShapeType.None, ParticlesShapesGroup);

                base.Update();
                return;
            }

            var pos = Vector3.Zero;
            var rot = Quaternion.Identity;
            var scl = Vector3.One;
            DebugDrawShape drawShape = DebugDrawShape.None;

            if (Component.ParticleSystem.TryGetDebugDrawShape(ref drawShape, ref pos, ref rot, ref scl))
            {
                // TODO Invert cones

                shapeRenderer.SetDebugShape((DebugShapeType)drawShape, ParticlesShapesGroup);
                shapeRenderer.SetTransform(pos, rot, scl);

                base.Update();
                return;
            }

            // Last - if nothing else is selected, don't display anything
            shapeRenderer.SetDebugShape(DebugShapeType.None, ParticlesShapesGroup);

            base.Update();
        }

    }
}
