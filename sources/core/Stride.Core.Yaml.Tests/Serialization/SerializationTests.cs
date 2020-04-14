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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml.Tests.Serialization
{
    public class SerializationTests : YamlTest
    {
        [Fact]
        public void Roundtrip()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(SerializationTests).Assembly);
            var serializer = new Serializer(settings);

            var buffer = new StringWriter();
            var original = new X();
            serializer.Serialize(buffer, original);

            Dump.WriteLine(buffer);

            var bufferText = buffer.ToString();
            var copy = serializer.Deserialize<X>(bufferText);

            foreach (var property in typeof(X).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && property.CanWrite)
                {
                    Assert.Equal(
                        property.GetValue(original, null),
                        property.GetValue(copy, null));
                }
            }
        }

        [Fact]
        public void RoundtripWithDefaults()
        {
            var settings = new SerializerSettings() {EmitDefaultValues = true};
            settings.RegisterAssembly(typeof(SerializationTests).Assembly);
            var serializer = new Serializer(settings);

            var buffer = new StringWriter();
            var original = new X();
            serializer.Serialize(buffer, original);

            Dump.WriteLine(buffer);

            var bufferText = buffer.ToString();
            var copy = serializer.Deserialize<X>(bufferText);

            foreach (var property in typeof(X).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && property.CanWrite)
                {
                    Assert.Equal(
                        property.GetValue(original, null),
                        property.GetValue(copy, null));
                }
            }
        }

        [Fact]
        public void CircularReference()
        {
            var serializer = new Serializer();

            var buffer = new StringWriter();
            var original = new Y();
            original.Child = new Y
            {
                Child = original,
                Child2 = original
            };

            serializer.Serialize(buffer, original, typeof(Y));

            Dump.WriteLine(buffer);
        }

        private class Y
        {
            public Y Child { get; set; }
            public Y Child2 { get; set; }
        }

        [Fact]
        public void DeserializeScalar()
        {
            var sut = new Serializer();
            var result = sut.Deserialize(YamlFile("test2.yaml"), typeof(object));

            Assert.Equal("a scalar", result);
        }

        [Fact]
        public void DeserializeExplicitType()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(SerializationTests).Assembly);

            var serializer = new Serializer(settings);
            object result = serializer.Deserialize(YamlFile("explicitType.yaml"), typeof(object));

            Assert.True(typeof(Z).IsAssignableFrom(result.GetType()));
            Assert.Equal("bbb", ((Z) result).aaa);
        }

        [Fact]
        public void DeserializeDictionary()
        {
            var serializer = new Serializer();
            var result = serializer.Deserialize(YamlFile("dictionary.yaml"));

            Assert.True(typeof(IDictionary<object, object>).IsAssignableFrom(result.GetType()), "The deserialized object has the wrong type.");

            var dictionary = (IDictionary<object, object>) result;
            Assert.Equal("value1", dictionary["key1"]);
            Assert.Equal("value2", dictionary["key2"]);
        }

        [Fact]
        public void DeserializeExplicitDictionary()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(SerializationTests).Assembly);

            var serializer = new Serializer(settings);
            object result = serializer.Deserialize(YamlFile("dictionaryExplicit.yaml"));

            Assert.True(typeof(IDictionary<string, int>).IsAssignableFrom(result.GetType()), "The deserialized object has the wrong type.");

            var dictionary = (IDictionary<string, int>) result;
            Assert.Equal(1, dictionary["key1"]);
            Assert.Equal(2, dictionary["key2"]);
        }

        [Fact]
        public void DeserializeListOfDictionaries()
        {
            var serializer = new Serializer();
            var result = serializer.Deserialize(YamlFile("listOfDictionaries.yaml"), typeof(List<Dictionary<string, string>>));

            Assert.IsType<List<Dictionary<string, string>>>(result);

            var list = (List<Dictionary<string, string>>) result;
            Assert.Equal("conn1", list[0]["connection"]);
            Assert.Equal("path1", list[0]["path"]);
            Assert.Equal("conn2", list[1]["connection"]);
            Assert.Equal("path2", list[1]["path"]);
        }

        [Fact]
        public void DeserializeList()
        {
            var serializer = new Serializer();
            var result = serializer.Deserialize(YamlFile("list.yaml"));

            Assert.True(typeof(IList).IsAssignableFrom(result.GetType()));

            var list = (IList) result;
            Assert.Equal("one", list[0]);
            Assert.Equal("two", list[1]);
            Assert.Equal("three", list[2]);
        }

        [Fact]
        public void DeserializeExplicitList()
        {
            var serializer = new Serializer();
            var result = serializer.Deserialize(YamlFile("listExplicit.yaml"));

            Assert.True(typeof(IList<int>).IsAssignableFrom(result.GetType()));

            var list = (IList<int>) result;
            Assert.Equal(3, list[0]);
            Assert.Equal(4, list[1]);
            Assert.Equal(5, list[2]);
        }

        [Fact]
        public void DeserializeEnumerable()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(SerializationTests).Assembly);

            var serializer = new Serializer(settings);
            var buffer = new StringWriter();
            var z = new[] {new Z {aaa = "Yo"}};
            serializer.Serialize(buffer, z);

            var bufferAsText = buffer.ToString();
            var result = (IEnumerable<Z>) serializer.Deserialize(bufferAsText, typeof(IEnumerable<Z>));
            Assert.Single(result);
            Assert.Equal("Yo", result.First().aaa);
        }

        [Fact]
        public void RoundtripList()
        {
            var serializer = new Serializer();

            var buffer = new StringWriter();
            var original = new List<int> {2, 4, 6};
            serializer.Serialize(buffer, original, typeof(List<int>));

            Dump.WriteLine(buffer);

            var copy = (List<int>) serializer.Deserialize(new StringReader(buffer.ToString()), typeof(List<int>));

            Assert.Equal(original.Count, copy.Count);

            for (int i = 0; i < original.Count; ++i)
            {
                Assert.Equal(original[i], copy[i]);
            }
        }

        [Fact]
        public void DeserializeArray()
        {
            var serializer = new Serializer();
            var result = serializer.Deserialize(YamlFile("list.yaml"), typeof(String[]));

            Assert.True(result is String[]);

            var array = (String[]) result;
            Assert.Equal("one", array[0]);
            Assert.Equal("two", array[1]);
            Assert.Equal("three", array[2]);
        }

        [Fact]
        public void Enums()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(StringFormatFlags).Assembly);
            var serializer = new Serializer(settings);

            var flags = StringFormatFlags.NoClip | StringFormatFlags.NoFontFallback;

            var buffer = new StringWriter();
            serializer.Serialize(buffer, flags);

            var bufferAsText = buffer.ToString();
            var deserialized = (StringFormatFlags) serializer.Deserialize(bufferAsText, typeof(StringFormatFlags));

            Assert.Equal(flags, deserialized);
        }

        [Fact]
        public void CustomTags()
        {
            var settings = new SerializerSettings();
            settings.RegisterTagMapping("tag:yaml.org,2002:point", typeof(Point));
            var serializer = new Serializer(settings);
            var result = serializer.Deserialize(YamlFile("tags.yaml"));

            Assert.Equal(typeof(Point), result.GetType());

            var value = (Point) result;
            Assert.Equal(10, value.X);
            Assert.Equal(20, value.Y);
        }

        // Convertible are not supported
        //[Fact]
        //public void DeserializeConvertible()
        //{
        //	var settings = new SerializerSettings();
        //	settings.RegisterAssembly(typeof(SerializationTests).Assembly);
        //	settings.RegisterSerializerFactory(new TypeConverterSerializerFactory());

        //	var serializer = new Serializer(settings);
        //	var result = serializer.Deserialize(YamlFile("convertible.yaml"), typeof(Z));

        //	Assert.True(typeof(Z).IsAssignableFrom(result.GetType()));
        //	Assert.Equal("[hello, world]", ((Z)result).aaa);
        //}

        [Fact]
        public void RoundtripWithTypeConverter()
        {
            var buffer = new StringWriter();
            var x = new SomeCustomType("Yo");
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(SerializationTests).Assembly);
            settings.SerializerFactorySelector.TryAddFactory(new CustomTypeConverter());
            var serializer = new Serializer(settings);
            serializer.Serialize(buffer, x);

            Dump.WriteLine(buffer);

            var bufferText = buffer.ToString();
            var copy = serializer.Deserialize<SomeCustomType>(bufferText);
            Assert.Equal("Yo", copy.Value);
        }

        class SomeCustomType
        {
            // Test specifically with no parameterless, supposed to fail unless a type converter is specified
            public SomeCustomType(string value)
            {
                Value = value;
            }

            public string Value;
        }

        public class CustomTypeConverter : ScalarSerializerBase, IYamlSerializableFactory
        {
            public IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
            {
                return typeDescriptor.Type == typeof(SomeCustomType) ? this : null;
            }

            public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
            {
                return new SomeCustomType(fromScalar.Value);
            }

            public override string ConvertTo(ref ObjectContext objectContext)
            {
                return ((SomeCustomType) objectContext.Instance).Value;
            }
        }

        [Fact]
        public void RoundtripDictionary()
        {
            var entries = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"},
                {"key3", "value3"},
            };

            var buffer = new StringWriter();
            var serializer = new Serializer();
            serializer.Serialize(buffer, entries);

            Dump.WriteLine(buffer);

            var deserialized = serializer.Deserialize<Dictionary<string, string>>(new StringReader(buffer.ToString()));

            foreach (var pair in deserialized)
            {
                Assert.Equal(entries[pair.Key], pair.Value);
            }
        }

        [Fact]
        public void SerializeAnonymousType()
        {
            var data = new {Key = 3};

            var serializer = new Serializer();

            var buffer = new StringWriter();
            serializer.Serialize(buffer, data);

            Dump.WriteLine(buffer);

            var bufferText = buffer.ToString();
            var parsed = serializer.Deserialize<Dictionary<string, string>>(bufferText);

            Assert.NotNull(parsed);
            Assert.Single(parsed);
            Assert.True(parsed.ContainsKey("Key"));
            Assert.Equal("3", parsed["Key"]);
        }

        [Fact]
        public void SerializationIncludesDefaultValueWhenAsked()
        {
            var settings = new SerializerSettings() {EmitDefaultValues = true};
            settings.RegisterAssembly(typeof(X).Assembly);
            var serializer = new Serializer(settings);

            var buffer = new StringWriter();
            var original = new X();
            serializer.Serialize(buffer, original, typeof(X));

            Dump.WriteLine(buffer);
            var bufferText = buffer.ToString();
            Assert.Contains("MyString", bufferText);
        }

        [Fact]
        public void SerializationDoesNotIncludeDefaultValueWhenNotAsked()
        {
            var settings = new SerializerSettings() {EmitDefaultValues = false};
            settings.RegisterAssembly(typeof(X).Assembly);
            var serializer = new Serializer(settings);

            var buffer = new StringWriter();
            var original = new X();

            serializer.Serialize(buffer, original, typeof(X));

            Dump.WriteLine(buffer);
            var bufferText = buffer.ToString();
            Assert.DoesNotContain("MyString", bufferText);
        }

        [Fact]
        public void SerializationOfNullWorksInJson()
        {
            var settings = new SerializerSettings() {EmitDefaultValues = true, EmitJsonComptible = true};
            settings.RegisterAssembly(typeof(X).Assembly);
            var serializer = new Serializer(settings);

            var buffer = new StringWriter();
            var original = new X {MyString = null};
            serializer.Serialize(buffer, original, typeof(X));

            Dump.WriteLine(buffer);
            var bufferText = buffer.ToString();
            Assert.Contains("MyString", bufferText);
        }

        [Fact]
        public void DeserializationOfNullWorksInJson()
        {
            var settings = new SerializerSettings() {EmitDefaultValues = true, EmitJsonComptible = true};
            settings.RegisterAssembly(typeof(X).Assembly);
            var serializer = new Serializer(settings);

            var buffer = new StringWriter();
            var original = new X {MyString = null};
            serializer.Serialize(buffer, original, typeof(X));

            Dump.WriteLine(buffer);

            var bufferText = buffer.ToString();
            var copy = (X) serializer.Deserialize(bufferText, typeof(X));

            Assert.Null(copy.MyString);
        }

        [Fact]
        public void SerializationRespectsYamlIgnoreAttribute()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(ContainsIgnore).Assembly);
            var serializer = new Serializer(settings);

            var buffer = new StringWriter();
            var orig = new ContainsIgnore();
            serializer.Serialize(buffer, orig);

            Dump.WriteLine(buffer);

            var copy = (ContainsIgnore) serializer.Deserialize(new StringReader(buffer.ToString()), typeof(ContainsIgnore));

            Assert.Throws<NotImplementedException>(() =>
            {
                if (copy.IgnoreMe == null)
                {
                }
            });
        }

        class ContainsIgnore
        {
            [DataMemberIgnore]
            public String IgnoreMe { get { throw new NotImplementedException("Accessing a [YamlIgnore] property"); } set { throw new NotImplementedException("Accessing a [YamlIgnore] property"); } }
        }

        [Fact]
        public void SerializeArrayOfIdenticalObjects()
        {
            var obj1 = new Z {aaa = "abc"};

            var objects = new[] {obj1, obj1, obj1};

            var result = SerializeThenDeserialize(objects);

            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal(obj1.aaa, result[0].aaa);
            Assert.Equal(obj1.aaa, result[1].aaa);
            Assert.Equal(obj1.aaa, result[2].aaa);
            Assert.Equal(result[0], result[1]);
            Assert.Equal(result[1], result[2]);
        }

        private T SerializeThenDeserialize<T>(T input)
        {
            var serializer = new Serializer();
            var writer = new StringWriter();
            serializer.Serialize(writer, input, typeof(T));

            var serialized = writer.ToString();
            Dump.WriteLine("serialized =\n-----\n{0}", serialized);

            return serializer.Deserialize<T>(new StringReader(serialized));
        }

        public class Z
        {
            public string aaa { get; set; }
        }

        [Fact]
        public void RoundtripAlias()
        {
            var input = new ConventionTest {AliasTest = "Fourth"};
            var serializer = new Serializer();
            var writer = new StringWriter();
            serializer.Serialize(writer, input, input.GetType());
            var serialized = writer.ToString();

            // Ensure serialisation is correct
            Assert.Contains("fourthTest: Fourth", serialized);

            var output = serializer.Deserialize<ConventionTest>(serialized);

            // Ensure round-trip retains value
            Assert.Equal(input.AliasTest, output.AliasTest);
        }

        private class ConventionTest
        {
            [DefaultValue(null)]
            public string FirstTest { get; set; }

            [DefaultValue(null)]
            public string SecondTest { get; set; }

            [DefaultValue(null)]
            public string ThirdTest { get; set; }

            [DataMember("fourthTest")]
            public string AliasTest { get; set; }

            [DataMemberIgnore]
            public string fourthTest { get; set; }
        }

        [Fact]
        public void DefaultValueAttributeIsUsedWhenPresentWithoutEmitDefaults()
        {
            var input = new HasDefaults {Value = HasDefaults.DefaultValue};
            var serializer = new Serializer();
            var writer = new StringWriter();

            serializer.Serialize(writer, input);
            var serialized = writer.ToString();

            Dump.WriteLine(serialized);
            Assert.DoesNotContain("Value", serialized);
        }

        [Fact]
        public void DefaultValueAttributeIsIgnoredWhenPresentWithEmitDefaults()
        {
            var input = new HasDefaults {Value = HasDefaults.DefaultValue};
            var serializer = new Serializer(new SerializerSettings() {EmitDefaultValues = true});
            var writer = new StringWriter();

            serializer.Serialize(writer, input);
            var serialized = writer.ToString();

            Dump.WriteLine(serialized);
            Assert.Contains("Value", serialized);
        }

        [Fact]
        public void DefaultValueAttributeIsIgnoredWhenValueIsDifferent()
        {
            var input = new HasDefaults {Value = "non-default"};
            var serializer = new Serializer();
            var writer = new StringWriter();

            serializer.Serialize(writer, input);
            var serialized = writer.ToString();

            Dump.WriteLine(serialized);

            Assert.Contains("Value", serialized);
        }

        public class HasDefaults
        {
            public const string DefaultValue = "myDefault";

            [DefaultValue(DefaultValue)]
            public string Value { get; set; }
        }

        [Fact]
        public void NullValuesInListsAreAlwaysEmittedWithoutEmitDefaults()
        {
            var input = new[] {"foo", null, "bar"};
            var serializer = new Serializer(new SerializerSettings() {LimitPrimitiveFlowSequence = 0});
            var writer = new StringWriter();

            serializer.Serialize(writer, input);
            var serialized = writer.ToString();

            Dump.WriteLine(serialized);
            Assert.Equal(3, Regex.Matches(serialized, "-").Count);
        }

        [Fact]
        public void NullValuesInListsAreAlwaysEmittedWithEmitDefaults()
        {
            var input = new[] {"foo", null, "bar"};
            var serializer = new Serializer(new SerializerSettings() {EmitDefaultValues = true, LimitPrimitiveFlowSequence = 0});
            var writer = new StringWriter();

            serializer.Serialize(writer, input);
            var serialized = writer.ToString();

            Dump.WriteLine(serialized);
            Assert.Equal(3, Regex.Matches(serialized, "-").Count);
        }

        [Fact]
        public void DeserializeTwoDocuments()
        {
            var yaml = @"---
Name: Andy
---
Name: Brad
...";
            var serializer = new Serializer();
            var reader = new EventReader(new Parser(new StringReader(yaml)));

            reader.Expect<StreamStart>();

            var andy = serializer.Deserialize<Person>(reader);
            Assert.NotNull(andy);
            Assert.Equal("Andy", andy.Name);

            var brad = serializer.Deserialize<Person>(reader);
            Assert.NotNull(brad);
            Assert.Equal("Brad", brad.Name);
        }

        [Fact]
        public void DeserializeManyDocuments()
        {
            var yaml = @"---
Name: Andy
---
Name: Brad
---
Name: Charles
...";
            var serializer = new Serializer();
            var reader = new EventReader(new Parser(new StringReader(yaml)));

            reader.Allow<StreamStart>();

            var people = new List<Person>();
            while (!reader.Accept<StreamEnd>())
            {
                var person = serializer.Deserialize<Person>(reader);
                people.Add(person);
            }

            Assert.Equal(3, people.Count);
            Assert.Equal("Andy", people[0].Name);
            Assert.Equal("Brad", people[1].Name);
            Assert.Equal("Charles", people[2].Name);
        }

        public class Person
        {
            public string Name { get; set; }
        }

        public class ExtendedPerson
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Fact]
        public void DeserializeIntoExisting()
        {
            var serializer = new Serializer();
            var andy = new ExtendedPerson {Name = "Not Andy", Age = 30};
            var yaml = @"---
Name: Andy
...";
            andy = serializer.DeserializeInto<ExtendedPerson>(yaml, andy);
            Assert.Equal("Andy", andy.Name);
            Assert.Equal(30, andy.Age);

            andy = new ExtendedPerson {Name = "Not Andy", Age = 30};
            andy = (ExtendedPerson) serializer.Deserialize(yaml, typeof(ExtendedPerson), andy);
            Assert.Equal("Andy", andy.Name);
            Assert.Equal(30, andy.Age);
        }

        [Fact]
        public void DeserializeWithExistingObject()
        {
            var serializer = new Serializer();
            var andy = new ExtendedPerson {Name = "Not Andy", Age = 30};
            var yaml = @"---
Name: Andy
...";
            andy = new ExtendedPerson {Name = "Not Andy", Age = 30};
            andy = (ExtendedPerson) serializer.Deserialize(yaml, typeof(ExtendedPerson), andy);
            Assert.Equal("Andy", andy.Name);
            Assert.Equal(30, andy.Age);
        }

        public class Family
        {
            public ExtendedPerson Mother { get; set; }
            public ExtendedPerson Father { get; set; }
        }

        [Fact]
        public void DeserializeIntoExistingSubObjects()
        {
            var serializer = new Serializer();
            var andy = new ExtendedPerson {Name = "Not Andy", Age = 30};
            var amy = new ExtendedPerson {Name = "Amy", Age = 33};
            var family = new Family {Father = andy, Mother = amy};
            var yaml = @"---
Mother:  
  Name: Betty
  Age: 22

Father:
  Name: Andy
...";
            family = serializer.DeserializeInto<Family>(yaml, family);
            Assert.Equal("Andy", family.Father.Name);
            Assert.Equal("Betty", family.Mother.Name);
            // Existing behaviour will pass with the commented line
            //Assert.Equal(0, family.Father.Age);
            Assert.Equal(30, family.Father.Age);
            Assert.Equal(22, family.Mother.Age);
        }

        [Fact]
        public void DeserializeWithRepeatedSubObjects()
        {
            var serializer = new Serializer();
            var yaml = @"---
Mother:  
  Name: Betty
  
Mother:
  Age: 22
...";
            var family = serializer.Deserialize<Family>(yaml);
            Assert.Null(family.Father);
            // Note: This is the behaviour I would expect
            // Existing behaviour will pass with the commented line
            //Assert.Null(family.Mother.Name);
            Assert.Equal("Betty", family.Mother.Name);
            Assert.Equal(22, family.Mother.Age);
        }


        [Fact]
        public void DeserializeEmptyDocument()
        {
            var serializer = new Serializer();
            var array = (int[]) serializer.Deserialize(new StringReader(""), typeof(int[]));
            Assert.Null(array);
        }

        [Fact]
        public void SerializeGenericDictionaryShouldNotThrowTargetException()
        {
            var serializer = new Serializer();

            var buffer = new StringWriter();
            serializer.Serialize(buffer, new OnlyGenericDictionary
            {
                {"hello", "world"},
            });
        }

        private class OnlyGenericDictionary : IDictionary<string, string>
        {
            private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>();

            #region IDictionary<string,string> Members

            public void Add(string key, string value)
            {
                _dictionary.Add(key, value);
            }

            public bool ContainsKey(string key)
            {
                throw new NotImplementedException();
            }

            public ICollection<string> Keys { get { throw new NotImplementedException(); } }

            public bool Remove(string key)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out string value)
            {
                throw new NotImplementedException();
            }

            public ICollection<string> Values { get { throw new NotImplementedException(); } }

            public string this[string key] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

            #endregion

            #region ICollection<KeyValuePair<string,string>> Members

            public void Add(KeyValuePair<string, string> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, string> item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count { get { throw new NotImplementedException(); } }

            public bool IsReadOnly { get { throw new NotImplementedException(); } }

            public bool Remove(KeyValuePair<string, string> item)
            {
                throw new NotImplementedException();
            }

            #endregion

            #region IEnumerable<KeyValuePair<string,string>> Members

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }

            #endregion
        }

        [Fact]
        public void UndefinedForwardReferencesFail()
        {
            var serializer = new Serializer();

            Assert.Throws<AnchorNotFoundException>(() =>
                serializer.Deserialize<X>(YamlText(@"
					Nothing: *forward
					MyString: ForwardReference
				"))
                );
        }

        [Fact]
        public void DeserializeNullList()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(Z).Assembly);
            var sut = new Serializer(settings);
            const string yaml = @"!Stride.Core.Yaml.Tests.Serialization.SerializationTests+W
    MyList:
        - aaa
        - bbb
        - ccc
";
            var result = (W)sut.Deserialize(yaml, typeof(W));

            Assert.NotNull(result);
            Assert.NotNull(result.MyList);
            Assert.Equal(3, result.MyList.Count);
            Assert.Equal("aaa", result.MyList[0]);
            Assert.Equal("bbb", result.MyList[1]);
            Assert.Equal("ccc", result.MyList[2]);
        }

        private class X
        {
            [DefaultValue(false)]
            public bool MyFlag { get; set; }

            [DefaultValue(null)]
            public string Nothing { get; set; }

            [DefaultValue(1234)]
            public int MyInt { get; set; }

            [DefaultValue(6789.1011)]
            public double MyDouble { get; set; }

            [DefaultValue("Hello world")]
            public string MyString { get; set; }

            public DateTime MyDate { get; set; }
            public TimeSpan MyTimeSpan { get; set; }
            public Point MyPoint { get; set; }

            [DefaultValue(8)]
            public int? MyNullableWithValue { get; set; }

            [DefaultValue(null)]
            public int? MyNullableWithoutValue { get; set; }

            public X()
            {
                MyInt = 1234;
                MyDouble = 6789.1011;
                MyString = "Hello world";
                MyDate = DateTime.Now;
                MyTimeSpan = TimeSpan.FromHours(1);
                MyPoint = new Point(100, 200);
                MyNullableWithValue = 8;
            }
        }

        public class W
        {
            public List<string> MyList { get; set; }
        }
    }
}
