// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Serializers;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Rendering;

namespace Stride.Assets.Serializers
{
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class ParameterKeyDictionarySerializer : DictionaryWithIdsSerializer, IDataCustomVisitor
    {
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.Type;
            return CanVisit(type) ? this : null;
        }

        protected override void WriteDictionaryItem(ref ObjectContext objectContext, KeyValuePair<object, object> keyValue, KeyValuePair<Type, Type> keyValueTypes)
        {
            if (AreCollectionItemsIdentifiable(ref objectContext))
            {
                objectContext.ObjectSerializerBackend.WriteDictionaryKey(ref objectContext, keyValue.Key, keyValueTypes.Key);
                // Deduce expected value type from PropertyKey
                var propertyKey = (PropertyKey)((IKeyWithId)keyValue.Key).Key;
                objectContext.SerializerContext.ObjectSerializerBackend.WriteDictionaryValue(ref objectContext, keyValue.Key, keyValue.Value, propertyKey.PropertyType);
            }
            else
            {
                objectContext.ObjectSerializerBackend.WriteDictionaryKey(ref objectContext, keyValue.Key, keyValueTypes.Key);
                // Deduce expected value type from PropertyKey
                var propertyKey = (PropertyKey)keyValue.Key;
                objectContext.SerializerContext.ObjectSerializerBackend.WriteDictionaryValue(ref objectContext, keyValue.Key, keyValue.Value, propertyKey.PropertyType);
            }
        }

        protected override KeyValuePair<object, object> ReadDictionaryItem(ref ObjectContext objectContext, KeyValuePair<Type, Type> keyValueTypes)
        {
            if (AreCollectionItemsIdentifiable(ref objectContext))
            {
                var keyResult = objectContext.ObjectSerializerBackend.ReadDictionaryKey(ref objectContext, keyValueTypes.Key);
                var peek = objectContext.SerializerContext.Reader.Peek<Scalar>();
                if (Equals(peek?.Value, YamlDeletedKey))
                {
                    return ReadDeletedDictionaryItem(ref objectContext, keyResult);
                }
                var propertyKey = (PropertyKey)((IKeyWithId)keyResult).Key;
                var valueResult = objectContext.ObjectSerializerBackend.ReadDictionaryValue(ref objectContext, propertyKey.PropertyType, keyResult);
                return new KeyValuePair<object, object>(keyResult, valueResult);
            }
            else
            {
                var keyResult = objectContext.ObjectSerializerBackend.ReadDictionaryKey(ref objectContext, keyValueTypes.Key);
                var propertyKey = (PropertyKey)keyResult;
                var valueResult = objectContext.ObjectSerializerBackend.ReadDictionaryValue(ref objectContext, propertyKey.PropertyType, keyResult);
                return new KeyValuePair<object, object>(keyResult, valueResult);
            }
        }

        public bool CanVisit(Type type)
        {
            return typeof(IDictionary<ParameterKey, object>).IsAssignableFrom(type)
                || typeof(IDictionary<PropertyKey, object>).IsAssignableFrom(type);
        }

        public void Visit(ref VisitorContext context)
        {
            // Visit a ComputeColorParameters without visiting properties
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }
    }
}
