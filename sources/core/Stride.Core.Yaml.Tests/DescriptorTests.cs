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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Xunit;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml.Tests
{
    public class DescriptorTests
    {
        public class TestObject
        {
            // unused, not public
            internal string InternalName { get; set; }

            public TestObject()
            {
                Collection = new List<string>();
                CollectionReadOnly = new ReadOnlyCollection<string>(new List<string>());
                DefaultValue = 5;
            }

            public object Value { get; set; }

            public string Name;

            public string Property { get; set; }

            public ICollection<string> Collection { get; set; }

            public ICollection<string> CollectionReadOnly { get; private set; }

            [DataMemberIgnore]
            public string DontSerialize { get; set; }

            [DataMember("Item1")]
            public string ItemRenamed1 { get; set; }

            // This property is renamed to Item2 by an external attribute
            public int ItemRenamed2 { get; set; }

            [DefaultValue(5)]
            public int DefaultValue { get; set; }

            public bool ShouldSerializeValue()
            {
                return Value != null;
            }
        }

        [Fact]
        public void TestObjectDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var factory = new TypeDescriptorFactory(attributeRegistry);

            // Rename ItemRenamed2 to Item2
            attributeRegistry.Register(typeof(TestObject).GetProperty("ItemRenamed2"), new DataMemberAttribute("Item2"));

            var descriptor = new ObjectDescriptor(factory, typeof(TestObject), false, new DefaultNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());

            // Verify members
            Assert.Equal(8, descriptor.Count);

            // Check names and their orders
            Assert.Equal(descriptor.Members.Select(memberDescriptor => memberDescriptor.Name), new[]
            {
                "Collection",
                "CollectionReadOnly",
                "DefaultValue",
                "Item1",
                "Item2",
                "Name",
                "Property",
                "Value"
            });

            var instance = new TestObject {Name = "Yes", Property = "property"};

            // Check field accessor
            Assert.Equal("Yes", descriptor[nameof(TestObject.Name)].Get(instance));
            descriptor[nameof(TestObject.Name)].Set(instance, "No");
            Assert.Equal("No", instance.Name);

            // Check property accessor
            Assert.Equal("property", descriptor[nameof(TestObject.Property)].Get(instance));
            descriptor[nameof(TestObject.Property)].Set(instance, "property1");
            Assert.Equal("property1", instance.Property);

            // Check ShouldSerialize
            Assert.True(descriptor[nameof(TestObject.Name)].ShouldSerialize(instance, null));

            Assert.False(descriptor[nameof(TestObject.Value)].ShouldSerialize(instance, null));
            instance.Value = 1;
            Assert.True(descriptor[nameof(TestObject.Value)].ShouldSerialize(instance, null));

            Assert.False(descriptor[nameof(TestObject.DefaultValue)].ShouldSerialize(instance, null));
            instance.DefaultValue++;
            Assert.True(descriptor[nameof(TestObject.DefaultValue)].ShouldSerialize(instance, null));

            // Check HasSet
            Assert.True(descriptor[nameof(TestObject.Collection)].HasSet);
            Assert.False(descriptor[nameof(TestObject.CollectionReadOnly)].HasSet);
        }

        public class TestObjectNamingConvention
        {
            public string Name { get; set; }

            public string ThisIsCamelName { get; set; }

            [DataMember("myname")]
            public string CustomName { get; set; }
        }

        [Fact]
        public void TestObjectWithCustomNamingConvention()
        {
            var attributeRegistry = new AttributeRegistry();
            var factory = new TypeDescriptorFactory(attributeRegistry);
            var descriptor = new ObjectDescriptor(factory, typeof(TestObjectNamingConvention), false, new FlatNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());

            // Check names and their orders
            Assert.Equal(descriptor.Members.Select(memberDescriptor => memberDescriptor.Name), new[]
            {
                "myname",
                "name",
                "this_is_camel_name"
            });
        }

        /// <summary>
        /// This is a non pure collection: It has at least one public get/set member.
        /// </summary>
        public class NonPureCollection : List<int>
        {
            public string Name { get; set; }
        }

        [Fact]
        public void TestCollectionDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var factory = new TypeDescriptorFactory(attributeRegistry);
            var descriptor = new CollectionDescriptor(factory, typeof(List<string>), false, new DefaultNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());

            // No Capacity as a member
            Assert.Equal(0, descriptor.Count);
            Assert.True(descriptor.IsPureCollection);
            Assert.Equal(typeof(string), descriptor.ElementType);

            descriptor = new CollectionDescriptor(factory, typeof(NonPureCollection), false,
                new DefaultNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());

            // Has name as a member
            Assert.Equal(1, descriptor.Count);
            Assert.False(descriptor.IsPureCollection);
            Assert.Equal(typeof(int), descriptor.ElementType);

            descriptor = new CollectionDescriptor(factory, typeof(ArrayList), false, new DefaultNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());

            // No Capacity
            Assert.Equal(0, descriptor.Count);
            Assert.True(descriptor.IsPureCollection);
            Assert.Equal(typeof(object), descriptor.ElementType);
        }

        /// <summary>
        /// This is a non pure collection: It has at least one public get/set member.
        /// </summary>
        public class NonPureDictionary : Dictionary<float, object>
        {
            public string Name { get; set; }
        }

        [Fact]
        public void TestDictionaryDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var factory = new TypeDescriptorFactory(attributeRegistry);
            var descriptor = new DictionaryDescriptor(factory, typeof(Dictionary<int, string>), false,
                new DefaultNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());

            Assert.Equal(0, descriptor.Count);
            Assert.True(descriptor.IsPureDictionary);
            Assert.Equal(typeof(int), descriptor.KeyType);
            Assert.Equal(typeof(string), descriptor.ValueType);

            descriptor = new DictionaryDescriptor(factory, typeof(NonPureDictionary), false,
                new DefaultNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());
            Assert.Equal(1, descriptor.Count);
            Assert.False(descriptor.IsPureDictionary);
            Assert.Equal(typeof(float), descriptor.KeyType);
            Assert.Equal(typeof(object), descriptor.ValueType);
        }

        [Fact]
        public void TestArrayDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var factory = new TypeDescriptorFactory(attributeRegistry);
            var descriptor = new ArrayDescriptor(factory, typeof(int[]), false, new DefaultNamingConvention());
            descriptor.Initialize(new DefaultKeyComparer());

            Assert.Equal(0, descriptor.Count);
            Assert.Equal(typeof(int), descriptor.ElementType);
        }

        public enum MyEnum
        {
            A,
            B
        }

        [Fact]
        public void TestPrimitiveDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var factory = new TypeDescriptorFactory(attributeRegistry);
            var descriptor = new PrimitiveDescriptor(factory, typeof(int), false, new DefaultNamingConvention());
            Assert.Equal(0, descriptor.Count);

            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(MyEnum)));
            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(object)));
            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(DateTime)));
            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(TimeSpan)));
            Assert.False(PrimitiveDescriptor.IsPrimitive(typeof(IList)));
        }
    }
}
