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
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Schemas;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// A context used while deserializing.
    /// </summary>
    public class SerializerContext : ITagTypeResolver
    {
        internal int AnchorCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerContext"/> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializerContextSettings">The serializer context settings.</param>
        internal SerializerContext(Serializer serializer, SerializerContextSettings serializerContextSettings)
        {
            Serializer = serializer;
            ObjectFactory = serializer.Settings.ObjectFactory;
            ObjectSerializerBackend = serializer.Settings.ObjectSerializerBackend;
            var contextSettings = serializerContextSettings ?? SerializerContextSettings.Default;
            Logger = contextSettings.Logger;
            MemberMask = contextSettings.MemberMask;
            Properties = contextSettings.Properties;
        }

        /// <summary>
        /// Gets a value indicating whether we are in the context of serializing.
        /// </summary>
        /// <value><c>true</c> if we are in the context of serializing; otherwise, <c>false</c>.</value>
        public bool IsSerializing => Writer != null;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public SerializerSettings Settings => Serializer.Settings;

        /// <summary>
        /// Gets the schema.
        /// </summary>
        /// <value>The schema.</value>
        public IYamlSchema Schema => Settings.Schema;

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <value>The serializer.</value>
        public Serializer Serializer { get; }

        /// <summary>
        /// Gets or sets the reader used while deserializing.
        /// </summary>
        /// <value>The reader.</value>
        public EventReader Reader { get; set; }

        /// <summary>
        /// Gets the object serializer backend.
        /// </summary>
        /// <value>The object serializer backend.</value>
        public IObjectSerializerBackend ObjectSerializerBackend { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether errors are allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if errors are allowed; otherwise, <c>false</c>.
        /// </value>
        public bool AllowErrors { get; set; }

        /// <summary>
        /// Gets a value indicating whether the deserialization has generated some remap.
        /// </summary>
        /// <value><c>true</c> if the deserialization has generated some remap; otherwise, <c>false</c>.</value>
        public bool HasRemapOccurred { get; internal set; }

        /// <summary>
        /// Gets or sets the member mask that will be used to filter <see cref="DataMemberAttribute.Mask"/>.
        /// </summary>
        /// <value>
        /// The member mask.
        /// </value>
        public uint MemberMask { get; }

        /// <summary>
        /// Gets the dictionary of custom properties associated to this context.
        /// </summary>
        public PropertyContainer Properties;

        /// <summary>
        /// Gets or sets the type of the create.
        /// </summary>
        /// <value>The type of the create.</value>
        public IObjectFactory ObjectFactory { get; set; }

        /// <summary>
        /// Gets or sets the writer used while deserializing.
        /// </summary>
        /// <value>The writer.</value>
        public IEventEmitter Writer { get; set; }

        /// <summary>
        /// Gets the emitter.
        /// </summary>
        /// <value>The emitter.</value>
        public IEmitter Emitter { get; internal set; }

        /// <summary>
        /// Finds the type descriptor for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>An instance of <see cref="ITypeDescriptor"/>.</returns>
        public ITypeDescriptor FindTypeDescriptor(Type type)
        {
            return Serializer.TypeDescriptorFactory.Find(type);
        }

        /// <summary>
        /// Resolves a type from the specified tag.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="isAlias">True if tag is an alias.</param>
        /// <returns>Type.</returns>
        public Type TypeFromTag(string tagName, out bool isAlias)
        {
            return Serializer.Settings.AssemblyRegistry.TypeFromTag(tagName, out isAlias);
        }

        /// <summary>
        /// Resolves a tag from a type
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The associated tag</returns>
        public string TagFromType(Type type)
        {
            return Serializer.Settings.AssemblyRegistry.TagFromType(type);
        }

        /// <summary>
        /// Resolves a type from the specified typename using registered assemblies.
        /// </summary>
        /// <param name="typeFullName">Full name of the type.</param>
        /// <returns>The type of null if not found</returns>
        public Type ResolveType(string typeFullName)
        {
            return Serializer.Settings.AssemblyRegistry.ResolveType(typeFullName);
        }

        /// <summary>
        /// Resolves a type and assembly from the full name.
        /// </summary>
        /// <param name="typeFullName">Full name of the type.</param>
        public void ParseType(string typeFullName, out string typeName, out string assemblyName)
        {
            Serializer.Settings.AssemblyRegistry.ParseType(typeFullName, out typeName, out assemblyName);
        }

        /// <summary>
        /// Gets the default tag and value for the specified <see cref="Scalar" />. The default tag can be different from a actual tag of this <see cref="NodeEvent" />.
        /// </summary>
        /// <param name="scalar">The scalar event.</param>
        /// <param name="defaultTag">The default tag decoded from the scalar.</param>
        /// <param name="value">The value extracted from a scalar.</param>
        /// <returns>System.String.</returns>
        public bool TryParseScalar(Scalar scalar, out string defaultTag, out object value)
        {
            return Settings.Schema.TryParse(scalar, true, out defaultTag, out value);
        }
    }
}
