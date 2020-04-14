// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Physics;

namespace GravitySensor
{
    /// <summary>
    /// This script will make sure that all the physics elements of this entity will never be set to sleep status
    /// The physics engine will sometimes set colliders to sleep state to reduce processor usage when there is no motion happening
    /// Those colliders will wake up if an external (an other collider hitting us) collision happens, but in this case we need to prevent this behavior totally,
    /// as there will be no external collision once the motion is 0.
    /// </summary>
    public class NoSleepScript : AsyncScript
    {
        public override Task Execute()
        {
            foreach (var physicsElement in Entity.GetAll<RigidbodyComponent>())
            {
                physicsElement.CanSleep = false;
            }

            return Task.FromResult(0);
        }
    }
}
