// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A Yaml serializer for <see cref="IKeyWithId"/>.
    /// </summary>
    [YamlSerializerFactory("Assets")] // TODO: use YamlAssetProfile.Name
    public class KeyWithIdSerializer : ItemIdSerializerBase
    {
        /// <summary>
        /// A key used in properties of serialization contexts to notify whether an override flag should be appened when serializing the key of the related <see cref="ItemId"/>.
        /// </summary>
        public static PropertyKey<string> OverrideKeyInfoKey = new PropertyKey<string>("OverrideKeyInfo", typeof(KeyWithIdSerializer));

        /// <inheritdoc/>
        public override object ConvertFrom(ref ObjectContext objectContext, Scalar fromScalar)
        {
            var idIndex = fromScalar.Value.IndexOf('~');
            var id = ItemId.Empty;
            var keyString = fromScalar.Value;
            if (idIndex >= 0)
            {
                var idString = fromScalar.Value.Substring(0, idIndex);
                keyString = fromScalar.Value.Substring(idIndex + 1);
                id = ItemId.Parse(idString);
            }
            var keyType = objectContext.Descriptor.Type.GetGenericArguments()[0];
            var keyDescriptor = objectContext.SerializerContext.FindTypeDescriptor(keyType);
            var keySerializer = objectContext.SerializerContext.Serializer.GetSerializer(objectContext.SerializerContext, keyDescriptor);
            var scalarKeySerializer = keySerializer as ScalarSerializerBase;
            // TODO: deserialize non-scalar keys!
            if (scalarKeySerializer == null)
                throw new InvalidOperationException("Non-scalar key not yet supported!");

            var context = new ObjectContext(objectContext.SerializerContext, null, keyDescriptor);
            var key = scalarKeySerializer.ConvertFrom(ref context, new Scalar(keyString));
            var result = Activator.CreateInstance(typeof(KeyWithId<>).MakeGenericType(keyType), id, key);
            return result;
        }

        /// <inheritdoc/>
        public override string ConvertTo(ref ObjectContext objectContext)
        {
            var key = (IKeyWithId)objectContext.Instance;
            var keyDescriptor = objectContext.SerializerContext.FindTypeDescriptor(key.KeyType);
            var keySerializer = objectContext.SerializerContext.Serializer.GetSerializer(objectContext.SerializerContext, keyDescriptor);

            // TODO: serialize non-scalar keys!
            // Guid:
            //     Key: {Key}
            //     Value: {Value}

            var scalarKeySerializer = keySerializer as ScalarSerializerBase;
            if (scalarKeySerializer == null)
                throw new InvalidOperationException("Non-scalar key not yet supported!");

            var context = new ObjectContext(objectContext.SerializerContext, key.Key, keyDescriptor);

            objectContext.Instance = key.Id;
            var itemIdPart = base.ConvertTo(ref objectContext);
            objectContext.Instance = key;

            if (key.IsDeleted)
                return $"{itemIdPart}~";

            var keyString = scalarKeySerializer.ConvertTo(ref context);
            string overrideInfo;
            if (objectContext.SerializerContext.Properties.TryGetValue(OverrideKeyInfoKey, out overrideInfo))
            {
                keyString += overrideInfo;
                objectContext.SerializerContext.Properties.Remove(OverrideKeyInfoKey);
            }
            return $"{itemIdPart}~{keyString}";
        }

        /// <inheritdoc/>
        public override bool CanVisit(Type type)
        {
            return typeof(IKeyWithId).IsAssignableFrom(type);
        }
    }
}
