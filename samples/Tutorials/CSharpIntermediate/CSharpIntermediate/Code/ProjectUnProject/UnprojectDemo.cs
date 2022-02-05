using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Stride.Animations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class UnprojectDemo : SyncScript
    {

        public CameraComponent camera;
        public Entity childBall;
        public Entity globalBall;

        public override void Start()
        {
            //camera = Entity.Get<CameraComponent>();
        }

        public override void Update()
        {
            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            var mousePosition = Input.MousePosition;
            mousePosition.X *= backBuffer.Width;
            mousePosition.Y *= backBuffer.Height;

            //var mousePositionAbs = Input.AbsoluteMousePosition;
            var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);
            //var positionNearCamera = viewport.Unproject(new Vector3(mousePosition, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            //var positionFarFromCamera = viewport.Unproject(new Vector3(mousePosition, 2), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            var positionNearCamera = viewport.Unproject(new Vector3(mousePosition, 0), ref Entity.Transform.WorldMatrix);
            var positionFarFromCamera = viewport.Unproject(new Vector3(mousePosition, 2), ref Entity.Transform.WorldMatrix);

       
            childBall.Transform.Position = positionFarFromCamera;
            globalBall.Transform.Position = positionFarFromCamera;

            DebugText.Print($"Camera pos {Entity.Transform.Position.Print()}", new Int2(500, 20));
            DebugText.Print($"Mousepos {mousePosition.Print()}", new Int2(500, 40));
            //DebugText.Print($"MousePos abs {mousePositionAbs.Print()}", new Int2(500, 60));
            DebugText.Print($"positionNearCamera {positionNearCamera.Print()}", new Int2(500, 80));
            DebugText.Print($"positionFarFromCamera {positionFarFromCamera.Print()}", new Int2(500, 100));


            DebugText.Print($"childBall.Transform.Position {childBall.Transform.Position.Print()}", new Int2(500, 140));
            DebugText.Print($"globalBall.Transform.Position {globalBall.Transform.Position.Print()}", new Int2(500, 160));
        }
    }

    public static class VectorExtensionMethods
    {
        public static string Print(this Vector2 pos)
        {
            return $"{Math.Round(pos.X, 1)} , {Math.Round(pos.Y, 1)}";
        }

        public static string Print(this Vector3 pos)
        {
            return $"{Math.Round(pos.X, 1)} , {Math.Round(pos.Y, 1)} , {Math.Round(pos.Z, 1)}";
        }
    }
}
