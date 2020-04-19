// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Engine.Design
{
    [DataContract]
    internal class CloneEntityComponentData
    {
        // Used to store entity data while in merge/text mode
        public static PropertyKey<CloneEntityComponentData> Key = new PropertyKey<CloneEntityComponentData>("Key", typeof(CloneEntityComponentData));

        [DataMemberCustomSerializer]
        public Entity Entity;
        public List<EntityComponentProperty> Properties;
        //public List<EntityComponentProperty> Properties;

        public static void RestoreEntityComponentData(EntityComponent entityComponent, CloneEntityComponentData data)
        {
            foreach (var componentProperty in data.Properties)
            {
                switch (componentProperty.Type)
                {
                    case EntityComponentPropertyType.Field:
                        {
                            var field = entityComponent.GetType().GetTypeInfo().GetDeclaredField(componentProperty.Name);
                            if (field == null) // Field disappeared? should we issue a warning?
                                continue;
                            var result = MergeObject(field.GetValue(entityComponent), componentProperty.Value);
                            field.SetValue(entityComponent, result);
                        }
                        break;
                    case EntityComponentPropertyType.Property:
                        {
                            var property = entityComponent.GetType().GetTypeInfo().GetDeclaredProperty(componentProperty.Name);
                            if (property == null) // Property disappeared? should we issue a warning?
                                continue;
                            var result = MergeObject(property.GetValue(entityComponent, null), componentProperty.Value);
                            if (property.CanWrite)
                                property.SetValue(entityComponent, result, null);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public static CloneEntityComponentData GenerateEntityComponentData(EntityComponent entityComponent)
        {
            var data = new CloneEntityComponentData { Properties = new List<EntityComponentProperty>() };
            foreach (var field in entityComponent.GetType().GetTypeInfo().DeclaredFields)
            {
                //if (!field.GetCustomAttributes(typeof(DataMemberConvertAttribute), true).Any())
                //    continue;

                data.Properties.Add(new EntityComponentProperty(EntityComponentPropertyType.Field, field.Name, field.GetValue(entityComponent)));
            }

            foreach (var property in entityComponent.GetType().GetTypeInfo().DeclaredProperties)
            {
                //if (!property.GetCustomAttributes(typeof(DataMemberConvertAttribute), true).Any())
                //    continue;

                data.Properties.Add(new EntityComponentProperty(EntityComponentPropertyType.Property, property.Name, property.GetValue(entityComponent, null)));
            }
            return data;
        }

        private static object MergeObject(object oldValue, object newValue)
        {
            if (oldValue is IList)
            {
                var oldList = (IList)oldValue;
                oldList.Clear();
                foreach (var item in (IEnumerable)newValue)
                {
                    oldList.Add(item);
                }
                return oldList;
            }
            if (oldValue is IDictionary)
            {
                var oldDictionary = (IDictionary)oldValue;
                oldDictionary.Clear();
                foreach (DictionaryEntry item in (IDictionary)newValue)
                {
                    oldDictionary.Add(item.Key, item.Value);
                }
                return oldDictionary;
            }

            return newValue;
        }
    }
}
