// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type

using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Reflection;

namespace Xenko.Navigation
{
    /// <summary>
    /// Navigation agent
    /// </summary>
    [DataContract]
    [ObjectFactory(typeof(NavigationAgentSettingsFactory))]
    public class NavigationAgentSettings
    {
        /// <summary>
        /// Height of the actor
        /// </summary>
        /// <userdoc>
        /// The height of the entities in this group. Entities can't enter areas with ceilings lower than this value.
        /// </userdoc>
        [DataMember(0)]
        [DataMemberRange(0, 3)]
        public float Height;

        /// <summary>
        /// Maximum vertical distance this agent can climb
        /// </summary>
        /// <userdoc>
        /// The maximum height that entities in this group can climb. 
        /// </userdoc>
        [DataMember(1)]
        [Display("Maximum climb height")]
        [DataMemberRange(0, 3)]
        public float MaxClimb;

        /// <summary>
        /// Maximum slope angle this agent can climb
        /// </summary>
        /// <userdoc>
        /// The maximum incline (in degrees) that entities in this group can climb. Entities can't go up or down slopes higher than this value. 
        /// </userdoc>
        [Display("Maximum slope")]
        [DataMember(2)]
        public AngleSingle MaxSlope;

        /// <summary>
        /// Radius of the actor
        /// </summary>
        /// <userdoc>
        /// The larger this value, the larger the area of the navigation mesh entities use. Entities can't pass through gaps of less than twice the radius.
        /// </userdoc>
        [DataMember(3)]
        [DataMemberRange(0, 3)]
        public float Radius;
    }

    public class NavigationAgentSettingsFactory : IObjectFactory
    {
        public object New(Type type)
        {
            return new NavigationAgentSettings
            {
                Height = 1.0f,
                MaxClimb = 0.25f,
                MaxSlope = new AngleSingle(45.0f, AngleType.Degree),
                Radius = 0.5f,
            };
        }
    }
}
