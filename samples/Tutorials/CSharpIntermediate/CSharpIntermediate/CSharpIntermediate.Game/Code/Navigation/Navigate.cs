using System;
using System.Collections.Generic;
using CSharpIntermediate.Code.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Navigation;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class Navigate : SyncScript
    {
        public Entity RegularCharacter;
        public Entity TargetSphere;

        private NavigationComponent navigationComponent;
        private List<Vector3> currentPath = new();

        public override void Start()
        {
            navigationComponent = Entity.Get<NavigationComponent>();
        }

        public override void Update()
        {

            DebugText.Print($"Left click to set Regular character target", new Int2(200, 20));

            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                currentPath.Clear();

                var backBuffer = GraphicsDevice.Presenter.BackBuffer;
                var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);

                var camera = Entity.Get<CameraComponent>();
                var nearPosition = viewport.Unproject(new Vector3(Input.AbsoluteMousePosition, 0.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
                var farPosition = viewport.Unproject(new Vector3(Input.AbsoluteMousePosition, 2.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
                
                DebugText.Print(nearPosition.Print(), new Int2(20, 40));
                DebugText.Print(farPosition.Print(), new Int2(20, 50));

                var hitResult = this.GetSimulation().Raycast(nearPosition, farPosition);
                if (hitResult.Succeeded)
                {
                    TargetSphere.Transform.Position = hitResult.Point;

                    //if (navigationComponent.TryFindPath(hitResult.Point, currentPath))
                    //{

                    //}
                    //else
                    //{
                    //    // A path couldn't be found using this navigation mesh
                    //}
                }
            }
        }

        private void UpdateTarget(Entity tinyCharacter)
        {

        }
    }
}
