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

using Stride.Core.Reflection;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// The object context used when serializing/deserializing an object instance. See remarks.
    /// </summary>
    /// <remarks>
    /// <para>When serializing, this struct contains the <see cref="Instance"/> of the object to serialize, the type, the tag to use
    /// and the style, as well as providing access to the <see cref="SerializerContext"/>.</para>
    /// <para>When deserializing, this struct will contain the expected type to deserialize and if not null, the instance of an object
    /// that will recieve deserialization of its members (in case the instance cannot be created).</para>
    /// </remarks>
    public struct ObjectContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContext"/> struct.
        /// </summary>
        /// <param name="serializerContext">The serializer context.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="descriptor">The descriptor.</param>
        public ObjectContext(SerializerContext serializerContext, object instance, ITypeDescriptor descriptor,
            ITypeDescriptor parentTypeDescriptor = null, IMemberDescriptor parentTypeMemberDescriptor = null) : this()
        {
            SerializerContext = serializerContext;
            Instance = instance;
            Descriptor = descriptor;
            ParentTypeDescriptor = parentTypeDescriptor;
            ParentTypeMemberDescriptor = parentTypeMemberDescriptor;
            Properties = new PropertyContainer();
        }

        /// <summary>
        /// The serializer context associated to this object context.
        /// </summary>
        public readonly SerializerContext SerializerContext;

        /// <summary>
        /// Gets the current YAML reader. Equivalent to calling directly <see cref="Serialization.SerializerContext.Reader"/>.
        /// </summary>
        /// <value>The current YAML reader.</value>
        public EventReader Reader => SerializerContext.Reader;

        /// <summary>
        /// Gets the writer used while deserializing. Equivalent to calling directly <see cref="Serialization.SerializerContext.Writer"/>.
        /// </summary>
        /// <value>The writer.</value>
        public IEventEmitter Writer => SerializerContext.Writer;

        /// <summary>
        /// Gets the settings. Equivalent to calling directly <see cref="Serialization.SerializerContext.Settings"/>.
        /// </summary>
        /// <value>The settings.</value>
        public SerializerSettings Settings => SerializerContext.Settings;

        /// <summary>
        /// Gets the object serializer backend.
        /// </summary>
        /// <value>The object serializer backend.</value>
        public IObjectSerializerBackend ObjectSerializerBackend => SerializerContext.ObjectSerializerBackend;

        /// <summary>
        /// The instance link to this context.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The expected type descriptor.
        /// </summary>
        public ITypeDescriptor Descriptor { get; set; }

        /// <summary>
        /// The type descriptor of the parent of the instance type.
        /// </summary>
        public ITypeDescriptor ParentTypeDescriptor { get; set; }

        /// <summary>
        /// The type descriptor of the parent's member that generates this type of instance.
        /// </summary>
        public IMemberDescriptor ParentTypeMemberDescriptor { get; set; }

        /// <summary>
        /// The tag used when serializing.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The anchor used when serializing.
        /// </summary>
        public string Anchor { get; set; }

        /// <summary>
        /// The style used when serializing.
        /// </summary>
        public DataStyle Style { get; set; }

        /// <summary>
        /// The style used when serializing scalars.
        /// </summary>
        public ScalarStyle ScalarStyle { get; set; }

        /// <summary>
        /// The dictionary containing custom properties for this context.
        /// </summary>
        public PropertyContainer Properties;
    }
}
