// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Physics;

namespace PhysicsSample
{
    public class RaycastingScript : SyncScript
    {
        private Simulation simulation;
        private CameraComponent camera;

        public override void Start()
        {
            camera = Entity.Get<CameraComponent>();
            simulation = this.GetSimulation();
        }

        public override void Update()
        {
            foreach (var pointerEvent in Input.PointerEvents.Where(x => x.EventType == PointerEventType.Pressed))
            {
                Raycast(pointerEvent.Position);
            }
        }

        private void Raycast(Vector2 screenPos)
        {
            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            screenPos.X *= backBuffer.Width;
            screenPos.Y *= backBuffer.Height;

            var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);

            var unprojectedNear =
                viewport.Unproject(
                    new Vector3(screenPos, 0.0f),
                    camera.ProjectionMatrix,
                    camera.ViewMatrix,
                    Matrix.Identity);

            var unprojectedFar =
                viewport.Unproject(
                    new Vector3(screenPos, 1.0f),
                    camera.ProjectionMatrix,
                    camera.ViewMatrix,
                    Matrix.Identity);

            var result = simulation.Raycast(unprojectedNear, unprojectedFar);
            if (!result.Succeeded || result.Collider == null) return;

            var rigidBody = result.Collider as RigidbodyComponent;
            if (rigidBody == null) return;

            rigidBody.Activate();
            rigidBody.ApplyImpulse(new Vector3(0, 5, 0));
        }
    }
}
