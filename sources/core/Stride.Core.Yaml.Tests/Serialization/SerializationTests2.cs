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
using System.IO;
using System.Linq;
using Xunit;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml.Tests.Serialization
{
    public class SerializationTests2
    {
        public enum MyEnum
        {
            A,
            B,
        }

        [Flags]
        public enum MyEnumWithFlags
        {
            A = 1,
            B = 2,
        }

        [Fact]
        public void TestHelloWorld()
        {
            var serializer = new Serializer();
            var text = serializer.Serialize(new {List = new List<int>() {1, 2, 3}, Name = "Hello", Value = "World!"});
            Console.WriteLine(text);
            Assert.Equal(@"List:
  - 1
  - 2
  - 3
Name: Hello
Value: World!
", text);
        }

        public struct Color
        {
            public byte R;

            public byte G;

            public byte B;

            public byte A;
        }

        public class TestStructColor
        {
            public Color Color { get; set; }
        }


        public struct StructWithDefaultValue
        {
            public StructWithDefaultValue(int width, int height) : this()
            {
                Width = width;
                Height = height;
            }

            public static StructWithDefaultValue Default { get { return new StructWithDefaultValue(100, 50); } }

            [DefaultValue(100)]
            public int Width { get; set; }

            [DefaultValue(50)]
            public int Height { get; set; }
        }

        public class TestStructWithDefaultValues
        {
            public TestStructWithDefaultValues()
            {
                Test = StructWithDefaultValue.Default;
            }

            public StructWithDefaultValue Test { get; set; }
        }


        [Fact]
        public void TestSimpleStruct()
        {
            var serializer = new Serializer();
            var value = (TestStructColor) serializer.Deserialize(@"Color: {R: 255, G: 255, B: 255, A: 255}", typeof(TestStructColor));
            Assert.Equal(new Color() {R = 255, G = 255, B = 255, A = 255}, value.Color);
            var text = serializer.Serialize(value, typeof(TestStructColor));
            Assert.Equal(@"Color:
  R: 255
  G: 255
  B: 255
  A: 255
", text);
        }

        [Fact]
        public void TestSimpleStructWithDefaultValues()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(SerializationTests2).Assembly);
            var serializer = new Serializer(settings);

            var value = new TestStructWithDefaultValues();
            var text = serializer.Serialize(value);
            var newValue = serializer.Deserialize<TestStructWithDefaultValues>(text);
            Assert.Equal(value.Test.Width, newValue.Test.Width);
            Assert.Equal(value.Test.Height, newValue.Test.Height);
        }

        [Fact]
        public void TestSimpleStructMemberOrdering()
        {
            var settings = new SerializerSettings() {ComparerForKeySorting = null};
            var serializer = new Serializer(settings);
            var value = new TestStructColor() {Color = new Color() {R = 255, G = 255, B = 255, A = 255}};
            var text = serializer.Serialize(value, typeof(TestStructColor));
            Assert.Equal(@"Color:
  R: 255
  G: 255
  B: 255
  A: 255
", text);
        }

        public class MyObject
        {
            public MyObject()
            {
                ArrayContent = new int[2];
                EnumWithFlags = MyEnumWithFlags.A | MyEnumWithFlags.B;
            }

            public string String { get; set; }

            public sbyte SByte { get; set; }

            public byte Byte { get; set; }

            public short Int16 { get; set; }

            public ushort UInt16 { get; set; }

            public int Int32 { get; set; }

            public uint UInt32 { get; set; }

            public long Int64 { get; set; }

            public ulong UInt64 { get; set; }

            public decimal Decimal { get; set; }

            public float Float { get; set; }

            public double Double { get; set; }

            public MyEnum Enum { get; set; }

            public MyEnumWithFlags EnumWithFlags { get; set; }

            public bool Bool { get; set; }

            public bool BoolFalse { get; set; }

            public string A0Anchor { get; set; }

            public string A1Alias { get; set; }

            public int[] Array { get; set; }

            public int[] ArrayContent { get; private set; }
        }

        [Fact]
        public void TestSimpleObjectAndPrimitive()
        {
            var text = @"!MyObject
String: This is a test
SByte: 1
Byte: 2
Int16: 3
UInt16: 4
Int32: 5
UInt32: 6
Int64: 7
UInt64: 8
Decimal: 4623451.0232342352463856744563
Float: 5.5
Double: 6.6
Enum: B
EnumWithFlags: A, B
Bool: true
BoolFalse: false
A0Anchor: &o2 Test
A1Alias: *o2
Array: [1, 2, 3]
ArrayContent: [1, 2]
";

            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 20};
            settings.RegisterTagMapping("MyObject", typeof(MyObject));
            SerialRoundTrip(settings, text);
        }


        private class MyDynamicMember : DynamicMemberDescriptorBase
        {
            public MyDynamicMember() : base("~Id", typeof(int), typeof(object))
            {
                DynamicIds = new Dictionary<object, int>();
                Order = -1000;
            }

            public readonly Dictionary<object, int> DynamicIds;

            public override object Get(object thisObject)
            {
                int id;
                DynamicIds.TryGetValue(thisObject, out id);
                return id;
            }

            public override void Set(object thisObject, object value)
            {
                DynamicIds[thisObject] = (int) value;
            }

            public override bool HasSet { get { return true; } }
        }

        [Fact]
        public void TestDynamicMember()
        {
            var settings = new SerializerSettings();

            var dynamicMember = new MyDynamicMember();

            settings.Attributes.PrepareMembersCallback = (typeDesc, list) =>
            {
                if (typeof(MyObject) == typeDesc.Type)
                {
                    // Add our dynamic member
                    list.Add(dynamicMember);
                }
            };
            settings.RegisterTagMapping("MyObject", typeof(MyObject));

            var serializer = new Serializer(settings);
            var myObject = new MyObject();
            dynamicMember.DynamicIds[myObject] = 16;
            var testStr = serializer.Serialize(myObject);

            var myObject1 = serializer.Deserialize<MyObject>(testStr);

            // Make sure that the dynamic member is actually round trip copied
            Assert.True(dynamicMember.DynamicIds.ContainsKey(myObject1));
            Assert.Equal((object) 16, dynamicMember.DynamicIds[myObject1]);
        }

        public class ObjectWithLiteralString
        {
            [DataStyle(ScalarStyle.Literal)]
            public string Text { get; set; }
        }

        [Theory]
        [InlineData("Simple")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\r\n")]
        [InlineData("\t")]
        [InlineData("\r\n ")]
        [InlineData(" \r\n")]
        [InlineData("\r\n\t")]
        [InlineData("\t\r\n")]
        [InlineData("    A\r\n    B")]
        [InlineData("    A\r\n    B\r\n")]
        [InlineData("    A\r\n    B\r\n\r\n")]
        [InlineData("    A\r\n    B\r\n    \r\n")]
        public void TestLiteralString(string text)
        {
            var settings = new SerializerSettings() { LimitPrimitiveFlowSequence = 20, EmitAlias = false };
            settings.RegisterTagMapping(nameof(ObjectWithLiteralString), typeof(ObjectWithLiteralString));

            // Normalize line ending to current OS
            text = text.Replace("\r\n", Environment.NewLine);

            var original = new ObjectWithLiteralString { Text = text };
            var obj = SerialRoundTrip(settings, original);
            Assert.True(obj is ObjectWithLiteralString);
            Assert.Equal(original.Text, ((ObjectWithLiteralString)obj).Text);
        }

        public class ObjectWithFoldedString
        {
            [DataStyle(ScalarStyle.Folded)]
            public string Text { get; set; }
        }

        [Theory]
        [InlineData("Simple")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\r\n")]
        [InlineData("\t")]
        [InlineData("\r\n ")]
        [InlineData(" \r\n")]
        [InlineData("\r\n\t")]
        [InlineData("\t\r\n")]
        [InlineData("    A\r\n    B")]
        [InlineData("    A\r\n    B\r\n")]
        [InlineData("    A\r\n    B\r\n\r\n")]
        [InlineData("    A\r\n    B\r\n    \r\n")]
        public void TestFoldedString(string text)
        {
            var settings = new SerializerSettings() { LimitPrimitiveFlowSequence = 20, EmitAlias = false };
            settings.RegisterTagMapping(nameof(ObjectWithFoldedString), typeof(ObjectWithFoldedString));

            // Normalize line ending to current OS
            text = text.Replace("\r\n", Environment.NewLine);

            var original = new ObjectWithFoldedString { Text = text };
            var obj = SerialRoundTrip(settings, original);
            Assert.True(obj is ObjectWithFoldedString);
            Assert.Equal(original.Text, ((ObjectWithFoldedString)obj).Text);
        }
        public class ObjectFloatDoublePrecision
        {
            public float Float { get; set; }

            public double Double { get; set; }
        }

        [Fact]
        public void TestFloatDoublePrecision()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 20};
            settings.RegisterTagMapping("ObjectFloatDoublePrecision", typeof(ObjectFloatDoublePrecision));

            var text = @"!ObjectFloatDoublePrecision
Float: 1E-05
Double: 1E-05
";

            SerialRoundTrip(settings, text);
        }

        public class ObjectFloatDoubleNaNInfinity
        {
            public double DoubleNaN { get; set; }

            public double DoubleNegativeInfinity { get; set; }

            public double DoublePositiveInfinity { get; set; }

            public float FloatNaN { get; set; }

            public float FloatNegativeInfinity { get; set; }

            public float FloatPositiveInfinity { get; set; }
        }

        [Fact]
        public void TestFloatDoubleNaNInfinity()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 20};
            settings.RegisterTagMapping("ObjectFloatDoubleNaNInfinity", typeof(ObjectFloatDoubleNaNInfinity));

            var text = @"!ObjectFloatDoubleNaNInfinity
DoubleNaN: NaN
DoubleNegativeInfinity: -Infinity
DoublePositiveInfinity: Infinity
FloatNaN: NaN
FloatNegativeInfinity: -Infinity
FloatPositiveInfinity: Infinity
";

            SerialRoundTrip(settings, text);
        }

        public class MyObjectAndCollection
        {
            public MyObjectAndCollection()
            {
                Values = new List<string>();
            }

            public string Name { get; set; }

            public List<string> Values { get; set; }
        }


        /// <summary>
        /// Tests the serialization of an object that contains a property with list
        /// </summary>
        [Fact]
        public void TestObjectWithCollection()
        {
            var text = @"!MyObjectAndCollection
Name: Yes
Values: [a, b, c]
";

            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 20};
            settings.RegisterTagMapping("MyObjectAndCollection", typeof(MyObjectAndCollection));
            SerialRoundTrip(settings, text);
        }

        public class MyCustomCollectionWithProperties : List<string>
        {
            public string Name { get; set; }

            public int Value { get; set; }
        }

        /// <summary>
        /// Tests the serialization of a custom collection with some custom properties.
        /// In this specific case, the collection cannot be serialized as a simple list
        /// so the serializer is serializing the list as a YAML mapping, using the mapping
        /// to store the usual propertis and using the special member '~Items' to serialzie 
        /// the real content of the list
        /// </summary>
        [Fact]
        public void TestCustomCollectionWithProperties()
        {
            var text = @"!MyCustomCollectionWithProperties
Name: Yes
Value: 1
~Items:
  - a
  - b
  - c
";

            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};
            settings.RegisterTagMapping("MyCustomCollectionWithProperties", typeof(MyCustomCollectionWithProperties));
            SerialRoundTrip(settings, text);
        }


        public class MyCustomDictionaryWithProperties : Dictionary<string, bool>
        {
            public string Name { get; set; }

            public int Value { get; set; }
        }


        /// <summary>
        /// Tests the serialization of a custom dictionary with some custom properties.
        /// In this specific case, the dictionary cannot be serialized as a simple mapping
        /// so the serializer is serializing the dictionary as a YAML !!map, using the mapping
        /// to store the usual propertis and using the special member '~Items' to serialize 
        /// the real content of the dictionary as a sub YAML !!map
        /// </summary>
        [Fact]
        public void TestCustomDictionaryWithProperties()
        {
            var text = @"!MyCustomDictionaryWithProperties
Name: Yes
Value: 1
~Items:
  a: true
  b: false
  c: true
";

            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};
            settings.RegisterTagMapping("MyCustomDictionaryWithProperties", typeof(MyCustomDictionaryWithProperties));
            SerialRoundTrip(settings, text);
        }


        /// <summary>
        /// Tests the serialization of a custom dictionary with some custom properties while allowing to serialize both
        /// member and items into the same YAML mapping, using the option Settings.SerializeDictionaryItemsAsMembers = true
        /// </summary>
        [Fact]
        public void TestCustomDictionaryWithItemsAsMembers()
        {
            var text = @"!MyCustomDictionaryWithProperties
Name: Yes
Value: 1
a: true
b: false
c: true
";

            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0, SerializeDictionaryItemsAsMembers = true};
            settings.RegisterTagMapping("MyCustomDictionaryWithProperties", typeof(MyCustomDictionaryWithProperties));
            SerialRoundTrip(settings, text);
        }


        public class MyCustomClassWithSpecialMembers
        {
            public MyCustomClassWithSpecialMembers()
            {
                StringListByContent = new List<string>();
                StringMapbyContent = new Dictionary<string, object>();
                ListByContent = new List<string>();
            }

            public string Name { get; set; }

            public int Value { get; set; }

            /// <summary>
            /// Gets or sets the basic list.
            /// </summary>
            /// <value>The basic list.</value>
            public IList BasicList { get; set; }

            /// <summary>
            /// For this property, the deserializer is instantiating
            /// automatically a default List&lt;string&gtl instance.
            /// </summary>
            public IList<string> StringList { get; set; }

            /// <summary>
            /// For this property, the deserializer is using the actual
            /// value of the list stored in this instance instead of 
            /// creating a new List&lt;T&gtl instance.
            /// </summary>
            public List<string> StringListByContent { get; private set; }

            /// <summary>
            /// Gets or sets the basic map.
            /// </summary>
            /// <value>The basic map.</value>
            public IDictionary BasicMap { get; set; }

            /// <summary>
            /// Idem as for <see cref="StringList"/> but for dictionary.
            /// </summary>
            /// <value>The string map.</value>
            public IDictionary<string, object> StringMap { get; set; }


            /// <summary>
            /// Idem as for <see cref="StringListByContent"/> but for dictionary.
            /// </summary>
            /// <value>The content of the string mapby.</value>
            public Dictionary<string, object> StringMapbyContent { get; private set; }

            /// <summary>
            /// For this property, the deserializer is using the actual
            /// value of the list stored in this instance instead of 
            /// creating a new List&lt;T&gtl instance.
            /// </summary>
            /// <value>The content of the list by.</value>
            public IList ListByContent { get; private set; }
        }

        /// <summary>
        /// Tests the serialization of a custom dictionary with some custom properties.
        /// In this specific case, the dictionary cannot be serialized as a simple mapping
        /// so the serializer is serializing the dictionary as a YAML !!map, using the mapping
        /// to store the usual propertis and using the special member '~Items' to serialize 
        /// the real content of the dictionary as a sub YAML !!map
        /// </summary>
        [Fact]
        public void TestMyCustomClassWithSpecialMembers()
        {
            var text = @"!MyCustomClassWithSpecialMembers
Name: Yes
Value: 0
BasicList:
  - 1
  - 2
StringList:
  - 1
  - 2
StringListByContent:
  - 3
  - 4
BasicMap:
  a: 1
  b: 2
StringMap:
  c: yes
  d: 3
StringMapbyContent:
  e: 4
  f: no
ListByContent:
  - a
  - b
";

            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};
            settings.RegisterTagMapping("MyCustomClassWithSpecialMembers", typeof(MyCustomClassWithSpecialMembers));
            SerialRoundTrip(settings, text);
        }


        /// <summary>
        /// Tests the serialization of ordered members.
        /// </summary>
        public class ClassMemberOrder
        {
            public ClassMemberOrder()
            {
                First = 1;
                Second = 2;
                BeforeName = 3;
                Name = 4;
                NameAfter = 5;
            }

            /// <summary>
            /// Sets an explicit order
            /// </summary>
            [DataMember(1)]
            public int Second { get; set; }

            /// <summary>
            /// Sets an explicit order
            /// </summary>
            [DataMember(0)]
            public int First { get; set; }

            /// <summary>
            /// This property will be sorted after 
            /// the explicit order by alphabetical order
            /// </summary>
            /// <value>The name after.</value>
            public int NameAfter { get; set; }

            /// <summary>
            /// This property will be sorted after 
            /// the explicit order by alphabetical order
            /// </summary>
            public int Name { get; set; }

            /// <summary>
            /// Sets an explicit order
            /// </summary>
            [DataMember(2)]
            public int BeforeName { get; set; }
        }

        /// <summary>
        /// Tests the serialization of ordered members in the class <see cref="ClassMemberOrder"/>.
        /// </summary>
        [Fact]
        public void TestClassMemberOrder()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};
            settings.RegisterTagMapping("ClassMemberOrder", typeof(ClassMemberOrder));
            SerialRoundTrip(settings, new ClassMemberOrder());
        }

        public class ClassWithMemberIEnumerable
        {
            public IEnumerable<int> Keys { get { return Enumerable.Range(0, 10); } }
        }

        // We no longer support IEnumerable
        //[Fact]
        //public void TestIEnumerable()
        //{
        //    var serializer = new Serializer();
        //    var text = serializer.Serialize(new ClassWithMemberIEnumerable(), typeof (ClassWithMemberIEnumerable));
        //    Assert.Throws<YamlException>(() => serializer.Deserialize(text, typeof(ClassWithMemberIEnumerable)));
        //    var value = serializer.Deserialize(text);

        //    Assert.True(value is IDictionary<object, object>);
        //    var dictionary = (IDictionary<object, object>) value;
        //    Assert.True(dictionary.ContainsKey("Keys"));
        //    Assert.True( dictionary["Keys"] is IList<object>);
        //    var list = (IList<object>) dictionary["Keys"];
        //    Assert.Equal(list.OfType<int>(), new ClassWithMemberIEnumerable().Keys);

        //    // Test simple IEnumerable
        //    var iterator = Enumerable.Range(0, 10);
        //    var values = serializer.Deserialize(serializer.Serialize(iterator, iterator.GetType()));
        //    Assert.True(value is IEnumerable);
        //    Assert.Equal(((IEnumerable<object>)values).OfType<int>(), iterator);
        //}

        public class ClassWithObjectAndScalar
        {
            public ClassWithObjectAndScalar()
            {
                Value1 = 1;
                Value2 = 2.0f;
                Value3 = "3";
                Value4 = (byte) 4;
            }

            public object Value1;

            public object Value2;

            public object Value3;

            public object Value4;
        }

        [Fact]
        public void TestClassWithObjectAndScalar()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};
            settings.RegisterTagMapping("ClassWithObjectAndScalar", typeof(ClassWithObjectAndScalar));
            SerialRoundTrip(settings, new ClassWithObjectAndScalar());
        }


        [Fact]
        public void TestNoEmitTags()
        {
            var settings = new SerializerSettings() {EmitTags = false};
            settings.RegisterTagMapping("ClassWithObjectAndScalar", typeof(ClassWithObjectAndScalar));
            var serializer = new Serializer(settings);
            var text = serializer.Serialize(new ClassWithObjectAndScalar {Value4 = new ClassWithObjectAndScalar()});
            Assert.DoesNotContain("!", text);
        }

        [Fact]
        public void TestImplicitDictionaryAndList()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};

            var text = @"BasicList:
  - 1
  - 2
BasicMap:
  a: 1
  b: 2
ListByContent:
  - a
  - b
Name: Yes
StringList:
  - 1
  - 2
StringListByContent:
  - 3
  - 4
StringMap:
  c: yes
  d: 3
StringMapbyContent:
  e: 4
  f: no
Value: 0
";

            SerialRoundTrip(settings, text, typeof(Dictionary<object, object>));
        }


        public interface IMemberInterface
        {
            string Name { get; set; }
        }

        public class MemberInterface : IMemberInterface
        {
            protected bool Equals(MemberInterface other)
            {
                return string.Equals(Name, other.Name) && string.Equals(Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((MemberInterface) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name != null ? Name.GetHashCode() : 0)*397) ^ (Value != null ? Value.GetHashCode() : 0);
                }
            }

            public MemberInterface()
            {
                Name = "name1";
                Value = "value1";
            }

            public string Name { get; set; }

            public string Value { get; set; }
        }

        public class MemberObject : MemberInterface
        {
            public MemberObject()
            {
                Object = "object1";
            }

            public string Object { get; set; }

            protected bool Equals(MemberObject other)
            {
                return base.Equals(other) && string.Equals(Object, other.Object);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((MemberObject) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (base.GetHashCode()*397) ^ (Object != null ? Object.GetHashCode() : 0);
                }
            }
        }

        public class ClassMemberWithInheritance
        {
            public ClassMemberWithInheritance()
            {
                ThroughObject = new MemberObject() {Object = "throughObject"};
                ThroughInterface = new MemberInterface();
                ThroughBase = new MemberObject() {Object = "throughBase"};
                Direct = new MemberObject() {Object = "direct"};
            }

            public object ThroughObject { get; set; }

            public IMemberInterface ThroughInterface { get; set; }

            public MemberInterface ThroughBase { get; set; }

            public MemberObject Direct { get; set; }

            protected bool Equals(ClassMemberWithInheritance other)
            {
                return Equals(ThroughObject, other.ThroughObject) && Equals(ThroughInterface, other.ThroughInterface) && Equals(ThroughBase, other.ThroughBase) && Equals(Direct, other.Direct);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((ClassMemberWithInheritance) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (ThroughObject != null ? ThroughObject.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (ThroughInterface != null ? ThroughInterface.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (ThroughBase != null ? ThroughBase.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Direct != null ? Direct.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        [Fact]
        public void TestClassMemberWithInheritance()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};
            settings.RegisterTagMapping("ClassMemberWithInheritance", typeof(ClassMemberWithInheritance));
            settings.RegisterTagMapping("MemberInterface", typeof(MemberInterface));
            settings.RegisterTagMapping("MemberObject", typeof(MemberObject));
            var original = new ClassMemberWithInheritance();
            var obj = SerialRoundTrip(settings, original);
            Assert.True(obj is ClassMemberWithInheritance);
            Assert.Equal(original, obj);
        }

        [Fact]
        public void TestEmitShortTypeName()
        {
            var settings = new SerializerSettings() {EmitShortTypeName = true};
            settings.RegisterAssembly(typeof(SerializationTests2).Assembly);
            SerialRoundTrip(settings, new ClassWithObjectAndScalar());
        }

        public class ClassWithChars
        {
            [DataMember(0)] public char Start;

            [DataMember(1)] public char End;
        }

        [Fact]
        public void TestClassWithChars()
        {
            var settings = new SerializerSettings() {EmitShortTypeName = true};
            settings.RegisterAssembly(typeof(SerializationTests2).Assembly);
            SerialRoundTrip(settings, new ClassWithChars()
            {
                Start = ' ',
                End = '\x7f'
            });
        }

        [Fact]
        public void TestClassWithSpecialChars()
        {
            for (int i = 0; i < 32; i++)
            {
                var settings = new SerializerSettings() { EmitShortTypeName = true };
                settings.RegisterAssembly(typeof(SerializationTests2).Assembly);
                SerialRoundTrip(settings, new ClassWithChars()
                {
                    Start = (char) i,
                    End = (char) i,
                });
            }
        }


        [DataStyle(DataStyle.Compact)]
        public class ClassWithStyle
        {
            public string Name { get; set; }

            public object Value { get; set; }
        }

        public class ClassNoStyle
        {
            public ClassNoStyle()
            {
                A_ListWithCustomStyle = new List<string>();
                D_ListHandleByDynamicStyleFormat = new List<object>();
                E_ListDefaultPrimitiveLimit = new List<int>();
                E_ListDefaultPrimitiveLimitExceed = new List<int>();
                F_ListClassWithStyleDefaultFormat = new List<ClassWithStyle>();
                G_ListCustom = new CustomList();
            }

            [DataStyle(DataStyle.Compact)]
            public List<string> A_ListWithCustomStyle { get; set; }

            public ClassWithStyle B_ClassWithStyle { get; set; }

            [DataStyle(DataStyle.Normal)]
            public ClassWithStyle C_ClassWithStyleOverridenByLocalYamlStyle { get; set; }

            public List<object> D_ListHandleByDynamicStyleFormat { get; set; }

            public List<int> E_ListDefaultPrimitiveLimit { get; set; }

            public List<int> E_ListDefaultPrimitiveLimitExceed { get; set; }

            public List<ClassWithStyle> F_ListClassWithStyleDefaultFormat { get; set; }

            public CustomList G_ListCustom { get; set; }
        }

        [DataStyle(DataStyle.Compact)]
        public class CustomList : List<object>
        {
            public string Name { get; set; }
        }


        private class FormatListObject : DefaultObjectSerializerBackend
        {
            public override DataStyle GetStyle(ref ObjectContext objectContext)
            {
                return objectContext.Instance is List<object> ? DataStyle.Compact : base.GetStyle(ref objectContext);
            }
        }

        /// <summary>
        /// Tests formatting styles.
        /// </summary>
        [Fact]
        public void TestStyles()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 4};
            settings.RegisterTagMapping("ClassNoStyle", typeof(ClassNoStyle));
            settings.RegisterTagMapping("ClassWithStyle", typeof(ClassWithStyle));
            settings.ObjectSerializerBackend = new FormatListObject();

            var classNoStyle = new ClassNoStyle();
            classNoStyle.A_ListWithCustomStyle.Add("a");
            classNoStyle.A_ListWithCustomStyle.Add("b");
            classNoStyle.A_ListWithCustomStyle.Add("c");
            classNoStyle.B_ClassWithStyle = new ClassWithStyle() {Name = "name1", Value = 1};
            classNoStyle.C_ClassWithStyleOverridenByLocalYamlStyle = new ClassWithStyle() {Name = "name2", Value = 2};
            classNoStyle.D_ListHandleByDynamicStyleFormat.Add(1);
            classNoStyle.D_ListHandleByDynamicStyleFormat.Add(2);
            classNoStyle.D_ListHandleByDynamicStyleFormat.Add(3);
            classNoStyle.E_ListDefaultPrimitiveLimit.Add(1);
            classNoStyle.E_ListDefaultPrimitiveLimit.Add(2);
            classNoStyle.E_ListDefaultPrimitiveLimitExceed.Add(1);
            classNoStyle.E_ListDefaultPrimitiveLimitExceed.Add(2);
            classNoStyle.E_ListDefaultPrimitiveLimitExceed.Add(3);
            classNoStyle.E_ListDefaultPrimitiveLimitExceed.Add(4);
            classNoStyle.E_ListDefaultPrimitiveLimitExceed.Add(5);
            classNoStyle.F_ListClassWithStyleDefaultFormat.Add(new ClassWithStyle() {Name = "name3", Value = 3});
            classNoStyle.G_ListCustom.Name = "name4";
            classNoStyle.G_ListCustom.Add(1);
            classNoStyle.G_ListCustom.Add(2);
            classNoStyle.G_ListCustom.Add(3);
            classNoStyle.G_ListCustom.Add(4);
            classNoStyle.G_ListCustom.Add(5);
            classNoStyle.G_ListCustom.Add(6);
            classNoStyle.G_ListCustom.Add(7);

            var serializer = new Serializer(settings);
            var text = serializer.Serialize(classNoStyle);

            var textReference = @"!ClassNoStyle
A_ListWithCustomStyle: [a, b, c]
B_ClassWithStyle: {Name: name1, Value: 1}
C_ClassWithStyleOverridenByLocalYamlStyle:
  Name: name2
  Value: 2
D_ListHandleByDynamicStyleFormat: [1, 2, 3]
E_ListDefaultPrimitiveLimit: [1, 2]
E_ListDefaultPrimitiveLimitExceed:
  - 1
  - 2
  - 3
  - 4
  - 5
F_ListClassWithStyleDefaultFormat:
  - {Name: name3, Value: 3}
G_ListCustom: {Name: name4, ~Items: [1, 2, 3, 4, 5, 6, 7]}
";

            Assert.Equal(textReference, text);
        }


        public class ClassWithKeyTransform
        {
            public ClassWithKeyTransform()
            {
                KeyValues = new Dictionary<string, object>();
            }

            [DataMember(0)]
            public string Name { get; set; }

            [DataMember(1)]
            public Dictionary<string, object> KeyValues { get; set; }
        }

        class MyMappingKeyTransform : DefaultObjectSerializerBackend
        {
            public MyMappingKeyTransform()
            {
                SpecialKeys = new List<Tuple<object, object>>();
            }

            public List<Tuple<object, object>> SpecialKeys { get; private set; }

            public override string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember)
            {
                skipMember = false;
                if (memberName.EndsWith("!"))
                {
                    memberName = memberName.Substring(0, memberName.Length - 1);
                    SpecialKeys.Add(new Tuple<object, object>(objectContext.Instance, objectContext.Descriptor[memberName]));
                }
                return memberName;
            }

            public override object ReadDictionaryKey(ref ObjectContext objectContext, Type keyType)
            {
                var itemKey = base.ReadDictionaryKey(ref objectContext, keyType) as string;
                if (itemKey != null && itemKey.EndsWith("!"))
                {
                    itemKey = itemKey.Substring(0, itemKey.Length - 1);
                    SpecialKeys.Add(new Tuple<object, object>(objectContext.Instance, itemKey));
                }
                return itemKey;
            }

            public override void WriteDictionaryKey(ref ObjectContext objectContext, object key, Type keyType)
            {
                var itemKey = key as string;
                if (itemKey != null && (itemKey.Contains("Name") || itemKey.Contains("Test")))
                {
                    itemKey = itemKey + "!";
                }
                base.WriteDictionaryKey(ref objectContext, itemKey, keyType);
            }

            public override void WriteMemberName(ref ObjectContext objectContext, IMemberDescriptor member, string name)
            {
                name = (name.Contains("Name") || name.Contains("Test")) ? name + "!" : name;
                base.WriteMemberName(ref objectContext, member, name);
            }
        }

        /// <summary>
        /// Tests the key transform capabilities.
        /// </summary>
        [Fact]
        public void TestKeyTransform()
        {
            var specialTransform = new MyMappingKeyTransform();
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 4};
            settings.ObjectSerializerBackend = specialTransform;
            settings.RegisterTagMapping("ClassWithKeyTransform", typeof(ClassWithKeyTransform));

            var myCustomObject = new ClassWithKeyTransform();
            myCustomObject.Name = "Yes";
            myCustomObject.KeyValues.Add("Test1", 1);
            myCustomObject.KeyValues.Add("Test2", 2);
            myCustomObject.KeyValues.Add("KeyNotModified1", 1);
            myCustomObject.KeyValues.Add("KeyNotModified2", 2);

            var serializer = new Serializer(settings);
            var myCustomObjectText = serializer.Serialize(myCustomObject);

            var myCustomObject2 = (ClassWithKeyTransform) serializer.Deserialize(myCustomObjectText);

            Assert.Equal(3, specialTransform.SpecialKeys.Count);

            Assert.Equal(myCustomObject2.KeyValues, specialTransform.SpecialKeys[1].Item1);
            Assert.Equal(myCustomObject2.KeyValues, specialTransform.SpecialKeys[2].Item1);

            Assert.Equal("Test1", specialTransform.SpecialKeys[1].Item2);
            Assert.Equal("Test2", specialTransform.SpecialKeys[2].Item2);

            Assert.Equal(myCustomObject2, specialTransform.SpecialKeys[0].Item1);
            Assert.True(specialTransform.SpecialKeys[0].Item2 is IMemberDescriptor);

            Assert.Equal("Name", ((IMemberDescriptor) specialTransform.SpecialKeys[0].Item2).Name);
        }


        /// <summary>
        /// Tests skipping members
        /// </summary>
        [Fact]
        public void TestSkipMember()
        {
            var specialTransform = new SkipMemberTransform();
            var settings = new SerializerSettings
            {
                LimitPrimitiveFlowSequence = 4,
                ObjectSerializerBackend = specialTransform
            };
            settings.RegisterTagMapping("TestRemapObject", typeof(TestRemapObject));

            var serializer = new Serializer(settings);
            var obj = serializer.Deserialize<TestRemapObject>(@"Name: Test
Enum: Value2");
            Assert.Null(obj.Name);
            Assert.Equal(MyRemapEnum.Value2, obj.Enum);
        }

        class SkipMemberTransform : DefaultObjectSerializerBackend
        {
            public override string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember)
            {
                var readMemberName = base.ReadMemberName(ref objectContext, memberName, out skipMember);
                skipMember = memberName == "Name";
                return readMemberName;
            }
        }

        [DataContract("TestRemapObject")]
        [DataAlias("TestRemapObjectOld")]
        public class TestRemapObject
        {
            [DataAlias("OldName")]
            public string Name { get; set; }

            public MyRemapEnum Enum { get; set; }
        }

        [DataContract("MyRemapEnum")]
        public enum MyRemapEnum
        {
            [DataAlias("OldValue1")] Value1,

            [DataAlias("OldValue2")] Value2
        }

        [Fact]
        public void TestRemap()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(TestRemapObject).Assembly);

            var serializer = new Serializer(settings);
            SerializerContext context;

            // Test no-remap
            var myCustomObjectText = serializer.Deserialize<TestRemapObject>(@"!TestRemapObject
Name: Test1
Enum: Value2
", out context);
            Assert.Equal("Test1", myCustomObjectText.Name);
            Assert.Equal(MyRemapEnum.Value2, myCustomObjectText.Enum);
            Assert.False(context.HasRemapOccurred);

            // Test all
            myCustomObjectText = serializer.Deserialize<TestRemapObject>(@"!TestRemapObjectOld
OldName: Test1
Enum: OldValue2
", out context);
            Assert.Equal("Test1", myCustomObjectText.Name);
            Assert.Equal(MyRemapEnum.Value2, myCustomObjectText.Enum);
            Assert.True(context.HasRemapOccurred);

            // Test HasRemapOccurred for Class name
            serializer.Deserialize<TestRemapObject>(@"!TestRemapObjectOld
Name: Test1
Enum: Value2
", out context);
            Assert.True(context.HasRemapOccurred);

            // Test HasRemapOccurred for of property name
            serializer.Deserialize<TestRemapObject>(@"!TestRemapObject
OldName: Test1
Enum: Value2
", out context);
            Assert.True(context.HasRemapOccurred);

            // Test HasRemapOccurred for of enum items
            serializer.Deserialize<TestRemapObject>(@"!TestRemapObject
Name: Test1
Enum: OldValue2
", out context);
            Assert.True(context.HasRemapOccurred);
        }

        [DataContract("TestWithMemberRenamed")]
        public sealed class TestWithMemberRenamed
        {
            [DataMember("~Base")]
            [DefaultValue(null)]
            public string Base { get; set; }
        }

        [Fact]
        public void TestYamlMember()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(TestWithMemberRenamed).Assembly);

            var value = new TestWithMemberRenamed {Base = "Test"};
            var serializer = new Serializer(settings);
            var text = serializer.Serialize(value);
            Assert.Contains("~Base", text);

            settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(TestWithMemberRenamed).Assembly);
            SerialRoundTrip(settings, value);
        }

        public sealed class MyClassImmutable
        {
            public MyClassImmutable(string name, int value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; private set; }

            public int Value { get; private set; }

            protected bool Equals(MyClassImmutable other)
            {
                return string.Equals(Name, other.Name) && Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((MyClassImmutable) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name != null ? Name.GetHashCode() : 0)*397) ^ Value;
                }
            }
        }

        public class MyClassImmutableSerializer : ObjectSerializer
        {
            public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
            {
                return typeDescriptor.Type == typeof(MyClassImmutable) ? this : null;
            }

            protected override void CreateOrTransformObject(ref ObjectContext objectContext)
            {
                objectContext.Instance = objectContext.SerializerContext.IsSerializing ? new MyClassMutable((MyClassImmutable) objectContext.Instance) : new MyClassMutable();
            }

            protected override void TransformObjectAfterRead(ref ObjectContext objectContext)
            {
                objectContext.Instance = ((MyClassMutable) objectContext.Instance).ToImmutable();
            }

            /// <summary>
            /// Use internally this object to serialize/deserialize instead of MyClassImmutable
            /// </summary>
            internal sealed class MyClassMutable
            {
                public MyClassMutable()
                {
                }

                public MyClassMutable(MyClassImmutable immutable)
                {
                    Name = immutable.Name;
                    Value = immutable.Value;
                }

                public string Name { get; set; }

                public int Value { get; set; }

                public MyClassImmutable ToImmutable()
                {
                    return new MyClassImmutable(Name, Value);
                }
            }
        }

        /// <summary>
        /// Example on how to handle immutable-mutable object when serializing/deserializing.
        /// </summary>
        [Fact]
        public void TestImmutable()
        {
            var settings = new SerializerSettings();
            settings.RegisterTagMapping("MyClassImmutable", typeof(MyClassImmutable));

            // Automatically register MyClassImmutableSerializer assembly
            settings.RegisterAssembly(typeof(SerializationTests2).Assembly);

            var immutable = new MyClassImmutable("Test", 1);
            var serializer = new Serializer(settings);
            var text = serializer.Serialize(immutable);

            var newImmutable = serializer.Deserialize(text);
            Assert.Equal(immutable, newImmutable);
        }

        [Fact]
        public void TestDictionaryWithObjectValue()
        {
            var settings = new SerializerSettings();
            settings.RegisterTagMapping("Color", typeof(Color));
            settings.RegisterTagMapping("ObjectWithDictionaryAndObjectValue", typeof(ObjectWithDictionaryAndObjectValue));

            var item = new ObjectWithDictionaryAndObjectValue();
            item.Values.Add("Test", new Color() {R = 1, G = 2, B = 3, A = 4});

            var serializer = new Serializer(settings);
            var text = serializer.Serialize(item);

            var newItem = (ObjectWithDictionaryAndObjectValue) serializer.Deserialize(text);
            Assert.Single(newItem.Values);
            Assert.True(newItem.Values.ContainsKey("Test"));
            Assert.Equal(item.Values["Test"], newItem.Values["Test"]);
        }

        public class ObjectWithDictionaryAndObjectValue
        {
            public ObjectWithDictionaryAndObjectValue()
            {
                Values = new Dictionary<string, object>();
            }

            public Dictionary<string, object> Values { get; set; }
        }

        [Fact]
        public void TestMaskSimple()
        {
            var settings = new SerializerSettings();
            settings.RegisterTagMapping("ObjectWithMask", typeof(ObjectWithMask));

            var item = new ObjectWithMask {Int1 = 1, Int2 = 2, Int3 = 3};

            var serializer = new Serializer(settings);
            var text = serializer.Serialize(item);

            var newItem = (ObjectWithMask) serializer.Deserialize(text);

            // Default: mask != 1 is ignored
            Assert.Equal(item.Int1, newItem.Int1);
            Assert.Equal(0, newItem.Int2);
            Assert.Equal(0, newItem.Int3);

            settings = new SerializerSettings();
            settings.RegisterTagMapping("ObjectWithMask", typeof(ObjectWithMask));
            serializer = new Serializer(settings);
            text = serializer.Serialize(item, null, new SerializerContextSettings {MemberMask = 4});

            newItem = (ObjectWithMask) serializer.Deserialize(text);

            // Only Int2 and Int3 should be serialized
            Assert.Equal(0, newItem.Int1);
            Assert.Equal(item.Int2, newItem.Int2);
            Assert.Equal(item.Int3, newItem.Int3);

            settings = new SerializerSettings();
            settings.RegisterTagMapping("ObjectWithMask", typeof(ObjectWithMask));
            serializer = new Serializer(settings);
            text = serializer.Serialize(item, null, new SerializerContextSettings {MemberMask = 1 | 4});

            newItem = (ObjectWithMask) serializer.Deserialize(text);

            // Everything should be serialized
            Assert.Equal(newItem.Int1, item.Int1);
            Assert.Equal(newItem.Int2, item.Int2);
            Assert.Equal(newItem.Int3, item.Int3);
        }

        public class ObjectWithMask
        {
            public int Int1 { get; set; }

            [DataMember(Mask = 4)]
            public int Int2 { get; set; }

            [DataMember(Mask = 4)]
            internal int Int3 { get; set; }
        }

        [Fact]
        public void TestImplicitMemberType()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};

            var text = @"!ClassWithImplicitMemberType
Test:
  String: test
";

            settings.RegisterTagMapping("ClassWithImplicitMemberType", typeof(ClassWithImplicitMemberType));
            settings.RegisterTagMapping("ClassWithImplicitMemberTypeInner", typeof(ClassWithImplicitMemberTypeInner));
            SerialRoundTrip(settings, text);
        }

        [Fact]
        public void TestNonImplicitMemberType()
        {
            var settings = new SerializerSettings() {LimitPrimitiveFlowSequence = 0};

            var text = @"!ClassWithNonImplicitMemberType
Test: !ClassWithImplicitMemberTypeInner
  String: test
";

            settings.RegisterTagMapping("ClassWithNonImplicitMemberType", typeof(ClassWithNonImplicitMemberType));
            settings.RegisterTagMapping("ClassWithImplicitMemberTypeInner", typeof(ClassWithImplicitMemberTypeInner));
            SerialRoundTrip(settings, text);
        }


        [Fact]
        public void TestLongIntegers()
        {
            var serializer = new Serializer();

            var values = new List<object>();
            int intValue = 5;
            long longValue = 50000000000000L;
            ulong uLongValue = ulong.MaxValue;
            values.Add(intValue);
            values.Add(longValue);
            values.Add(uLongValue);

            var text = serializer.Serialize(values);

            var values2 = serializer.Deserialize(new StringReader(text)) as List<object>;
            Assert.NotNull(values2);
            Assert.Equal(3, values.Count);
            Assert.Equal(intValue, values[0]);
            Assert.Equal(longValue, values[1]);
            Assert.Equal(uLongValue, values[2]);
        }

        public class ClassWithImplicitMemberType
        {
            public ClassWithImplicitMemberType()
            {
                Test = new ClassWithImplicitMemberTypeInner {String = "test"};
            }

            public object Test { get; protected set; }
        }

        public class ClassWithNonImplicitMemberType
        {
            public ClassWithNonImplicitMemberType()
            {
                Test = new ClassWithImplicitMemberTypeInner {String = "test"};
            }

            public object Test { get; set; }
        }

        public class ClassWithImplicitMemberTypeInner
        {
            public string String { get; set; }
        }

        private void SerialRoundTrip(SerializerSettings settings, string text, Type serializedType = null)
        {
            var serializer = new Serializer(settings);
            // not working yet, scalar read/write are not yet implemented
            Console.WriteLine("Text to serialize:");
            Console.WriteLine("------------------");
            Console.WriteLine(text);
            var value = serializer.Deserialize(text);

            Console.WriteLine();

            var text2 = serializer.Serialize(value, serializedType);
            Console.WriteLine("Text deserialized:");
            Console.WriteLine("------------------");
            Console.WriteLine(text2);

            Assert.Equal(text, text2);
        }

        private object SerialRoundTrip(SerializerSettings settings, object value, Type expectedType = null)
        {
            var serializer = new Serializer(settings);

            var text = serializer.Serialize(value, expectedType);
            Console.WriteLine("Text serialize:");
            Console.WriteLine("------------------");
            Console.WriteLine(text);

            // not working yet, scalar read/write are not yet implemented
            Console.WriteLine("Text to deserialize/serialize:");
            Console.WriteLine("------------------");
            var valueDeserialized = serializer.Deserialize(text);
            var text2 = serializer.Serialize(valueDeserialized);
            Console.WriteLine(text2);

            Assert.Equal(text, text2);

            return valueDeserialized;
        }
    }
}
