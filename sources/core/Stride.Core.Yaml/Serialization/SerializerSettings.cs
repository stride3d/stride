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
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Schemas;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Settings used to configure serialization and control how objects are encoded into YAML.
    /// </summary>
    public sealed class SerializerSettings
    {
        internal readonly Dictionary<Type, IYamlSerializable> serializers = new Dictionary<Type, IYamlSerializable>();
        internal readonly YamlAssemblyRegistry AssemblyRegistry;
        private IAttributeRegistry attributeRegistry;
        private readonly IYamlSchema schema;
        private IObjectFactory objectFactory;
        private int preferredIndent;
        private string specialCollectionMember;
        private IObjectSerializerBackend objectSerializerBackend;
        private IMemberNamingConvention _namingConvention;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerSettings"/> class.
        /// </summary>
        public SerializerSettings() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerSettings" /> class.
        /// </summary>
        public SerializerSettings(IYamlSchema schema)
        {
            PreferredIndent = 2;
            IndentLess = false;
            EmitAlias = true;
            EmitTags = true;
            SortKeyForMapping = true;
            EmitJsonComptible = false;
            EmitCapacityForList = false;
            SpecialCollectionMember = "~Items";
            LimitPrimitiveFlowSequence = 0;
            DefaultStyle = DataStyle.Normal;
            this.schema = schema ?? new CoreSchema();
            AssemblyRegistry = new YamlAssemblyRegistry(Schema);
            attributeRegistry = new AttributeRegistry();
            ObjectFactory = new DefaultObjectFactory();
            ObjectSerializerBackend = new DefaultObjectSerializerBackend();
            ComparerForKeySorting = new DefaultMemberComparer();
            NamingConvention = new DefaultNamingConvention();
            SerializerFactorySelector = new ProfileSerializerFactorySelector(YamlSerializerFactoryAttribute.Default);
            // Register default mapping for map and seq
            AssemblyRegistry.RegisterTagMapping("!!map", typeof(IDictionary<object, object>), false);
            AssemblyRegistry.RegisterTagMapping("!!seq", typeof(IList<object>), false);
        }

        /// <summary>
        /// Gets or sets a serializer that is executed just before the <see cref="RoutingSerializer"/>.
        /// </summary>
        public ChainedSerializer PreSerializer { get; set; }

        /// <summary>
        /// Gets or sets a serializer that is executed after all other serializers, including <see cref="TagTypeSerializer"/>.
        /// </summary>
        public ChainedSerializer PostSerializer { get; set; }

        /// <summary>
        /// Gets or sets the preferred indentation. Default is 2.
        /// </summary>
        /// <value>The preferred indentation.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">value;Expecting value &gt; 0</exception>
        public int PreferredIndent
        {
            get { return preferredIndent; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Expecting value > 0");
                preferredIndent = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to emit anchor alias. Default is true.
        /// </summary>
        /// <value><c>true</c> to emit anchor alias; otherwise, <c>false</c>.</value>
        public bool EmitAlias { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to emit tags when serializing. Default is true.
        /// </summary>
        /// <value><c>true</c> to emit tags when serializing; otherwise, <c>false</c>.</value>
        public bool EmitTags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the identation is trying to less
        /// indent when possible
        /// (For example, sequence after a key are not indented). Default is false.
        /// </summary>
        /// <value><c>true</c> if [always indent]; otherwise, <c>false</c>.</value>
        public bool IndentLess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable sorting keys from dictionary to YAML mapping. Default is true. See remarks.
        /// </summary>
        /// <value><c>true</c> to enable sorting keys from dictionary to YAML mapping; otherwise, <c>false</c>.</value>
        /// <remarks>When storing a YAML document, It can be important to keep the same order for key mapping in order to keep
        /// a YAML document versionable/diffable.</remarks>
        public bool SortKeyForMapping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to emit JSON compatible YAML.
        /// </summary>
        /// <value><c>true</c> if to emit JSON compatible YAML; otherwise, <c>false</c>.</value>
        public bool EmitJsonComptible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the property <see cref="List{T}.Capacity" /> should be emitted. Default is false.
        /// </summary>
        /// <value><c>true</c> if the property <see cref="List{T}.Capacity" /> should be emitted; otherwise, <c>false</c>.</value>
        public bool EmitCapacityForList { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of elements an array/list of primitive can be emitted as a
        /// flow sequence (instead of a block sequence by default). Default is 0, meaning block style
        /// for all sequuences.
        /// </summary>
        /// <value>The emit compact array limit.</value>
        public int LimitPrimitiveFlowSequence { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to emit default value. Default is false.
        /// </summary>
        /// <value><c>true</c> if to emit default value; otherwise, <c>false</c>.</value>
        public bool EmitDefaultValues { get; set; }

        /// <summary>
        /// Gets or sets the default key comparer used to sort members (<see cref="IMemberDescriptor"/>) or
        /// dictionary keys, when serializing objects as YAML mappings. Default is <see cref="DefaultMemberComparer"/>. 
        /// To disable the default comparer, this value can be set to null.
        /// </summary>
        /// <value>The key comparer.</value>
        public IComparer<object> ComparerForKeySorting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to serialize dictionary items as regular members.s
        /// </summary>
        /// <value><c>true</c> if [enable dictionary items as members]; otherwise, <c>false</c>.</value>
        public bool SerializeDictionaryItemsAsMembers { get; set; }

        /// <summary>
        /// Gets or sets the naming convention. Default is to output name as-is <see cref="DefaultNamingConvention"/>.
        /// </summary>
        /// <value>The naming convention.</value>
        public IMemberNamingConvention NamingConvention
        {
            get { return _namingConvention; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _namingConvention = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to emit short type name (type, assembly name) or full <see cref="Type.AssemblyQualifiedName"/>. Default is false.
        /// </summary>
        /// <value><c>true</c> to emit short type name; otherwise, <c>false</c>.</value>
        public bool EmitShortTypeName { get { return AssemblyRegistry.UseShortTypeName; } set { AssemblyRegistry.UseShortTypeName = value; } }

        /// <summary>
        /// Gets the tag type registry.
        /// </summary>
        /// <value>The tag type registry.</value>
        public ITagTypeRegistry TagTypeRegistry { get { return AssemblyRegistry; } }

        /// <summary>
        /// Gets or sets the default <see cref="DataStyle"/>. Default is <see cref="DataStyle.Normal"/>. See <see cref="DynamicStyleFormat"/> to understand the resolution of styles.
        /// </summary>
        /// <value>The default style.</value>
        public DataStyle DefaultStyle { get; set; }

        /// <summary>
        /// Gets or sets the prefix used to serialize items for a non pure <see cref="System.Collections.IDictionary" /> or
        /// <see cref="System.Collections.ICollection" />
        /// . Default to "~Items", see remarks.
        /// </summary>
        /// <value>The prefix for items.</value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        /// <exception cref="System.ArgumentException">Expecting length >= 2 and at least a special character '.', '~', '-' (not starting on first char for '-')</exception>
        /// <remarks>A pure <see cref="System.Collections.IDictionary" /> or <see cref="System.Collections.ICollection" /> is a class that inherits from these types but are not adding any
        /// public properties or fields. When these types are pure, they are respectively serialized as a YAML mapping (for dictionary) or a YAML sequence (for collections).
        /// If the collection type to serialize is not pure, the type is serialized as a YAML mapping sequence that contains the public properties/fields as well as a
        /// special fielx (e.g. "~Items") that contains the actual items of the collection (either a mapping for dictionary or a sequence for collections).
        /// The <see cref="SpecialCollectionMember" /> is this special key that is used when serializing items of a non-pure collection.</remarks>
        public string SpecialCollectionMember
        {
            get { return specialCollectionMember; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                // TODO this is a poor check. Need to verify this against the specs
                if (value.Length < 2 || !(value.Contains(".") || value.Contains("~") || value.IndexOf('-') > 0))
                {
                    throw new ArgumentException(
                        "Expecting length >= 2 and at least a special character '.', '~', '-' (not starting on first char for '-')");
                }

                specialCollectionMember = value;
            }
        }

        /// <summary>
        /// Gets the attribute registry.
        /// </summary>
        /// <value>The attribute registry.</value>
        public IAttributeRegistry Attributes
        {
            get { return attributeRegistry; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                attributeRegistry = value;
            }
        }

        /// <summary>
        /// Gets or sets the ObjectSerializerBackend. Default implementation is <see cref="DefaultObjectSerializerBackend"/>
        /// </summary>
        /// <value>The ObjectSerializerBackend.</value>
        public IObjectSerializerBackend ObjectSerializerBackend
        {
            get { return objectSerializerBackend; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                objectSerializerBackend = value;
            }
        }

        /// <summary>
        /// Gets or sets the default factory to instantiate a type. Default is <see cref="DefaultObjectFactory" />.
        /// </summary>
        /// <value>The default factory to instantiate a type.</value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public IObjectFactory ObjectFactory
        {
            get { return objectFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                objectFactory = value;
            }
        }

        /// <summary>
        /// Gets or sets the schema. Default is <see cref="CoreSchema" />.
        /// method.
        /// </summary>
        /// <value>The schema.</value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public IYamlSchema Schema { get { return schema; } }

        public ISerializerFactorySelector SerializerFactorySelector { get; set; }

        /// <summary>
        /// Gets a methods that will build the proper chain of serializers out of the default chain.
        /// </summary>
        public Action<ChainedSerializer> ChainedSerializerFactory { get; set; }

        /// <summary>
        /// Register a mapping between a tag and a type.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void RegisterAssembly(Assembly assembly)
        {
            AssemblyRegistry.RegisterAssembly(assembly, attributeRegistry);
        }

        /// <summary>
        /// Register a mapping between a tag and a type.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="tagType">Type of the tag.</param>
        /// <param name="isAlias">if set to <c>true</c> the new tag name is an alias to a type that has already a tag name associated to it.</param>
        public void RegisterTagMapping(string tagName, Type tagType, bool isAlias = false)
        {
            AssemblyRegistry.RegisterTagMapping(tagName, tagType, isAlias);
        }
    }
}
