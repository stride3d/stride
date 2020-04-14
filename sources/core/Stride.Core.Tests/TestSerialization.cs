// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Stride.Core.Serialization;
using Xunit;

namespace Stride.Core.Tests
{
    public partial class TestSerialization
    {
        [DataContract]
        public struct SerializeStructTest
        {
            public int A { get; set; }
            public int B;
        }

        [DataContract]
        public class SerializeClassTest
        {
            public int A { get; set; }
            public int B;
        }

        public enum SerializeEnum
        {
            Enum1,
            Enum2,
            Enum3,
        }

        [DataContract]
        public class SerializeBaseTypeTest
        {
            public bool Bool;
            public float F;
            public double D;
            public ushort U16;
            public short I16;
            public uint U32;
            public int I32;
            public ulong U64;
            public long I64;
            public string String;
            public string StringNull;
            public char C;
            public byte B;
            public sbyte SB;
            public int? Nullable1;
            public int? Nullable2;
            public SerializeEnum Enum;
            public Enum Enum2;
            public Enum EnumNull;
            public byte[] Data;
            public byte[] DataNull;
            public Guid Guid;
            //public decimal Decimal; Not implemented
        }

        [DataContract(Inherited = true)]
        public class A {}
        public class B : A { }

        [DataContract]
        public class SerializeTypeTest
        {
            public object A { get; set; }
        }

        [DataContract]
        public class SerializeCollectionTest
        {
            public List<int> List;
            public List<int> ListNull;
            public IList<int> ListInterface;
            public IList<int> ListInterfaceNull;
            public List<SerializeClassTest> ListClass;
            public Dictionary<string, string> Dictionary;
            public Dictionary<string, string> DictionaryNull;
            public readonly List<int> ReadOnlyList = new List<int>();
        }

        [DataContract]
        public struct S
        {
            public int A { get; set; }
        }

        public byte[] SerializeDotNet<T>(T obj)
        {
            var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var memoryStream = new MemoryStream();
            bformatter.Serialize(memoryStream, obj);
            return memoryStream.ToArray();
        }

        public enum SerializationBackend
        {
            Binary,
            Xml,
        }

        public byte[] SerializeBinary<T>(T obj)
        {
            var memoryStream = new MemoryStream();
            var writer = new BinarySerializationWriter(memoryStream);
            writer.Write(obj);
            writer.Flush();

            return memoryStream.ToArray();
        }

        public T CopyBySerialization<T>(T obj, SerializationBackend serializationBackend)
        {
            var result = default(T);

            if (serializationBackend == SerializationBackend.Binary)
            {
                var memoryStream = new MemoryStream();
                var writer = new BinarySerializationWriter(memoryStream);
                writer.Write(obj);
                writer.Flush();

                memoryStream.Seek(0, SeekOrigin.Begin);
                var reader = new BinarySerializationReader(memoryStream);
                reader.Serialize(ref result, ArchiveMode.Deserialize);
            }
            //else if (serializationBackend == SerializationBackend.Xml)
            //{
            //    var xmlDoc = new XmlDocument();
            //    var xmlElement = xmlDoc.CreateElement("object");

            //    var writer = new XmlSerializationWriter(xmlElement);
            //    writer.Write(obj);
            //    writer.Flush();

            //    var reader = new XmlSerializationReader(xmlElement);
            //    reader.Serialize(ref result, ArchiveMode.Deserialize);
            //}

            return result;
        }

        //[InlineData(SerializationBackend.Binary)]
        //[InlineData(SerializationBackend.Xml)]
        //[Description("Test struct serialization of field and properties (using ComplexTypeSerializer)")]
        //public void TestSerializationComplexTypeStruct(SerializationBackend serializationBackend)
        //{
        //    Serializer.Default.RegisterSerializer(new ComplexTypeSerializer<SerializeStructTest> { Flags = ComplexTypeSerializerFlags.SerializePublicProperties | ComplexTypeSerializerFlags.SerializePublicFields });
        //    var source = new SerializeStructTest { A = 32, B = 123 };
        //    var copy = CopyBySerialization(source, serializationBackend);
        //    Assert.Equal(source.A, copy.A);
        //    Assert.Equal(source.B, copy.B);
        //}

        [Theory]
        [InlineData(SerializationBackend.Binary)]
        [Description("Test class serialization of field and properties (using ComplexTypeSerializer)")]
        public void TestSerializationComplexTypeClass(SerializationBackend serializationBackend)
        {
            var source = new SerializeClassTest { A = 32, B = 123 };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.Equal(source.A, copy.A);
            Assert.Equal(source.B, copy.B);
        }

        [Theory]
        [InlineData(SerializationBackend.Binary)]
        [Description("Test class serialization of field and properties (using ComplexTypeSerializer)")]
        public void TestSerializationList(SerializationBackend serializationBackend)
        {
            var source = new List<int> { 32, 12, 15 };
            var copy = CopyBySerialization(source, serializationBackend);

            Assert.Equal(source.Count, copy.Count);
            for (int i = 0; i < source.Count; ++i)
                Assert.Equal(source[i], copy[i]);
        }

        [Theory]
        [InlineData(SerializationBackend.Binary)]
        [Description("Test serialization for every base types")]
        public void TestSerializationBaseTypes(SerializationBackend serializationBackend)
        {
            var source = new SerializeBaseTypeTest
                            {
                                Nullable1 = 546,
                                Bool = true,
                                B = 12,
                                SB = 14,
                                C = 'a',
                                F = 12.0f,
                                D = 13.0,
                                Enum = SerializeEnum.Enum2,
                                Enum2 = SerializeEnum.Enum3,
                                I16 = -8,
                                I32 = -12,
                                I64 = -16,
                                U16 = 8,
                                U32 = 12,
                                U64 = 16,
                                //Decimal = 12300,
                                String = "Tat\"asd\\a",
                                Data = new byte[] { 12, 31, 11 },
                                Guid = Guid.NewGuid(),
                            };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.Equal(source.Nullable1, copy.Nullable1);
            Assert.Equal(source.Nullable2, copy.Nullable2);
            Assert.Equal(source.Bool, copy.Bool);
            Assert.Equal(source.C, copy.C);
            Assert.Equal(source.B, copy.B);
            Assert.Equal(source.SB, copy.SB);
            Assert.Equal(source.F, copy.F);
            Assert.Equal(source.D, copy.D);
            Assert.Equal(source.U16, copy.U16);
            Assert.Equal(source.I16, copy.I16);
            Assert.Equal(source.U32, copy.U32);
            Assert.Equal(source.I32, copy.I32);
            Assert.Equal(source.U64, copy.U64);
            Assert.Equal(source.I64, copy.I64);
            Assert.Equal(source.Enum, copy.Enum);
            Assert.Equal(source.Enum2, copy.Enum2);
            Assert.Equal(source.EnumNull, copy.EnumNull);
            Assert.Equal(source.String, copy.String);
            Assert.Equal(source.StringNull, copy.StringNull);
            Assert.Equal(source.Data, copy.Data);
            Assert.Equal(source.DataNull, copy.DataNull);
            Assert.Equal(source.Guid, copy.Guid);
        }

        [Theory]
        [InlineData(SerializationBackend.Binary)]
        [Description("Test serialization for collection types")]
        public void TestSerializationCollectionTypes(SerializationBackend serializationBackend)
        {
            var source = new SerializeCollectionTest
                             {
                                 List = new List<int> { 3112, 123 },
                                 ListInterface = new List<int> { 5112, 623 },
                                 ListClass = new List<SerializeClassTest> { new SerializeClassTest { A = 1, B = 2 }, new SerializeClassTest { A = 3, B = 4 } },
                                 ReadOnlyList = { 345, 567 },
                                 Dictionary = new Dictionary<string, string> { { "a", "b" }, { "c", "d" } },
                             };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.Equal(source.List, copy.List);
            Assert.Null(copy.ListNull);
            Assert.Equal(source.ListInterface, copy.ListInterface);
            Assert.Null(copy.ListInterfaceNull);
            Assert.Equal(source.ListClass.Count, copy.ListClass.Count);
            Assert.Equal(source.ReadOnlyList.Count, copy.ReadOnlyList.Count);
            for (int i = 0; i < source.ListClass.Count; ++i)
            {
                Assert.Equal(source.ListClass[i].A, copy.ListClass[i].A);
                Assert.Equal(source.ListClass[i].B, copy.ListClass[i].B);
            }

            Assert.Equal(source.Dictionary, copy.Dictionary);
            Assert.Null(copy.DictionaryNull);
        }

        [Theory]
        [InlineData(SerializationBackend.Binary)]
        [Description("Test serialization with type information")]
        public void TestSerializationType(SerializationBackend serializationBackend)
        {
            var source = new SerializeTypeTest { A = new B() };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.True(copy.A is B);
        }

        [Theory]
        [InlineData(SerializationBackend.Binary)]
        [Description("Test serialization with type information")]
        public void TestSerializationStructType(SerializationBackend serializationBackend)
        {
            var source = new SerializeTypeTest { A = new S { A = 32 } };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.True(copy.A is S);
            Assert.Equal(((S)source.A).A, ((S)copy.A).A);
        }

        struct StructWithRef
        {
            public S A { get; set; }
        }
        
        //[InlineData(SerializationBackend.Binary)]
        //[InlineData(SerializationBackend.Xml)]
        //[Description("Test serialization with type information")]
        //public void TestSerializationPropertyStructType(SerializationBackend serializationBackend)
        //{
        //    Serializer.Default.RegisterSerializer(new ComplexTypeSerializer<StructWithRef>());
        //    var source = new StructWithRef { A = new S { A = 32 } };
        //    var copy = CopyBySerialization(source, serializationBackend);
        //    Assert.IsInstanceOf<S>(copy.A);
        //    Assert.Equal(((S)source.A).A, ((S)copy.A).A);
        //}

    }
}
