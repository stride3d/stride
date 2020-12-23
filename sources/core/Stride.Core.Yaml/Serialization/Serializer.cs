// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Serializes and deserializes objects into and from YAML documents.
    /// </summary>
    public sealed class Serializer
    {
        internal readonly IYamlSerializable ObjectSerializer;
        internal readonly RoutingSerializer RoutingSerializer;
        internal readonly ITypeDescriptorFactory TypeDescriptorFactory;

        private static readonly IYamlSerializableFactory[] DefaultFactories = new IYamlSerializableFactory[]
        {
            new PrimitiveSerializer(),
            new DictionarySerializer(),
            new CollectionSerializer(),
            new ArraySerializer(),
            new ObjectSerializer(),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Serializer"/> class.
        /// </summary>
        public Serializer() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Serializer"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public Serializer(SerializerSettings settings)
        {
            Settings = settings ?? new SerializerSettings();
            TypeDescriptorFactory = CreateTypeDescriptorFactory();
            RoutingSerializer routingSerializer;
            RegisterSerializerFactories();
            ObjectSerializer = CreateProcessor(out routingSerializer);
            RoutingSerializer = routingSerializer;
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public SerializerSettings Settings { get; }

        /// <summary>
        /// Serializes the specified object to a string.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <returns>A YAML string of the object.</returns>
        public string Serialize(object graph)
        {
            var stringWriter = new StringWriter();
            Serialize(stringWriter, graph);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Serializes the specified object to a string.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <returns>A YAML string of the object.</returns>
        public string Serialize(object graph, Type expectedType, SerializerContextSettings contextSettings = null)
        {
            var stringWriter = new StringWriter();
            Serialize(stringWriter, graph, expectedType, contextSettings);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="graph">The object to serialize.</param>
        public void Serialize(Stream stream, object graph)
        {
            Serialize(stream, graph, null);
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="contextSettings">The context settings.</param>
        public void Serialize(Stream stream, object graph, Type expectedType, SerializerContextSettings contextSettings = null)
        {
            var writer = new StreamWriter(stream);
            try
            {
                Serialize(writer, graph, expectedType, contextSettings);
            }
            finally
            {
                try
                {
                    writer.Flush();
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> where to serialize the object.</param>
        /// <param name="graph">The object to serialize.</param>
        public void Serialize(TextWriter writer, object graph)
        {
            Serialize(new Emitter(writer, Settings.PreferredIndent), graph);
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> where to serialize the object.</param>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="type">The static type of the object to serialize.</param>
        /// <param name="contextSettings">The context settings.</param>
        public void Serialize(TextWriter writer, object graph, Type type, SerializerContextSettings contextSettings = null)
        {
            Serialize(new Emitter(writer, Settings.PreferredIndent), graph, type, contextSettings);
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="emitter">The <see cref="IEmitter" /> where to serialize the object.</param>
        /// <param name="graph">The object to serialize.</param>
        public void Serialize(IEmitter emitter, object graph)
        {
            Serialize(emitter, graph, graph == null ? typeof(object) : null);
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="emitter">The <see cref="IEmitter" /> where to serialize the object.</param>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="type">The static type of the object to serialize.</param>
        /// <param name="contextSettings">The context settings.</param>
        public void Serialize(IEmitter emitter, object graph, Type type, SerializerContextSettings contextSettings = null)
        {
            if (emitter == null) throw new ArgumentNullException(nameof(emitter));

            if (graph == null && type == null) throw new ArgumentNullException(nameof(type));

            // Configure the emitter
            // TODO the current emitter is not enough configurable to format its output
            // This should be improved
            var defaultEmitter = emitter as Emitter;
            if (defaultEmitter != null)
            {
                defaultEmitter.ForceIndentLess = Settings.IndentLess;
            }

            var context = new SerializerContext(this, contextSettings) { Emitter = emitter, Writer = CreateEmitter(emitter) };

            // Serialize the document
            context.Writer.StreamStart();
            context.Writer.DocumentStart();
            var objectContext = new ObjectContext(context, graph, context.FindTypeDescriptor(type)) { Style = DataStyle.Any };
            context.Serializer.ObjectSerializer.WriteYaml(ref objectContext);
            context.Writer.DocumentEnd();
            context.Writer.StreamEnd();
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A deserialized object.</returns>
        public object Deserialize(Stream stream)
        {
            return Deserialize(stream, null);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>A deserialized object.</returns>
        public object Deserialize(TextReader reader)
        {
            return Deserialize(reader, null);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="Stream" /> with an expected specific type.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public object Deserialize(Stream stream, Type expectedType, SerializerContextSettings contextSettings = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return Deserialize(new StreamReader(stream), expectedType, null, contextSettings);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="Stream" /> with an expected specific type.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <param name="context">The context used to deserialize this object.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public object Deserialize(Stream stream, Type expectedType, SerializerContextSettings contextSettings, out SerializerContext context)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return Deserialize(new StreamReader(stream), expectedType, null, contextSettings, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="Stream" /> with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public T Deserialize<T>(Stream stream)
        {
            return (T) Deserialize(stream, typeof(T));
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="TextReader" /> with an expected specific type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="existingObject">The object to deserialize into. If null (the default) then a new object will be created</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public object Deserialize(TextReader reader, Type expectedType, object existingObject = null, SerializerContextSettings contextSettings = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            return Deserialize(new EventReader(new Parser(reader)), expectedType, existingObject, contextSettings);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="TextReader" /> with an expected specific type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="existingObject">The object to deserialize into. If null (the default) then a new object will be created</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <param name="context">The context used to deserialize this object.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public object Deserialize(TextReader reader, Type expectedType, object existingObject, SerializerContextSettings contextSettings, out SerializerContext context)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            return Deserialize(new EventReader(new Parser(reader)), expectedType, existingObject, contextSettings, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="TextReader" /> with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="existingObject">The object to deserialize into. If null (the default) then a new object will be created</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public T Deserialize<T>(TextReader reader, object existingObject = null)
        {
            return (T) Deserialize(reader, typeof(T), existingObject);
        }

        /// <summary>
        /// Deserializes an object from the specified string.
        /// </summary>
        /// <param name="fromText">The text.</param>
        /// <param name="existingObject">The object to deserialize into. If null (the default) then a new object will be created</param>
        /// <returns>A deserialized object.</returns>
        public object Deserialize(string fromText, object existingObject = null)
        {
            return Deserialize(fromText, null, existingObject);
        }

        /// <summary>
        /// Deserializes an object from the specified string. with an expected specific type.
        /// </summary>
        /// <param name="fromText">From text.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="existingObject">The object to deserialize into. If null (the default) then a new object will be created</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public object Deserialize(string fromText, Type expectedType, object existingObject = null)
        {
            if (fromText == null) throw new ArgumentNullException(nameof(fromText));
            return Deserialize(new StringReader(fromText), expectedType, existingObject);
        }

        /// <summary>
        /// Deserializes an object from the specified string. with an expected specific type.
        /// </summary>
        /// <param name="fromText">From text.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="existingObject">The object to deserialize into. If null (the default) then a new object will be created</param>
        /// <param name="context">The context used to deserialize this object.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public object Deserialize(string fromText, Type expectedType, object existingObject, out SerializerContext context)
        {
            if (fromText == null) throw new ArgumentNullException(nameof(fromText));
            return Deserialize(new StringReader(fromText), expectedType, existingObject, null, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified string. with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="fromText">From text.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public T Deserialize<T>(string fromText)
        {
            return (T) Deserialize(fromText, typeof(T));
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="EventReader" /> with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public T Deserialize<T>(EventReader reader)
        {
            return (T) Deserialize(reader, typeof(T));
        }

        /// <summary>
        /// Deserializes an object from the specified string. with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="fromText">From text.</param>
        /// <param name="existingObject">The object to deserialize into.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// 
        /// Note: These need a different name, because otherwise they will conflict with existing Deserialize(string,Type). They are new so the difference should not matter
        public T DeserializeInto<T>(string fromText, T existingObject)
        {
            return (T) Deserialize(fromText, typeof(T), existingObject);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="EventReader" /> with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="existingObject">The object to deserialize into.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public T DeserializeInto<T>(EventReader reader, T existingObject)
        {
            return (T) Deserialize(reader, typeof(T), existingObject);
        }

        /// <summary>
        /// Deserializes an object from the specified string. with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="fromText">From text.</param>
        /// <param name="context">The context.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public T Deserialize<T>(string fromText, out SerializerContext context)
        {
            return (T) Deserialize(fromText, typeof(T), null, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="EventReader" /> with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="context">The context used to deserialize this object.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public T Deserialize<T>(EventReader reader, out SerializerContext context)
        {
            return (T) Deserialize(reader, typeof(T), null, null, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified string. with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="fromText">From text.</param>
        /// <param name="existingObject">The object to deserialize into.</param>
        /// <param name="context">The context used to deserialize this object.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// Note: These need a different name, because otherwise they will conflict with existing Deserialize(string,Type). They are new so the difference should not matter
        public T DeserializeInto<T>(string fromText, T existingObject, out SerializerContext context)
        {
            return (T) Deserialize(fromText, typeof(T), existingObject, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="EventReader" /> with an expected specific type.
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="existingObject">The object to deserialize into.</param>
        /// <param name="context">The context used to deserialize this object.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public T DeserializeInto<T>(EventReader reader, T existingObject, out SerializerContext context)
        {
            return (T) Deserialize(reader, typeof(T), existingObject, null, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="EventReader" /> with an expected specific type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="expectedType">The expected type, maybe null.</param>
        /// <param name="existingObject">An existing object, may be null.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public object Deserialize(EventReader reader, Type expectedType, object existingObject = null, SerializerContextSettings contextSettings = null)
        {
            SerializerContext context;
            return Deserialize(reader, expectedType, existingObject, contextSettings, out context);
        }

        /// <summary>
        /// Deserializes an object from the specified <see cref="EventReader" /> with an expected specific type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="expectedType">The expected type, maybe null.</param>
        /// <param name="existingObject">An existing object, may be null.</param>
        /// <param name="contextSettings">The context settings.</param>
        /// <param name="context">The context used to deserialize the object.</param>
        /// <returns>A deserialized object.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public object Deserialize(EventReader reader, Type expectedType, object existingObject, SerializerContextSettings contextSettings, out SerializerContext context)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var hasStreamStart = reader.Allow<StreamStart>() != null;
            var hasDocumentStart = reader.Allow<DocumentStart>() != null;
            context = null;

            object result = null;
            if (!reader.Accept<DocumentEnd>() && !reader.Accept<StreamEnd>())
            {
                context = new SerializerContext(this, contextSettings) {Reader = reader};
                var node = context.Reader.Parser.Current;
                try
                {
                    var objectContext = new ObjectContext(context, existingObject, context.FindTypeDescriptor(expectedType));
                    result = context.Serializer.ObjectSerializer.ReadYaml(ref objectContext);
                }
                catch (YamlException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ex = ex.Unwrap();
                    throw new YamlException(node, ex);
                }
            }

            if (hasDocumentStart)
            {
                reader.Expect<DocumentEnd>();
            }

            if (hasStreamStart)
            {
                reader.Expect<StreamEnd>();
            }

            return result;
        }

        public IYamlSerializable GetSerializer(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return Settings.SerializerFactorySelector.GetSerializer(context, typeDescriptor);
        }

        private void RegisterSerializerFactories()
        {
            // Add registered factories
            foreach (var factory in Settings.AssemblyRegistry.SerializableFactories)
            {
                Settings.SerializerFactorySelector.TryAddFactory(factory);
            }

            // Add default factories
            foreach (var defaultFactory in DefaultFactories)
            {
                Settings.SerializerFactorySelector.TryAddFactory(defaultFactory);
            }
            Settings.SerializerFactorySelector.Seal();
        }

        private IYamlSerializable CreateProcessor(out RoutingSerializer routingSerializer)
        {
            routingSerializer = new RoutingSerializer(Settings.SerializerFactorySelector);

            var tagTypeSerializer = new TagTypeSerializer();
            routingSerializer.Prepend(tagTypeSerializer);
            if (Settings.EmitAlias)
            {
                var anchorSerializer = new AnchorSerializer();
                tagTypeSerializer.Prepend(anchorSerializer);
            }

            Settings.ChainedSerializerFactory?.Invoke(routingSerializer.First);
            return routingSerializer.First;
        }

        private ITypeDescriptorFactory CreateTypeDescriptorFactory()
        {
            return new TypeDescriptorFactory(Settings.Attributes, Settings.EmitDefaultValues, Settings.NamingConvention, Settings.ComparerForKeySorting);
        }

        private IEventEmitter CreateEmitter(IEmitter emitter)
        {
            var writer = (IEventEmitter) new WriterEventEmitter(emitter);

            if (Settings.EmitJsonComptible)
            {
                writer = new JsonEventEmitter(writer);
            }
            return Settings.EmitAlias ? new AnchorEventEmitter(writer) : writer;
        }
    }
}
