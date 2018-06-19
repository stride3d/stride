// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using Xenko.Core.Serialization;
using NUnit.Framework;

namespace Xenko.Core.Tests
{
    [TestFixture]
    [Description("Tests serialization")]
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

        //[TestCase(SerializationBackend.Binary)]
        //[TestCase(SerializationBackend.Xml)]
        //[Description("Test struct serialization of field and properties (using ComplexTypeSerializer)")]
        //public void TestSerializationComplexTypeStruct(SerializationBackend serializationBackend)
        //{
        //    Serializer.Default.RegisterSerializer(new ComplexTypeSerializer<SerializeStructTest> { Flags = ComplexTypeSerializerFlags.SerializePublicProperties | ComplexTypeSerializerFlags.SerializePublicFields });
        //    var source = new SerializeStructTest { A = 32, B = 123 };
        //    var copy = CopyBySerialization(source, serializationBackend);
        //    Assert.AreEqual(source.A, copy.A);
        //    Assert.AreEqual(source.B, copy.B);
        //}

        [TestCase(SerializationBackend.Binary)]
        [Description("Test class serialization of field and properties (using ComplexTypeSerializer)")]
        public void TestSerializationComplexTypeClass(SerializationBackend serializationBackend)
        {
            var source = new SerializeClassTest { A = 32, B = 123 };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.That(copy.A, Is.EqualTo(source.A));
            Assert.That(copy.B, Is.EqualTo(source.B));
        }

        [TestCase(SerializationBackend.Binary)]
        [Description("Test class serialization of field and properties (using ComplexTypeSerializer)")]
        public void TestSerializationList(SerializationBackend serializationBackend)
        {
            var source = new List<int> { 32, 12, 15 };
            var copy = CopyBySerialization(source, serializationBackend);

            Assert.That(copy.Count, Is.EqualTo(source.Count));
            for (int i = 0; i < source.Count; ++i)
                Assert.That(copy[i], Is.EqualTo(source[i]));
        }

        [TestCase(SerializationBackend.Binary)]
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
            Assert.That(copy.Nullable1, Is.EqualTo(source.Nullable1));
            Assert.That(copy.Nullable2, Is.EqualTo(source.Nullable2));
            Assert.That(copy.Bool, Is.EqualTo(source.Bool));
            Assert.That(copy.C, Is.EqualTo(source.C));
            Assert.That(copy.B, Is.EqualTo(source.B));
            Assert.That(copy.SB, Is.EqualTo(source.SB));
            Assert.That(copy.F, Is.EqualTo(source.F));
            Assert.That(copy.D, Is.EqualTo(source.D));
            Assert.That(copy.U16, Is.EqualTo(source.U16));
            Assert.That(copy.I16, Is.EqualTo(source.I16));
            Assert.That(copy.U32, Is.EqualTo(source.U32));
            Assert.That(copy.I32, Is.EqualTo(source.I32));
            Assert.That(copy.U64, Is.EqualTo(source.U64));
            Assert.That(copy.I64, Is.EqualTo(source.I64));
            Assert.That(copy.Enum, Is.EqualTo(source.Enum));
            Assert.That(copy.Enum2, Is.EqualTo(source.Enum2));
            Assert.That(copy.EnumNull, Is.EqualTo(source.EnumNull));
            Assert.That(copy.String, Is.EqualTo(source.String));
            Assert.That(copy.StringNull, Is.EqualTo(source.StringNull));
            Assert.That(copy.Data, Is.EqualTo(source.Data));
            Assert.That(copy.DataNull, Is.EqualTo(source.DataNull));
            Assert.That(copy.Guid, Is.EqualTo(source.Guid));
        }

        [TestCase(SerializationBackend.Binary)]
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
            Assert.That(copy.List, Is.EqualTo(source.List));
            Assert.Null(copy.ListNull);
            Assert.That(copy.ListInterface, Is.EqualTo(source.ListInterface));
            Assert.Null(copy.ListInterfaceNull);
            Assert.That(copy.ListClass.Count, Is.EqualTo(source.ListClass.Count));
            Assert.That(copy.ReadOnlyList.Count, Is.EqualTo(source.ReadOnlyList.Count));
            for (int i = 0; i < source.ListClass.Count; ++i)
            {
                Assert.That(copy.ListClass[i].A, Is.EqualTo(source.ListClass[i].A));
                Assert.That(copy.ListClass[i].B, Is.EqualTo(source.ListClass[i].B));
            }

            Assert.That(copy.Dictionary, Is.EqualTo(source.Dictionary));
            Assert.Null(copy.DictionaryNull, null);
        }

        [TestCase(SerializationBackend.Binary)]
        [Description("Test serialization with type information")]
        public void TestSerializationType(SerializationBackend serializationBackend)
        {
            var source = new SerializeTypeTest { A = new B() };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.That(copy.A, Is.InstanceOf(typeof(B)));
        }

        [TestCase(SerializationBackend.Binary)]
        [Description("Test serialization with type information")]
        public void TestSerializationStructType(SerializationBackend serializationBackend)
        {
            var source = new SerializeTypeTest { A = new S { A = 32 } };
            var copy = CopyBySerialization(source, serializationBackend);
            Assert.That(copy.A, Is.InstanceOf(typeof(S)));
            Assert.That(((S)source.A).A, Is.EqualTo(((S)copy.A).A));
        }

        struct StructWithRef
        {
            public S A { get; set; }
        }
        
        //[TestCase(SerializationBackend.Binary)]
        //[TestCase(SerializationBackend.Xml)]
        //[Description("Test serialization with type information")]
        //public void TestSerializationPropertyStructType(SerializationBackend serializationBackend)
        //{
        //    Serializer.Default.RegisterSerializer(new ComplexTypeSerializer<StructWithRef>());
        //    var source = new StructWithRef { A = new S { A = 32 } };
        //    var copy = CopyBySerialization(source, serializationBackend);
        //    Assert.IsInstanceOf<S>(copy.A);
        //    Assert.AreEqual(((S)source.A).A, ((S)copy.A).A);
        //}

    }
}
