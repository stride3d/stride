// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering.Compositing;

namespace Stride.Graphics.Regression
{
    /// <summary>
    /// The default script for the scene editor camera.
    /// </summary>
    public class TestCamera : AsyncScript
    {
        private readonly Vector3 upVector = new Vector3(0, 1, 0);
        private readonly Vector3 forwardVector = new Vector3(0, 0, -1);

        private float yaw = (float)(Math.PI * 0.25f);
        private float pitch = -(float)(Math.PI * 0.25f);
        private Vector3 position = new Vector3(10, 10, 10);

        private readonly CameraComponent camera = new CameraComponent();

        /// <summary>
        /// Gets the camera component used to visualized the scene.
        /// </summary>
        public CameraComponent Camera { get { return camera; } }

        /// <summary>
        /// Gets or sets the moving speed of the camera (in units/second).
        /// </summary>
        public float MoveSpeed { get; set; }

        /// <summary>
        /// Gets or sets the rotation speed of the camera (in radian/screen units)
        /// </summary>
        public float RotationSpeed { get; set; }

        /// <summary>
        /// Create a new instance of scene camera.
        /// </summary>
        public TestCamera(GraphicsCompositor graphicsCompositor)
        {
            MoveSpeed = 10f;
            RotationSpeed = MathUtil.Pi / 2f;
            SceneUnit = 1;
            Camera.UseCustomAspectRatio = true;
            Camera.Slot = graphicsCompositor.Cameras[0].ToSlotId();
        }

        /// <summary>
        /// Gets or sets the Yaw rotation of the camera.
        /// </summary>
        public float Yaw
        {
            get { return yaw; }
            set
            {
                yaw = value;
                UpdateViewMatrix();
            }
        }

        public float SceneUnit { get; set; }

        /// <summary>
        /// Gets or sets the Pitch rotation of the camera.
        /// </summary>
        public float Pitch
        {
            get { return pitch; }
            set
            {
                pitch = value;
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// Gets or sets the position of the camera.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set
            {
                position = value;
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// Gets the current view matrix of the camera.
        /// </summary>
        public virtual Matrix ViewMatrix
        {
            get { return Camera.ViewMatrix; }
        }

        /// <summary>
        /// Gets the current projection matrix of the camera.
        /// </summary>
        public virtual Matrix ProjectionMatrix
        {
            get
            {
                Camera.Update();
                return Camera.ProjectionMatrix;
            }
        }
        /// <summary>
        /// Gets the current view projection matrix of the camera.
        /// </summary>
        public virtual Matrix ViewProjectionMatrix
        {
            get
            {
                Camera.Update();

                return Camera.ViewProjectionMatrix;
            }
        }

        /// <summary>
        /// Gets or sets the near plane of the camera.
        /// </summary>
        public float NearPlane
        {
            get { return Camera.NearClipPlane; }
            set { Camera.NearClipPlane = value; }
        }

        /// <summary>
        /// Gets or sets the far plane of the camera.
        /// </summary>
        public float FarPlane
        {
            get { return Camera.FarClipPlane; }
            set { Camera.FarClipPlane = value; }
        }

        /// <summary>
        /// Gets the current aspect ratio of the camera.
        /// </summary>
        public virtual float AspectRatio
        {
            get { return Camera.AspectRatio; }
        }

        /// <summary>
        /// Gets the current vertical field of view.
        /// </summary>
        public virtual float VerticalFieldOfView
        {
            get { return Camera.VerticalFieldOfView; }
            set { Camera.VerticalFieldOfView = value; }
        }

        public override async Task Execute()
        {
            Game.Window.ClientSizeChanged += OnClientSizeChanged;

            SetCamera();

            while (true)
            {
                UpdateCamera();

                await Script.NextFrame();
            }
        }

        private void OnClientSizeChanged(object sender, EventArgs eventArgs)
        {
            OnWindowSizeChanged();
        }

        /// <summary>
        /// Sets the target of the camera.
        /// </summary>
        public void SetTarget(Entity target, bool keepActualTargetDistance, float deltaDistance = 0, Vector3? targetOffset = null)
        {
            var ypr = Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0);
            var direction = Vector3.TransformNormal(forwardVector, Matrix.RotationQuaternion(ypr));
            var targetPos = target.Transform.WorldMatrix.TranslationVector;
            if (targetOffset.HasValue)
            {
                targetPos += targetOffset.Value;
            }
            float distance;
            if (keepActualTargetDistance)
            {
                distance = (targetPos - position).Length() + deltaDistance;
            }
            else
            {
                // TODO: Move PreviewGame.CalculateViewParameters into a more generic location
                var viewParam = CalculateViewParameters(target, upVector, direction, Camera);
                distance = viewParam.Distance * 1.2f;
            }

            Position = targetPos - direction * distance;
            UpdateViewMatrix();
        }

        /// <summary>
        /// Sets the cameras to the scene and editor system.
        /// </summary>
        protected virtual void SetCamera()
        {
            // set the camera values
            Camera.NearClipPlane = 0.1f;
            Camera.FarClipPlane = 1000f;
            Camera.UseCustomViewMatrix = true;
            OnWindowSizeChanged();
        }

        /// <summary>
        /// Update the camera parameters.
        /// </summary>
        protected virtual void UpdateCamera()
        {
            // Capture/release mouse when the button is pressed/released
            if (Input.IsMouseButtonPressed(MouseButton.Right))
            {
                Input.LockMousePosition();
            }
            else if (Input.IsMouseButtonReleased(MouseButton.Right))
            {
                Input.UnlockMousePosition();
            }

            // Update rotation according to mouse deltas
            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                yaw -= Input.MouseDelta.X * RotationSpeed;
                pitch -= Input.MouseDelta.Y * RotationSpeed;
            }

            // Compute translation speed according to framerate and modifiers
            float translationSpeed = MoveSpeed * SceneUnit * (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
                translationSpeed *= 10;

            // Compute base vectors for camera movement
            var rotation = Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0);

            var forward = Vector3.Transform(forwardVector, rotation);
            var up = Vector3.Transform(upVector, rotation);
            var right = Vector3.Cross(forward, up);

            // Update position according to movement input
            if (!IsModifierDown(false))
            {
                if (Input.IsKeyDown(Keys.A))
                    position += -right * translationSpeed;
                if (Input.IsKeyDown(Keys.D))
                    position += right * translationSpeed;
                if (Input.IsKeyDown(Keys.S))
                    position += -forward * translationSpeed;
                if (Input.IsKeyDown(Keys.W))
                    position += forward * translationSpeed;
                if (Input.IsKeyDown(Keys.Z))
                    position += -up * translationSpeed;
                if (Input.IsKeyDown(Keys.Q))
                    position += up * translationSpeed;
            }

            // Update the camera view matrix 
            UpdateViewMatrix();
        }

        internal bool IsModifierDown(bool includeShift)
        {
            return (includeShift && (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift)))
                   || Input.IsKeyDown(Keys.LeftCtrl) || Input.IsKeyDown(Keys.RightCtrl)
                   || Input.IsKeyDown(Keys.LeftAlt) || Input.IsKeyDown(Keys.RightAlt);
        }

        private void UpdateViewMatrix()
        {
            var rotation = Quaternion.Invert(Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
            var viewMatrix = Matrix.Translation(-position) * Matrix.RotationQuaternion(rotation);
            Camera.ViewMatrix = viewMatrix;
        }

        /// <summary>
        /// Called when the size of the windows changed.
        /// </summary>
        protected virtual void OnWindowSizeChanged()
        {
            // TODO GRAPHICS REFACTOR Need to get aspect ratio using graphics compositor
            Camera.AspectRatio = GraphicsDevice.Presenter.BackBuffer.Width / (float)GraphicsDevice.Presenter.BackBuffer.Height;
        }


        /// <summary>
        /// Structure defining all the parameters required to specify a view on an object
        /// </summary>
        public class ViewParameters
        {
            /// <summary>
            /// The orientation of the up vector
            /// </summary>
            public Vector3 UpVector { get; private set; }

            /// <summary>
            /// The point representing the target of the camera
            /// </summary>
            public Vector3 Target { get; set; }

            /// <summary>
            /// The angle between the <see cref="UpVector"/> and the view vector.
            /// </summary>
            /// <remarks>Its value is clamped between 0 and Pi</remarks>
            public float Phi { get; set; }

            /// <summary>
            /// The angle between the initial view vector and the projection of the view vector onto a plane perpendicular to <see cref="UpVector"/>.
            /// </summary>
            public float Theta { get; set; }

            /// <summary>
            /// The distance of the camera to the target point
            /// </summary>
            public float Distance { get; set; }

            /// <summary>
            /// The distance of the far plane after the object.
            /// </summary>
            public float FarPlane { get; set; }

            /// <summary>
            /// Create a new instance of view parameters with the provided Up and initial view vectors.
            /// That is, is set the value of <see cref="UpVector" /> and initializes the values of <see cref="Theta" /> and <see cref="Phi" />.
            /// </summary>
            /// <param name="upVector">The up vector of the object to view</param>
            /// <exception cref="System.ArgumentException">upVector cannot have a length of 0</exception>
            public ViewParameters(Vector3 upVector)
            {
                if (upVector.Length() < MathUtil.ZeroTolerance)
                    throw new ArgumentException("upVector cannot have a length of 0");

                UpVector = Vector3.Normalize(upVector);
            }
        }

        /// <summary>
        /// Calculate camera view parameters based on the entity bounding box.
        /// </summary>
        /// <param name="entity">The entity that we want to display</param>
        /// <param name="up">The vector representing the up of the entity</param>
        /// <param name="front">The vector representing the front of the entity</param>
        /// <param name="cameraComponent">The camera component used to render to object</param>
        /// <returns>Appropriate view parameters that can be used to display the entity</returns>
        public static ViewParameters CalculateViewParameters(Entity entity, Vector3 up, Vector3 front, CameraComponent cameraComponent)
        {
            var upAxis = up.Length() < MathUtil.ZeroTolerance ? Vector3.UnitY : Vector3.Normalize(up);

            // if we ensure that the whole object bounding box seen under the view vector in the frustum,
            // most of the time it results in object occupying only ~ 1/4 * 1/4 of the screen space.
            // So instead we decided to approximate the object with a bounding sphere (taking the risk that some of the objects are a little bit out of the screen)
            var boundingSphere = CalculateBoundingSpere(entity);

            // calculate the min distance from the object to see it entirely
            var minimunDistance = Math.Max(
                boundingSphere.Radius / (2f * (float)Math.Tan(cameraComponent.VerticalFieldOfView * cameraComponent.AspectRatio / 2f)),
                boundingSphere.Radius / (2f * (float)Math.Tan(cameraComponent.VerticalFieldOfView / 2f)));

            var distance = 1.2f * (minimunDistance + boundingSphere.Radius); // set the view distance such that the object can be seen entirely 
            var parameters = new ViewParameters(upAxis)
            {
                Target = boundingSphere.Center + entity.Transform.Position, // use of center of the bounding box as camera target
                Distance = distance,
                FarPlane = distance
            };

            return parameters;
        }

        /// <summary>
        /// Recursively approximate the bounding box of the scene represented by the entity.
        /// </summary>
        /// <param name="rootEntity">The root entity of the scene</param>
        /// <returns>The bounding box</returns>
        public static BoundingSphere CalculateBoundingSpere(Entity rootEntity)
        {
            var boundingSphere = new BoundingSphere();

            var model = rootEntity.Get<ModelComponent>();
            if (model != null && model.Model != null)
            {
                var boundingBox = model.Model.BoundingBox;
                var objectHalfSize = (boundingBox.Maximum - boundingBox.Minimum) / 2f;
                var scaledHalfSize = objectHalfSize * rootEntity.Transform.Scale;
                var sphereRadius = Math.Max(scaledHalfSize.X, Math.Max(scaledHalfSize.Y, scaledHalfSize.Z));
                var sphereCenter = (boundingBox.Maximum + boundingBox.Minimum) / 2f + rootEntity.Transform.Position;
                boundingSphere = new BoundingSphere(sphereCenter, sphereRadius);
            }

            rootEntity.Transform.UpdateLocalMatrix();

            foreach (var child in rootEntity.GetChildren())
            {
                var childSphere = CalculateBoundingSpere(child);
                if (Math.Abs(childSphere.Radius) < MathUtil.ZeroTolerance)
                    continue;

                var centerInParent = Vector3.TransformCoordinate(childSphere.Center, rootEntity.Transform.LocalMatrix);
                var radiusInParent = Vector3.TransformNormal(new Vector3(childSphere.Radius, 0, 0), rootEntity.Transform.LocalMatrix).Length();
                var sphereInParent = new BoundingSphere(centerInParent, radiusInParent);

                if (Math.Abs(boundingSphere.Radius) < MathUtil.ZeroTolerance)
                    boundingSphere = sphereInParent;
                else
                    boundingSphere = BoundingSphere.Merge(boundingSphere, sphereInParent);
            }

            return boundingSphere;
        }
    }
}
