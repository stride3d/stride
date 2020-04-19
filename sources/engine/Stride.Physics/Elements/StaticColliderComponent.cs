// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Physics
{
    [DataContract("StaticColliderComponent")]
    [Display("Static collider")]
    public sealed class StaticColliderComponent : PhysicsTriggerComponentBase
    {
        [DataMember(100)]
        public bool AlwaysUpdateNaviMeshCache { get; set; } = false;

        protected override void OnAttach()
        {
            NativeCollisionObject = new BulletSharp.CollisionObject
            {
                CollisionShape = ColliderShape.InternalShape,
                ContactProcessingThreshold = !Simulation.CanCcd ? 1e18f : 1e30f,
                UserObject = this,
            };

            NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.NoContactResponse;

            if (ColliderShape.NeedsCustomCollisionCallback)
            {
                NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.CustomMaterialCallback;
            }

            //this will set all the properties in the native side object
            base.OnAttach();

            UpdatePhysicsTransformation(); //this will set position and rotation of the collider

            Simulation.AddCollider(this, (CollisionFilterGroupFlags)CollisionGroup, CanCollideWith);
        }

        protected override void OnDetach()
        {
            if (NativeCollisionObject == null) return;

            Simulation.RemoveCollider(this);

            base.OnDetach();
        }
    }
}
