using CSharpIntermediate.Code.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

namespace CSharpIntermediate.Code
{
    public class ProjectDemo : SyncScript
    {
        public Entity projectSphere;
        public Entity projectSphereChild;
        private CameraComponent camera;

        public override void Start()
        {
            camera = Entity.Get<CameraComponent>();
        }

        public override void Update()
        {
            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            var sphereProjection = Vector3.Project(projectSphere.Transform.WorldMatrix.TranslationVector, 0, 0, backBuffer.Width, backBuffer.Height,0, 8, camera.ViewProjectionMatrix);
            var sphereChildProjection = Vector3.Project(projectSphereChild.Transform.WorldMatrix.TranslationVector, 0, 0, backBuffer.Width, backBuffer.Height,0, 8, camera.ViewProjectionMatrix);

            // Similar method using Viewports
            //var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);
            //var sphereProjection = viewport.Project(projectSphere.Transform.WorldMatrix.TranslationVector, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            //var sphereChildProjection = viewport.Project(projectSphereChild.Transform.WorldMatrix.TranslationVector, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            DebugText.Print($"Parent", new Int2(sphereProjection.XY()));
            DebugText.Print($"Child", new Int2(sphereChildProjection.XY()));
        }
    }
}
