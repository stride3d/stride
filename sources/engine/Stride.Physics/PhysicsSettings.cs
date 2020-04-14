// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;

namespace Stride.Physics
{
    [DataContract]
    [Display("Physics")]
    public class PhysicsSettings : Configuration
    {
        [DataMember(10)]
        public PhysicsEngineFlags Flags;

        /// <userdoc>
        /// The maximum number of simulations the the physics engine can run in a frame to compensate for slowdown
        /// </userdoc>
        [DataMember(20)]
        public int MaxSubSteps = 1;

        /// <userdoc>
        /// The length in seconds of a physics simulation frame. The default is 0.016667 (one sixtieth of a second)
        /// </userdoc>
        [DataMember(30)]
        public float FixedTimeStep = 1.0f / 60.0f;
    }
}
