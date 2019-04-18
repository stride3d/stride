// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.IO;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml.Events;
using Xenko.Core.Yaml.Serialization;
using Xenko.Core.Yaml.Serialization.Serializers;

namespace Xenko.Core.Settings
{
    [YamlSerializerFactory(SettingsProfileSerializer.YamlProfile)]
    internal class SettingsDictionarySerializer : DictionarySerializer
    {
        [CanBeNull]
        public override IYamlSerializable TryCreate(SerializerContext context, [NotNull] ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.Type;
            return type == typeof(SettingsDictionary) ? this : null;
        }

        protected override void WriteDictionaryItem(ref ObjectContext objectContext, KeyValuePair<object, object> keyValue, KeyValuePair<Type, Type> keyValueTypes)
        {
            var propertyKey = (string)keyValue.Key;
            objectContext.SerializerContext.ObjectSerializerBackend.WriteDictionaryKey(ref objectContext, propertyKey, keyValueTypes.Key);

            // Deduce expected value type from PropertyKey
            var parsingEvents = (List<ParsingEvent>)keyValue.Value;
            var writer = objectContext.Writer;
            foreach (var parsingEvent in parsingEvents)
            {
                writer.Emit(parsingEvent);
            }
        }

        protected override KeyValuePair<object, object> ReadDictionaryItem(ref ObjectContext objectContext, KeyValuePair<Type, Type> keyValueTypes)
        {
            // Read PropertyKey
            var keyResult = objectContext.SerializerContext.ObjectSerializerBackend.ReadDictionaryKey(ref objectContext, keyValueTypes.Key);
            // Temporary fix for launcher 3.0.x, which was serializing keys as UFile with !file prefix
            if (keyResult is UFile keyResultFile)
                keyResult = keyResultFile.FullPath;

            // Save the Yaml stream, in case loading fails we can keep this representation
            var parsingEvents = new List<ParsingEvent>();
            var reader = objectContext.Reader;
            var startDepth = reader.CurrentDepth;
            do
            {
                parsingEvents.Add(reader.Expect<ParsingEvent>());
            } while (reader.CurrentDepth > startDepth);

            var valueResult = parsingEvents;

            return new KeyValuePair<object, object>(keyResult, valueResult);
        }
    }
}
