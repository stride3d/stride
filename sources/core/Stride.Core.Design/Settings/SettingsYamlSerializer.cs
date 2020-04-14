// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Stride.Core.Annotations;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Settings
{
    /// <summary>
    /// Default Yaml serializer used to serialize assets by default.
    /// </summary>
    public class SettingsYamlSerializer : YamlSerializer
    {
        public static new SettingsYamlSerializer Default { get; set; } = new SettingsYamlSerializer();

        /// <summary>
        /// Deserializes an object from the specified stream (expecting a YAML string) into an existing object.
        /// </summary>
        /// <param name="stream">A YAML string from a stream.</param>
        /// <param name="existingObject">The object to deserialize into.</param>
        /// <returns>An instance of the YAML data.</returns>
        public object Deserialize([NotNull] Stream stream, [NotNull] object existingObject)
        {
            if (existingObject == null) throw new ArgumentNullException(nameof(existingObject));
            using (var textReader = new StreamReader(stream))
            {
                var serializer = GetYamlSerializer();
                return serializer.Deserialize(textReader, existingObject.GetType(), existingObject);
            }
        }

        /// <summary>
        /// Deserializes an object from the specified stream (expecting a YAML string).
        /// </summary>
        /// <param name="eventReader">A YAML event reader.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <returns>An instance of the YAML data.</returns>
        public object Deserialize(EventReader eventReader, Type expectedType)
        {
            var serializer = GetYamlSerializer();
            return serializer.Deserialize(eventReader, expectedType);
        }

        /// <summary>
        /// Serializes an object to specified stream in YAML format.
        /// </summary>
        /// <param name="emitter">The emitter.</param>
        /// <param name="instance">The object to serialize.</param>
        /// <param name="type">The type.</param>
        public void Serialize(IEmitter emitter, object instance, Type type)
        {
            var serializer = GetYamlSerializer();
            serializer.Serialize(emitter, instance, type);
        }

        /// <summary>
        /// Serializes an object to specified stream in YAML format.
        /// </summary>
        /// <param name="stream">The stream to receive the YAML representation of the object.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="generateIds"><c>true</c> to generate ~Id for class objects</param>
        public void Serialize(Stream stream, object instance, bool generateIds = true)
        {
            var serializer = GetYamlSerializer();
            serializer.Serialize(stream, instance);
        }

        /// <inheritdoc />
        protected override ISerializerFactorySelector CreateSelector()
        {
            return new ProfileSerializerFactorySelector(YamlSerializerFactoryAttribute.Default, "Settings");
        }
    }
}
