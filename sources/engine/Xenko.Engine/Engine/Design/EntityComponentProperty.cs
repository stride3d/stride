// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Engine.Design
{
    [DataContract]
    public class EntityComponentProperty
    {
        public EntityComponentProperty()
        {
        }

        public EntityComponentProperty(EntityComponentPropertyType type, string name, object value)
        {
            Type = type;
            Name = name;
            Value = value;
        }

        public EntityComponentPropertyType Type { get; set; }
        public string Name { get; set; }

        public object Value { get; set; }
    }
}
