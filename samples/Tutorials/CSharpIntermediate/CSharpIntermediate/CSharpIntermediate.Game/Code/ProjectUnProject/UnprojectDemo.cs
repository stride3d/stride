using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class UnprojectDemo : SyncScript
    {
        private CameraComponent camera;
        public Entity sphereToClone;

        public override void Start()
        {
            camera = Entity.Get<CameraComponent>();
        }

        public override void Update()
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                var backBuffer = GraphicsDevice.Presenter.BackBuffer;
                var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);
                var camera = Entity.Get<CameraComponent>();
                var nearPosition = viewport.Unproject(new Vector3(Input.AbsoluteMousePosition, 0.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
                var farPosition = viewport.Unproject(new Vector3(Input.AbsoluteMousePosition, 1.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

                var hitResult = this.GetSimulation().Raycast(nearPosition, farPosition);

                if (hitResult.Succeeded)
                {
                    var sphereClone = sphereToClone.Clone();
                    sphereClone.Transform.Position = hitResult.Point;
                    Entity.Scene.Entities.Add(sphereClone);
                }
            }
        }
    }
}
