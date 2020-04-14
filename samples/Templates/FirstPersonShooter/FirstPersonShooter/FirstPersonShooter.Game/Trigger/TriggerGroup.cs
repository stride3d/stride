// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;

namespace FirstPersonShooter.Trigger
{
    [DataContract("TriggerGroup")]
    public class TriggerGroup
    {
        [DataMember(10)]
        [Display("Name")]
        public string Name { get; set; } = "NewTriggerGroup";

        [DataMember(20)]
        [Display("Events")]
        public List<TriggerEvent> TriggerEvents { get; } = new List<TriggerEvent>();

        public TriggerEvent Find(string name) => Find(x => x.Name.Equals(name));

        public List<TriggerEvent> FindAll(Predicate<TriggerEvent> match)
        {
            return TriggerEvents.FindAll(match);
        }

        public TriggerEvent Find(Predicate<TriggerEvent> match)
        {
            return TriggerEvents.Find(match);
        }
    }
}
