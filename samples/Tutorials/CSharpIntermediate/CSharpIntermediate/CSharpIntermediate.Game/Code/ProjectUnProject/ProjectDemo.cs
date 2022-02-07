using CSharpIntermediate.Code.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

namespace CSharpIntermediate.Code
{
    public class ProjectDemo : SyncScript
    {

        public CameraComponent camera;
        public Entity childBall;
        public Entity globalBall;
        private Texture backBuffer;

        public override void Start()
        {
            //camera = Entity.Get<CameraComponent>();
            backBuffer = GraphicsDevice.Presenter.BackBuffer;
        }

        public override void Update()
        {
            // Works
            //var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);
            //var childBall2d = viewport.Project(childBall.Transform.WorldMatrix.TranslationVector, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            //var globalBall2d = viewport.Project(globalBall.Transform.Position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            //Works too
            var childBall2d = Vector3.Project(childBall.Transform.WorldMatrix.TranslationVector, 0, 0, backBuffer.Width, backBuffer.Height, 0, 8, camera.ViewProjectionMatrix);
            var globalBall2d = Vector3.Project(globalBall.Transform.Position, 0, 0, backBuffer.Width, backBuffer.Height,0, 8, camera.ViewProjectionMatrix);

            DebugText.Print($"childBall2d {childBall2d.Print()}", new Int2(20, 40));
            DebugText.Print($"globalBall2d {globalBall2d.Print()}", new Int2(20, 60));

            DebugText.Print($"CHI", new Int2(childBall2d.XY()));
            DebugText.Print($"GLO", new Int2(globalBall2d.XY()));


        }
    }
}
