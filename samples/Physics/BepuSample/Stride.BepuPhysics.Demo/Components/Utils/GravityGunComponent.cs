// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.


using Stride.BepuPhysics.Constraints;
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

        private BodyComponent? _body;
        private float _distance = 0f;
        private Vector3 _localGrabPoint = new Vector3();
        private Quaternion _targetOrientation = Quaternion.Identity;
        private int simIndex = 0;
        private BepuConfiguration? _config;

        public CameraComponent? Camera { get; set; }
        public int SimIndex
        {
            get => simIndex; set
            {
                UnsetActive();
                simIndex = value;
            }
        }
        public void SetActive(HitInfo info)
        {
            if (_body != null || Camera == null)
                return;

            if (info.Collidable is not BodyComponent body)
                return;

            _oblscc = new OneBodyLinearServoConstraintComponent();
            _oblscc.ServoMaximumSpeed = float.MaxValue;
            _oblscc.ServoBaseSpeed = 0;
            _oblscc.ServoMaximumForce = 1000;
            _oblscc.A = body;
            _oblscc.Enabled = false;

            _obascc = new OneBodyAngularServoConstraintComponent();
            _obascc.ServoMaximumSpeed = float.MaxValue;
            _obascc.ServoBaseSpeed = 0;
            _obascc.ServoMaximumForce = 1000;
            _obascc.A = body;
            _obascc.Enabled = false;

            body.Entity.Add(_oblscc);
            body.Entity.Add(_obascc);

            _body = body;
            _distance = info.Distance;
            _localGrabPoint = Vector3.Transform(info.Point - _body.Position, Quaternion.Invert(_body.Orientation));
            _targetOrientation = body.Entity.Transform.GetWorldRot() * Quaternion.Invert(Camera.Entity.Transform.GetWorldRot());
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

            _obascc.TargetOrientation = _targetOrientation * Camera.Entity.Transform.GetWorldRot();
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

        public override void Start()
        {
            base.Start();
            _config = Services.GetService<BepuConfiguration>();
        }

        public override void Update()
        {
            if (Camera == null || _config == null)
                return;

            if (_body == null)
            {
                if (Input.IsMouseButtonDown(MouseButton.Left))
                {
                    if (_config.BepuSimulations[SimIndex].RayCast(Camera.Entity.Transform.WorldMatrix.TranslationVector, GetCameraRay(), 25, out var info))
                    {
                        SetActive(info);
                        DebugText.Print(info.ToString(), new(20, 500));
                    }
                }
            }
            else
            {
                _distance += Input.MouseWheelDelta;
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
        private Vector3 GetCameraRay() //There is porbably a better way, but it's ok for now.
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
