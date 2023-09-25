//TODO (FROM BEPU DEMO !)

using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    struct GrabberComponent
    {
        bool active;
        BodyReference body;
        float t;
        Vector3 localGrabPoint;
        Quaternion targetOrientation;
        ConstraintHandle linearMotorHandle;
        ConstraintHandle angularMotorHandle;

        struct RayHitHandler : IRayHitHandler
        {
            public float T;
            public CollidableReference HitCollidable;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable)
            {
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable, int childIndex)
            {
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
            {
                //We are only interested in the earliest hit. This callback is executing within the traversal, so modifying maximumT informs the traversal
                //that it can skip any AABBs which are more distant than the new maximumT.
                maximumT = t;
                //Cache the earliest impact.
                T = t;
                HitCollidable = collidable;
            }

        }

        readonly void CreateMotorDescription(Vector3 target, float inverseMass, out OneBodyLinearServo linearDescription, out OneBodyAngularServo angularDescription)
        {
            linearDescription = new OneBodyLinearServo
            {
                LocalOffset = localGrabPoint,
                Target = target,
                ServoSettings = new ServoSettings(float.MaxValue, 0, 360 / inverseMass),
                SpringSettings = new SpringSettings(5, 2),
            };
            angularDescription = new OneBodyAngularServo
            {
                TargetOrientation = targetOrientation,
                ServoSettings = new ServoSettings(float.MaxValue, 0, localGrabPoint.Length() * 180 / inverseMass),
                SpringSettings = new SpringSettings(5, 2),
            };
        }

        public void Update(Simulation simulation, CameraComponent camera, bool mouseLocked, bool shouldGrab, Quaternion rotation, in Vector2 normalizedMousePosition)
        {
            //On the off chance some demo modifies the kinematic state, treat that as a grab terminator.
            var bodyExists = body.Exists && !body.Kinematic;
            if (active && (!shouldGrab || !bodyExists))
            {
                active = false;
                if (bodyExists)
                {
                    //If the body wasn't removed, then the constraint should be removed.
                    //(Body removal forces connected constraints to removed, so in that case we wouldn't have to worry about it.)
                    simulation.Solver.Remove(linearMotorHandle);
                    if (!Bodies.HasLockedInertia(body.LocalInertia.InverseInertiaTensor))
                        simulation.Solver.Remove(angularMotorHandle);
                }
                body = new BodyReference();
            }
            else if (shouldGrab && !active)
            {
                var screenPos = new Stride.Core.Mathematics.Vector2(0.5f, 0.5f);
                var invViewProj = Stride.Core.Mathematics.Matrix.Invert(camera.ViewProjectionMatrix);
                Stride.Core.Mathematics.Vector3 sPos;
                sPos.X = screenPos.X * 2f - 1f;
                sPos.Y = 1f - screenPos.Y * 2f;

                sPos.Z = 0f;
                var vectorNear = Stride.Core.Mathematics.Vector3.Transform(sPos, invViewProj);
                vectorNear /= vectorNear.W;
                vectorNear.Normalize();
                //TODOO probably .Position instead of decompose
                camera.Entity.Transform.WorldMatrix.Decompose(out Stride.Core.Mathematics.Vector3 scale, out Stride.Core.Mathematics.Quaternion rotationE, out Stride.Core.Mathematics.Vector3 translation);
                //TODOO probably
                //var forward = Stride.Core.Mathematics.Vector3.TransformNormal(-Stride.Core.Mathematics.Vector3.UnitZ, Stride.Core.Mathematics.Matrix.RotationQuaternion(Camera.Entity.Transform.Rotation)).ToNumericVector();

                var rayDirection = new Vector3(vectorNear.X, vectorNear.Y, vectorNear.Z);
                var hitHandler = default(RayHitHandler);
                hitHandler.T = float.MaxValue;
                simulation.RayCast(translation.ToNumericVector(), rayDirection, float.MaxValue, ref hitHandler);
                if (hitHandler.T < float.MaxValue && hitHandler.HitCollidable.Mobility == CollidableMobility.Dynamic)
                {
                    //Found something to grab!
                    t = hitHandler.T;
                    body = simulation.Bodies[hitHandler.HitCollidable.BodyHandle];
                    var hitLocation = translation.ToNumericVector() + rayDirection * t;
                    RigidPose.TransformByInverse(hitLocation, body.Pose, out localGrabPoint);
                    targetOrientation = body.Pose.Orientation;
                    active = true;
                    CreateMotorDescription(hitLocation, body.LocalInertia.InverseMass, out var linearDescription, out var angularDescription);
                    linearMotorHandle = simulation.Solver.Add(body.Handle, linearDescription);
                    if (!Bodies.HasLockedInertia(body.LocalInertia.InverseInertiaTensor))
                        angularMotorHandle = simulation.Solver.Add(body.Handle, angularDescription);
                }
            }
            else if (active)
            {
                var screenPos = new Stride.Core.Mathematics.Vector2(0.5f, 0.5f);
                var invViewProj = Stride.Core.Mathematics.Matrix.Invert(camera.ViewProjectionMatrix);
                Stride.Core.Mathematics.Vector3 sPos;
                sPos.X = screenPos.X * 2f - 1f;
                sPos.Y = 1f - screenPos.Y * 2f;

                sPos.Z = 0f;
                var vectorNear = Stride.Core.Mathematics.Vector3.Transform(sPos, invViewProj);
                vectorNear /= vectorNear.W;
                vectorNear.Normalize();
                camera.Entity.Transform.WorldMatrix.Decompose(out Stride.Core.Mathematics.Vector3 scale, out Stride.Core.Mathematics.Quaternion rotationE, out Stride.Core.Mathematics.Vector3 translation);

                var rayDirection = new Vector3(vectorNear.X, vectorNear.Y, vectorNear.Z);
                var targetPoint = translation.ToNumericVector() + rayDirection * t;
                targetOrientation = QuaternionEx.Normalize(QuaternionEx.Concatenate(targetOrientation, rotation));

                CreateMotorDescription(targetPoint, body.LocalInertia.InverseMass, out var linearDescription, out var angularDescription);
                simulation.Solver.ApplyDescription(linearMotorHandle, linearDescription);
                if (!Bodies.HasLockedInertia(body.LocalInertia.InverseInertiaTensor))
                    simulation.Solver.ApplyDescription(angularMotorHandle, angularDescription);
                body.Activity.TimestepsUnderThresholdCount = 0;
            }
        }
    }


}
