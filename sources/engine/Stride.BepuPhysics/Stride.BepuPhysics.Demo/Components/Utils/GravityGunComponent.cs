using BepuPhysics;
using BulletSharp.SoftBody;
using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Raycast;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;


namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class GravityGunComponent : SyncScript
    {
        private OneBodyLinearServoConstraintComponent? _oblscc;
        private OneBodyAngularServoConstraintComponent? _obascc;

        private BodyContainerComponent? _body;
        private float _distance = 0f;
        private Vector3 _localGrabPoint = new Vector3();
        private Quaternion _targetOrientation = Quaternion.Identity;
        public CameraComponent? Camera { get; set; }

        public void SetActive(HitInfo info)
        {
            if (_body != null)
                return;

            if (info.Container is not BodyContainerComponent body)
                return;

            _oblscc = new OneBodyLinearServoConstraintComponent();
            _oblscc.ServoMaximumSpeed = float.MaxValue;
            _oblscc.ServoBaseSpeed = 0;
            _oblscc.ServoMaximumForce = 1000;
            _oblscc.Bodies.Add(body);
            _oblscc.Enabled = false;

            _obascc = new OneBodyAngularServoConstraintComponent();
            _obascc.ServoMaximumSpeed = float.MaxValue;
            _obascc.ServoBaseSpeed = 0;
            _obascc.ServoMaximumForce = 1000;
            _obascc.Bodies.Add(body);
            _obascc.Enabled = false;

            body.Entity.Add(_oblscc);
            body.Entity.Add(_obascc);

            _body = body;
            _distance = info.Distance;
            _localGrabPoint = Vector3.Transform(info.Point.ToStrideVector() - _body.Position, Quaternion.Invert(_body.Orientation));
            _targetOrientation = body.Entity.Transform.GetWorldRot();
        }
        public void UpdateConstraints()
        {
            if (Camera == null || _body == null || _oblscc == null || _obascc == null)
                return;

            var rayDirection = GetCameraRay();
            var targetPoint = Camera.Entity.Transform.WorldMatrix.TranslationVector + rayDirection * _distance;

            _oblscc.LocalOffset = _localGrabPoint;
            _oblscc.Target = targetPoint;
            _oblscc.Enabled = true;

            _obascc.TargetOrientation = _targetOrientation;
            _obascc.Enabled = true;
        }
        public void UnsetActive()
        {
            if (_body == null)
                return;

            _body.Entity.Remove(_oblscc);
            _body.Entity.Remove(_obascc);

            _oblscc = null;
            _obascc = null;
            _body = null;
        }

        public override void Update()
        {
            if (Camera == null)
                return;

            if (_body == null)
            {
                if (Input.IsMouseButtonDown(MouseButton.Left))
                {
                    if (Services.GetService<BepuConfiguration>().BepuSimulations[0].RayCast(Camera.Entity.Transform.WorldMatrix.TranslationVector, GetCameraRay(), 25, out var info))
                    {
                        SetActive(info);
                        DebugText.Print(info.ToString(), new(20, 500));
                    }
                }
            }
            else
            {
                var rayDirection = GetCameraRay();
                var targetPoint = Camera.Entity.Transform.WorldMatrix.TranslationVector + rayDirection * _distance;

                DebugText.Print($"object should go at {targetPoint} and is at {_body.Position}", new(20, 600));
                DebugText.Print($"Camera : {Camera.Entity.Transform.GetWorldPos()}", new(20, 650));

                if (Input.IsKeyDown(Keys.K))
                {
                    _body.ApplyLinearImpulse(GetCameraRay() * 100f);
                    UnsetActive();
                    return;
                }

                if (Input.IsMouseButtonDown(MouseButton.Left))
                {
                    UpdateConstraints();
                }
                else
                {
                    UnsetActive();
                }
                

            }
        }
        private Vector3 GetCameraRay()
        {
            if (Camera == null)
                return Vector3.Zero;

            var wrot = Camera.Entity.Transform.GetWorldRot();
            var res = Vector3.Transform(-Vector3.UnitZ, wrot);
            DebugText.Print(res.ToString(), new(20, 550));
            return res;
        }
    }
}
