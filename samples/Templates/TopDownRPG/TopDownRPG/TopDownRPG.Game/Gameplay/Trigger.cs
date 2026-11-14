// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Events;

namespace TopDownRPG.Gameplay
{
    public enum CollisionEventType
    {
        /// <summary>
        /// Will broadcast an event only when the collision starts
        /// </summary>
        [Display("On Start")]
        StartOnly,

        /// <summary>
        /// Will broadcast an event only when the collision ends
        /// </summary>
        [Display("On End")]
        EndOnly,

        /// <summary>
        /// Will broadcast an event both when the collision starts and when it ends
        /// </summary>
        [Display("On Start and End")]
        StartAndEnd,
    }

    public class Trigger : StartupScript, IContactHandler
    {
        [Display("Condition")]
        public CollisionEventType TriggerCondition { get; set; } = CollisionEventType.StartOnly;

        public CollisionMask CollisionMask { get; set; } = CollisionMask.Everything;

        public bool CanObjectsPassThrough { get; set; } = true;

        // Let objects pass through this trigger, false would make objects bounce off it
        bool IContactHandler.NoContactResponse => CanObjectsPassThrough;

        [DataMemberIgnore]
        public EventKey<bool> TriggerEvent = new EventKey<bool>();

        void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
        {
            if ((contacts.Other.CollisionLayer.ToMask() & CollisionMask) == 0)
                return;
            if (TriggerCondition is CollisionEventType.StartOnly or CollisionEventType.StartAndEnd)
                TriggerEvent.Broadcast(true);
        }

        void IContactHandler.OnStoppedTouching<TManifold>(Contacts<TManifold> contacts)
        {
            if ((contacts.Other.CollisionLayer.ToMask() & CollisionMask) == 0)
                return;
            if (TriggerCondition is CollisionEventType.EndOnly or CollisionEventType.StartAndEnd)
                TriggerEvent.Broadcast(false);
        }
    }
}
