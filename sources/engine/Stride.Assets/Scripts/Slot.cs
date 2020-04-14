// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Assets.Scripts
{
    [DataContract]
    [DataStyle(DataStyle.Compact)]
    public class Slot : IIdentifiable
    {
        public Slot()
        {
            Id = Guid.NewGuid();
        }

        public Slot(SlotDirection direction, SlotKind kind, string name = null, SlotFlags flags = SlotFlags.None, string type = null, string value = null) : this()
        {
            Direction = direction;
            Kind = kind;
            Name = name;
            Flags = flags;
            Type = type;
            Value = value;
        }

        [DataMemberIgnore]
        public Block Owner { get; internal set; }

        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        // TODO: Internal setter when serialization supports it
        [DataMember(0)]
        public SlotDirection Direction { get; set; }

        // TODO: Internal setter when serialization supports it
        [DataMember(10)]
        [DefaultValue(SlotKind.Value)]
        public SlotKind Kind { get; set; }

        // TODO: Internal setter when serialization supports it
        /// <summary>
        /// The name of this slot.
        /// </summary>
        [DataMember(20)]
        [DefaultValue(null)]
        public string Name { get; set; }


        // TODO: Internal setter when serialization supports it
        /// <summary>
        /// The type of this slot, only used as hint for input slots.
        /// </summary>
        [DataMember(30)]
        [DefaultValue(null)]
        public string Type { get; set; }

        // TODO: Internal setter when serialization supports it
        [DataMember(40)]
        [DefaultValue(SlotFlags.None)]

        public SlotFlags Flags { get; set; }

        [DataMember(50)]
        [DefaultValue(null)]
        public string Value { get; set; }

        public override string ToString()
        {
            return $"[{Direction}/{Kind}] {Name ?? "Unnamed"}";
        }
    }
}
