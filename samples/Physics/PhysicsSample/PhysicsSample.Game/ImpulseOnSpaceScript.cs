// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Physics;

namespace PhysicsSample
{
    /// <summary>
    /// Apply an impulse on the entity when pressing key 'Space'
    /// </summary>
    public class ImpulseOnSpaceScript : SyncScript
    {
        public override void Update()
        {
            if (Input.IsKeyDown(Keys.Space))
            {
                var rigidBody = Entity.Get<RigidbodyComponent>();

                rigidBody.Activate();
                rigidBody.ApplyImpulse(new Vector3(0, 1, 0));
            }
        }
    }
}
