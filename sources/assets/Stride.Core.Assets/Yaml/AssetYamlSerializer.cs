// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core.Assets.Serializers;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;
using SerializerContext = Stride.Core.Yaml.Serialization.SerializerContext;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Default Yaml serializer used to serialize assets by default.
    /// </summary>
    public class AssetYamlSerializer : YamlSerializerBase
    {
        private event Action<ObjectDescriptor, List<IMemberDescriptor>> PrepareMembersEvent;

        private Serializer serializer;

        public static AssetYamlSerializer Default { get; set; } = new AssetYamlSerializer();

        public event Action<ObjectDescriptor, List<IMemberDescriptor>> PrepareMembers
        {
            add
            {
                if (serializer != null)
                    throw new InvalidOperationException("Event handlers can't be added or removed after the serializer has been initialized.");

                PrepareMembersEvent += value;
            }
            remove
            {
                if (serializer != null)
                    throw new InvalidOperationException("Event handlers can't be added or removed after the serializer has been initialized.");
                PrepareMembersEvent -= value;
            }
        }

        /// <summary>
        /// Deserializes an object from the specified stream (expecting a YAML string).
        /// </summary>
        /// <param name="stream">A YAML string from a stream.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <returns>An instance of the YAML data.</returns>
        public object Deserialize(Stream stream, Type expectedType = null, SerializerContextSettings contextSettings = null)
        {
            bool aliasOccurred;
            PropertyContainer contextProperties;
            return Deserialize(stream, expectedType, contextSettings, out aliasOccurred, out contextProperties);
        }

        /// <summary>
        /// Deserializes an object from the specified stream (expecting a YAML string).
        /// </summary>
        /// <param name="stream">A YAML string from a stream .</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <param name="aliasOccurred">if set to <c>true</c> a class/field/property/enum name has been renamed during deserialization.</param>
        /// <param name="contextProperties">A dictionary or properties that were generated during deserialization.</param>
        /// <returns>An instance of the YAML data.</returns>
        public object Deserialize(Stream stream, Type expectedType, SerializerContextSettings contextSettings, out bool aliasOccurred, out PropertyContainer contextProperties)
        {
            EnsureYamlSerializer();
            SerializerContext context;
            var result = serializer.Deserialize(stream, expectedType, contextSettings, out context);
            aliasOccurred = context.HasRemapOccurred;
            contextProperties = context.Properties;
            return result;
        }

        /// <summary>
        /// Deserializes an object from the specified stream (expecting a YAML string).
        /// </summary>
        /// <param name="eventReader">A YAML event reader.</param>
        /// <param name="value">The value.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="contextProperties">A dictionary or properties that were generated during deserialization.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <returns>An instance of the YAML data.</returns>
        public object Deserialize(EventReader eventReader, object value, Type expectedType, out PropertyContainer contextProperties, SerializerContextSettings contextSettings = null)
        {
            EnsureYamlSerializer();
            SerializerContext context;
            var result = serializer.Deserialize(eventReader, expectedType, value, contextSettings, out context);
            contextProperties = context.Properties;
            return result;
        }

        /// <summary>
        /// Deserializes an object from the specified stream (expecting a YAML string).
        /// </summary>
        /// <param name="stream">A YAML string from a stream .</param>
        /// <returns>An instance of the YAML data.</returns>
        public IEnumerable<T> DeserializeMultiple<T>(Stream stream)
        {
            EnsureYamlSerializer();

            var input = new StreamReader(stream);
            var reader = new EventReader(new Parser(input));
            reader.Expect<StreamStart>();

            while (reader.Accept<DocumentStart>())
            {
                // Deserialize the document
                var doc = serializer.Deserialize<T>(reader);

                yield return doc;
            }
        }

        /// <summary>
        /// Serializes an object to specified stream in YAML format.
        /// </summary>
        /// <param name="emitter">The emitter.</param>
        /// <param name="instance">The object to serialize.</param>
        /// <param name="type">The type.</param>
        /// <param name="contextSettings">The context settings.</param>
        public void Serialize(IEmitter emitter, object instance, Type type, SerializerContextSettings contextSettings = null)
        {
            EnsureYamlSerializer();
            serializer.Serialize(emitter, instance, type, contextSettings);
        }

        /// <summary>
        /// Serializes an object to specified stream in YAML format.
        /// </summary>
        /// <param name="stream">The stream to receive the YAML representation of the object.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="type">The expected type.</param>
        /// <param name="contextSettings">The context settings.</param>
        public void Serialize(Stream stream, object instance, Type type = null, SerializerContextSettings contextSettings = null)
        {
            EnsureYamlSerializer();
            serializer.Serialize(stream, instance, type, contextSettings);
        }

        /// <summary>
        /// Gets the serializer settings.
        /// </summary>
        /// <returns>SerializerSettings.</returns>
        public SerializerSettings GetSerializerSettings()
        {
            EnsureYamlSerializer();
            return serializer.Settings;
        }

        /// <summary>
        /// Reset the assembly cache used by this class.
        /// </summary>
        public override void ResetCache()
        {
            lock (Lock)
            {
                // Reset the current serializer as the set of assemblies has changed
                serializer = null;
            }
        }

        private void EnsureYamlSerializer()
        {
            lock (Lock)
            {
                if (serializer == null)
                {
                    // var clock = Stopwatch.StartNew();

                    var config = new SerializerSettings
                    {
                        EmitAlias = false,
                        LimitPrimitiveFlowSequence = 0,
                        Attributes = new AttributeRegistry(),
                        PreferredIndent = 4,
                        EmitShortTypeName = true,
                        ComparerForKeySorting = new DefaultMemberComparer(),
                        PreSerializer = new ContextAttributeSerializer(),
                        PostSerializer = new ErrorRecoverySerializer(),
                        SerializerFactorySelector = new ProfileSerializerFactorySelector(YamlSerializerFactoryAttribute.Default, "Assets"),
                        ChainedSerializerFactory = x =>
                        {
                            var routingSerializer = x.FindNext<RoutingSerializer>();
                            if (routingSerializer == null)
                                throw new InvalidOperationException("RoutingSerializer expected in the chain of serializers");
                            // Prepend the IdentifiableObjectSerializer just before the routing serializer
                            routingSerializer.Prepend(new IdentifiableObjectSerializer());
                            // Prepend the ContextAttributeSerializer just before the routing serializer
                            routingSerializer.Prepend(new ContextAttributeSerializer());
                            // Prepend the ErrorRecoverySerializer at the beginning
                            routingSerializer.First.Prepend(new ErrorRecoverySerializer());
                        }
                    };

                    config.Attributes.PrepareMembersCallback += (objDesc, members) => PrepareMembersEvent?.Invoke(objDesc, members);

                    for (var index = RegisteredAssemblies.Count - 1; index >= 0; index--)
                    {
                        var registeredAssembly = RegisteredAssemblies[index];
                        config.RegisterAssembly(registeredAssembly);
                    }

                    var newSerializer = new Serializer(config);
                    newSerializer.Settings.ObjectSerializerBackend = new AssetObjectSerializerBackend(TypeDescriptorFactory.Default);

                    // Log.Info("New YAML serializer created in {0}ms", clock.ElapsedMilliseconds);
                    serializer = newSerializer;
                }
            }
        }
    }
}
