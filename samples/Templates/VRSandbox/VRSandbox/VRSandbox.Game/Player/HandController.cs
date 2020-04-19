// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
using Stride.VirtualReality;

namespace VRSandbox.Player
{
    public class HandController : SyncScript
    {
        private readonly EventReceiver<HandsInput> handsControlEvent = new EventReceiver<HandsInput>(PlayerInput.HandsControlEventKey);

        [Display("Move Speed")]
        public float MaxMoveSpeed { get; set; } = 1;

        [Display("Hand")]
        public HandSide Hand { get; set; } = HandSide.Right;

        private Entity grabbedEntity;

        public Entity Head;

        private AnimationComponent animationComponent;

        private PlayingAnimation animation;

        private VRDeviceSystem vrDeviceSystem;

        public override void Start()
        {
            base.Start();

            vrDeviceSystem = Services.GetService<VRDeviceSystem>();

            animationComponent = Entity.FindChild("Entity")?.Get<AnimationComponent>();

            // Assume Mars gravity for the sake of more enjoyable game
            this.GetSimulation().Gravity = new Vector3(0, -3.711f, 0);

            animation = animationComponent?.Play("Grab");
            if(animation != null)
                animation.TimeFactor = -1.0f;
        }

        /// <summary>
        /// Called on every frame update
        /// </summary>
        public override void Update()
        {
            UpdateVrController();
        }

        // Use this if you have one or two vr controllers (usually one for each hand)
        private void UpdateVrController()
        {
            var vrController = Hand == HandSide.Left ? vrDeviceSystem.Device?.LeftHand : vrDeviceSystem.Device?.RightHand;

            if (vrController == null) return;

            if (vrController.State == DeviceState.Invalid) return;

            Head.Transform.UpdateWorldMatrix();
            Entity.Transform.Parent.UpdateWorldMatrix();
            var parentWorldInv = Entity.Transform.Parent.WorldMatrix;
            parentWorldInv.Invert();

            var handPose = Matrix.RotationQuaternion(vrController.Rotation) * Matrix.Translation(vrController.Position);

            var mat = handPose * Head.Transform.WorldMatrix * parentWorldInv;
            Vector3 pos, scale;
            Quaternion rot;
            mat.Decompose(out scale, out rot, out pos);
            Entity.Transform.Position = pos;
            Entity.Transform.Rotation = rot;

            if (vrController.Trigger > 0.5f)
            {
                if (animation != null)
                    animation.TimeFactor = 1.0f;

                GrabNewEntity();
            }
            else
            {
                if (animation != null)
                    animation.TimeFactor = -1.0f;

                var rb = grabbedEntity?.Get<RigidbodyComponent>();

                ReleaseGrabbedEntity();

                rb?.ApplyImpulse(vrController.LinearVelocity);
                rb?.ApplyTorqueImpulse(vrController.AngularVelocity / 5.0f);
            }
        }

        private void GrabNewEntity()
        {
            if (grabbedEntity != null)
                return;

            var collisions = Entity.Get<PhysicsComponent>().Collisions;
            if (collisions.Count == 0)
                return;

            var enumerator = collisions.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var collision = enumerator.Current;
                var entityA = collision?.ColliderA?.Entity;
                var entityB = collision?.ColliderB?.Entity;

                var otherEntity = (entityA == Entity) ? entityB : entityA;

                var otherRigidBody = otherEntity?.Get<RigidbodyComponent>();
                if (otherRigidBody == null || otherRigidBody.IsKinematic ||
                    otherRigidBody.CollisionGroup == CollisionFilterGroups.CharacterFilter)
                    continue;

                grabbedEntity = otherEntity;
                break;
            }

            if (grabbedEntity == null)
                return;

            var rigidBody = grabbedEntity?.Get<RigidbodyComponent>();
            rigidBody.IsKinematic = true;
            rigidBody.CanSleep = false;

            Vector3 posObject, posParent;
            Vector3 sclObject, sclParent;
            Quaternion rotObject, rotParent;

            // Make sure old positions are up to date
            grabbedEntity.Transform.UpdateWorldMatrix();
            grabbedEntity.Transform.WorldMatrix.Decompose(out sclObject, out rotObject, out posObject);
            var parentEntity = Entity.GetChild(0);
            parentEntity.Transform.WorldMatrix.Decompose(out sclParent, out rotParent, out posParent);

            // Calculate relative transformations
            posObject -= posParent;
            posObject /= sclParent;
            rotParent.Conjugate();
            rotParent.Rotate(ref posObject);
            rotObject = rotParent * rotObject;

            // Attach the object to the parent
            var transformLink = grabbedEntity.Get<ModelNodeLinkComponent>();
            if (transformLink != null)
            {
                transformLink.Target = parentEntity.Get<ModelComponent>();
            }
            else
            {
                transformLink = new ModelNodeLinkComponent();
                transformLink.Target = parentEntity.Get<ModelComponent>();
                grabbedEntity.Add(transformLink);
            }

            grabbedEntity.Transform.UseTRS = true;
            grabbedEntity.Transform.Position = posObject;
            grabbedEntity.Transform.Rotation = rotObject;
        }

        private void ReleaseGrabbedEntity()
        {
            if (grabbedEntity == null)
                return;

            var rigidBody = grabbedEntity.Get<RigidbodyComponent>();

            rigidBody.IsKinematic = false;
            rigidBody.CanSleep = true;

            // Update the entity world matrix and set it as local, because the link will disappear after that
            grabbedEntity.Transform.UpdateWorldMatrix();
            grabbedEntity.Transform.LocalMatrix = grabbedEntity.Transform.WorldMatrix;

            // Remove the model node link
            var transformLink = grabbedEntity.Get<ModelNodeLinkComponent>();
            if (transformLink != null)
            {
                grabbedEntity.Remove<ModelNodeLinkComponent>();
                transformLink.Target = null;
            }

            grabbedEntity.Transform.UseTRS = false;
            grabbedEntity.Transform.TransformLink = null;
            grabbedEntity.Transform.UpdateWorldMatrix();    // Will set World matrix = Local matrix

            grabbedEntity = null;
        }

        // Use this if you have a regular controller/gamepad
        private void UpdatePlayerInput()
        {
            var vrController = Hand == HandSide.Left ? vrDeviceSystem.Device?.LeftHand : vrDeviceSystem.Device?.RightHand;

            if (vrController != null && vrController.State != DeviceState.Invalid) return;

            HandsInput handsInput;
            if (!handsControlEvent.TryReceive(out handsInput))
                return;

            var hand = (int) Hand;

            var dt = (float) (Game.UpdateTime.Elapsed.Milliseconds * 0.001);

            // Movement
            var amountMovement = handsInput.HandMovement[hand] * (dt * MaxMoveSpeed);
            Entity.Transform.Position += amountMovement;

            // TODO Hand orientation should match the controller orientation

            // Grabbed object
            if (handsInput.HandGrab[hand] >= 0.5f)
                GrabNewEntity();
            else
                ReleaseGrabbedEntity();
        }
    }
}
