using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Demo.Camera
{
    public class ThirdPersonCamera : SyncScript
    {
        private Entity? _firstPersonPivot;
        private Entity? _thirdPersonPivot;

        private Vector3 dynCameraOffset;
        private Vector2 maxCameraAnglesRadians;
        private Vector3 camRotation;

        public bool Enabled = true;
        public bool InvertMouseY = true;

        public Vector2 MouseSpeed = new Vector2(1.5f, 1.5f);
        public float MaxLookUpAngle = -360;
        public float MaxLookDownAngle = 360;

        public float MinimumCameraDistance = 1.5f;
        public Vector3 CameraOffset = new Vector3(0, 0, 5);



        public override void Start()
        {
            Game.IsMouseVisible = false;

            _firstPersonPivot = Entity.FindChild("FirstPersonPivot");
            _thirdPersonPivot = Entity.FindChild("ThirdPersonPivot");

            maxCameraAnglesRadians = new Vector2(MathUtil.DegreesToRadians(MaxLookUpAngle), MathUtil.DegreesToRadians(MaxLookDownAngle));
            camRotation = _firstPersonPivot.Transform.RotationEulerXYZ;
            Input.MousePosition = new Vector2(0.5f, 0.5f);
            dynCameraOffset = CameraOffset;
        }

        public override void Update()
        {
            if (_firstPersonPivot == null || _thirdPersonPivot == null) return;

            if (Input.IsKeyPressed(Keys.Tab))
            {
                Enabled = !Enabled;
                Game.IsMouseVisible = !Enabled;
                Input.UnlockMousePosition();
                Input.MousePosition = new Vector2(0.5f, 0.5f);
            }

            if (Enabled)
            {
                Input.LockMousePosition();
                var mouseMovement = -Input.MouseDelta * MouseSpeed;
                dynCameraOffset -= CameraOffset * Input.MouseWheelDelta * 0.1f;

                // Update rotation values with the mouse movement
                camRotation.Y += mouseMovement.X;
                camRotation.X += InvertMouseY ? mouseMovement.Y : -mouseMovement.Y;

                camRotation.X = MathUtil.Clamp(camRotation.X, maxCameraAnglesRadians.X, maxCameraAnglesRadians.Y);

                _firstPersonPivot.Transform.Rotation = Quaternion.RotationX(camRotation.X) * Quaternion.RotationY(camRotation.Y);
                _thirdPersonPivot.Transform.Position = dynCameraOffset;
                _thirdPersonPivot.Transform.UpdateWorldMatrix();

                //TODO Bepu raycast
                // Raycast from first person pivot to third person pivot
                //var raycastStart = firstPersonPivot.Transform.WorldMatrix.TranslationVector;
                //var raycastEnd = thirdPersonPivot.Transform.WorldMatrix.TranslationVector;

                //if (simulation.Raycast(raycastStart, raycastEnd, out HitResult hitResult))
                //{
                //    // If we hit something along the way, calculate the distance
                //    var hitDistance = Vector3.Distance(raycastStart, hitResult.Point);

                //    if (hitDistance >= MinimumCameraDistance)
                //    {
                //        // If the distance is larger than the minimum distance, place the camera at the hitpoint
                //        thirdPersonPivot.Transform.Position.Z = -(hitDistance - 0.1f);
                //    }
                //    else
                //    {
                //        // If the distance is lower than the minimum distance, place the camera at first person pivot
                //        thirdPersonPivot.Transform.Position = new Vector3(0);
                //    }
                //}
            }
        }
    }
}
