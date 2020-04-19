// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if DEBUG
using System;
using System.ComponentModel;

using Stride.Core.Annotations;
using System.Collections.Generic;

using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Test
{
    public enum TestEnum
    {
        FirstValue,
        SecondValue,
        ThirdValue,
        FourthValue
    }

    [Flags]
    public enum TestFlagEnum
    {
        ZeroFlag = 0,
        FirstFlag = 1,
        SecondFlag = 2,
        ThirdFlag = 4,
        FourthFlag = 8,
        SecondAndThirdFlags = SecondFlag | ThirdFlag,
    }

    [DataContract("TestStruct")]
    public struct TestStruct
    {
        public int Integer { get; set; }
        public float Float { get; set; }
        public string String { get; set; }
    }

    [DataContract("TestClass")]
    public class TestClass
    {
        public float Float { get; set; }
        public string String { get; set; }
    }

    [DataContract("TestEnabledClass")]
    public class TestEnabledClass
    {
        public bool Enabled { get; set; }
        public float Float { get; set; }
        public string String { get; set; }
    }

    [DataContract("TestAbstractClass")]
    public abstract class TestAbstractClass
    {
        public int DefaultInt { get; set; }
    }

    [DataContract("TestImplemClass1")]
    public class TestImplemClass1 : TestAbstractClass
    {
        public string String1 { get; set; }
    }

    [DataContract("TestImplemClass2")]
    public class TestImplemClass2 : TestAbstractClass
    {
        public string String2 { get; set; }
    }

    [DataContract("TestImplemClassEnabled")]
    public class TestImplemClassEnabled : TestAbstractClass
    {
        public bool Enabled { get; set; }
    }

    [DataContract("TestInlinedAbstractClass")]
    [InlineProperty]
    public abstract class TestInlinedAbstractClass
    {
        public int DefaultInt { get; set; }
    }

    [DataContract("TestInlinedImplemClass1")]
    public class TestInlinedImplemClass1 : TestInlinedAbstractClass
    {
        [InlineProperty]
        public Vector2 Vector { get; set; }
    }

    [DataContract("TestInlinedImplemClass2")]
    public class TestInlinedImplemClass2 : TestInlinedAbstractClass
    {
        [InlineProperty]
        public Vector3 Vector { get; set; }
    }

    [DataContract("TestMissingInlinedProperty")]
    public class TestMissingInlinedProperty : TestInlinedAbstractClass
    {
        public Vector3 Vector { get; set; }
    }

    [DataContract("TestAsset")]
    [AssetDescription(FileExtension)]
    [Display("Test Asset")]
    [CategoryOrder(1, "Primitive types")]
    [CategoryOrder(2, "Vector types")]
    [CategoryOrder(3, "Class & Struct types")]
    [CategoryOrder(4, "Instanciable types")]
    [CategoryOrder(5, "Collections")]
    [CategoryOrder(6, "Categories")]
    public sealed class TestAsset : Asset
    {
        public const string FileExtension = ".sdtest";

        [DataMember(10)]
        [Display("String", "Primitive types")]
        public string String { get; set; }

        [DataMember(20)]
        [Display("Float", "Primitive types")]
        public float Size { get; set; }

        [DataMember(22)]
        [DataMemberRange(200, 2000, 10, 100, 2)]
        [Display("Constrained Float", "Primitive types")]
        public float ConstrainedFloat { get; set; }

        [DataMember(23)]
        [Display("Byte", "Primitive types")]
        public byte Byte { get; set; }

        [DataMember(25)]
        [Display("Boolean", "Primitive types")]
        public bool Boolean { get; set; }

        [DataMember(27)]
        [Display("Unsigned Short", "Primitive types")]
        public ushort UnsignedShort { get; set; }

        [DataMember(28)]
        [Display("TimeSpan", "Primitive types")]
        public TimeSpan TimeSpan { get; set; }

        [DataMember(30)]
        [Display("Enum", "Primitive types")]
        public TestEnum Enum { get; set; }

        [DataMember(31)]
        [Display("FlagEnum", "Primitive types")]
        public TestFlagEnum FlagEnum { get; set; }

        [DataMember(40)]
        [Display("Character", "Primitive types")]
        public char Character { get; set; }

        [DataMember(41)]
        [Display("FilePath", "Primitive types")]
        public UFile FilePath { get; set; } = new UFile("");

        [DataMember(41)]
        [Display("DirectoryPath", "Primitive types")]
        public UDirectory DirectoryPath { get; set; } = new UDirectory("");

        [DataMember(42)]
        [Display("Color", "Vector types")]
        public Color Color { get; set; } = new Color(0, 255, 255, 255);

        [DataMember(43)]
        [Display("Color3", "Vector types")]
        public Color3 Color3 { get; set; } = new Color3(1.0f, 1.0f, 0.0f);

        [DataMember(44)]
        [Display("Color4", "Vector types")]
        public Color4 Color4 { get; set; } = new Color4(1.0f, 0.0f, 1.0f, 1.0f);

        [DataMember(45)]
        [Display("Vector2", "Vector types")]
        public Vector2 Vector2 { get; set; }

        [DataMember(46)]
        [Display("Vector3", "Vector types")]
        public Vector3 Vector3 { get; set; }

        [DataMember(47)]
        [Display("Vector4", "Vector types")]
        public Vector4 Vector4 { get; set; }

        [DataMember(48)]
        [Display("Int2", "Int types")]
        public Int2 Int2 { get; set; }

        [DataMember(49)]
        [Display("Int3", "Int types")]
        public Int3 Int3 { get; set; }

        [DataMember(50)]
        [Display("Int4", "Int types")]
        public Int4 Int4 { get; set; }

        [DataMember(51)]
        [Display("RectangleF", "Vector types")]
        public RectangleF RectangleF { get; set; }

        [DataMember(52)]
        [Display("Rectangle", "Vector types")]
        public Rectangle Rectangle { get; set; }

        [DataMember(53)]
        [Display("Angle", "Vector types")]
        public AngleSingle Angle { get; set; }

        [DataMember(54)]
        [Display("Rotation", "Vector types")]
        public Quaternion Rotation { get; set; }

        [DataMember(55)]
        [Display("Matrix", "Vector types")]
        public Matrix Matrix { get; set; }

        // TODO: Add ImageEnum and ImageFlagEnum
        [DataMember(56)]
        [Display("Nullable Enum", "Class & Struct types")]
        public TestEnum? NullableEnum { get; set; }

        [DataMember(57)]
        [Display("Nullable Rectangle", "Class & Struct types")]
        public Rectangle? NullableRectangle { get; set; }

        [DataMember(60)]
        [Display("Structure", "Class & Struct types")]
        public TestStruct Structure { get; set; }

        [DataMember(70)]
        [Display("Class", "Class & Struct types")]
        public TestClass Class { get; set; } = new TestClass();

        [DataMember(80)]
        [Display("Enabled Class", "Class & Struct types")]
        public TestEnabledClass EnabledClass { get; set; } = new TestEnabledClass();

        [DataMember(100)]
        [Display("Abstract Class", "Instanciable types")]
        public TestAbstractClass AbstractClass { get; set; }

        [DataMember(110)]
        [Display("Inlined Abstract Class", "Instanciable types")]
        public TestInlinedAbstractClass InlinedAbstractClass { get; set; }

        [DataMember(120)]
        [Display("ReadOnly Abstract Class", "Instanciable types")]
        public TestAbstractClass ReadOnlyAbstractClass { get; } = new TestImplemClass1 { DefaultInt = 5, String1 = "TestImplem1" };

        [DataMember(130)]
        [Display("ReadOnly Abstract Class (Null)", "Instanciable types")]
        public TestAbstractClass ReadOnlyAbstractClassNull { get; } = null;

        [DataMember(140)]
        [Display("Inlined ReadOnly Abstract Class", "Instanciable types")]
        public TestInlinedAbstractClass ReadOnlyInlinedAbstractClass { get; } = new TestInlinedImplemClass1 { DefaultInt = 5, Vector = Vector2.One };

        [DataMember(150)]
        [Display("Inlined ReadOnly Abstract Class (Null)", "Instanciable types")]
        public TestInlinedAbstractClass ReadOnlyInlinedAbstractClassNull { get; } = null;

        [DataMember(200)]
        [Display("Integer List", "Collections")]
        public List<int> IntegerList { get; } = new List<int>();

        [DataMember(210)]
        [Display("Structure List", "Collections")]
        public List<TestStruct> StructureList { get; set; } = new List<TestStruct>();

        [DataMember(220)]
        [Display("Vector List", "Collections")]
        public List<Vector3> VectorList { get; set; } = new List<Vector3>();

        [DataMember(230)]
        [Display("Class List", "Collections")]
        public List<TestClass> ClassList { get; set; } = new List<TestClass>();

        [DataMember(240)]
        [Display("Abstract List", "Collections")]
        public List<TestAbstractClass> AbstractList { get; set; } = new List<TestAbstractClass>();

        //[DataMember(250)]
        //[Display("List of Integer Lists", "Collections")]
        //public List<List<int>> ListOfListsInt { get; set; } = new List<List<int>>();

        //[DataMember(260)]
        //[Display("List of Struct Lists", "Collections")]
        //public List<List<TestStruct>> ListOfListsStruct { get; set; } = new List<List<TestStruct>>();

        //[DataMember(270)]
        //[Display("List of Class Lists", "Collections")]
        //public List<List<TestClass>> ListOfListsClass { get; set; } = new List<List<TestClass>>();

        [DataMember(275)]
        [MemberCollection(ReadOnly = true)]
        [Display("ReadOnly Abstract List", "Collections")]
        public List<TestAbstractClass> ReadOnlyAbstractList { get; private set; } = new List<TestAbstractClass>
        {
            new TestImplemClass1 { String1 = "Item1", DefaultInt = 1 },
            new TestImplemClass2 { String2 = "Item2", DefaultInt = 2 },
            new TestImplemClassEnabled { DefaultInt = 3, Enabled = true }
        };

        [DataMember(280)]
        [Display("Integer Dictionary", "Collections")]
        public Dictionary<string, int> IntegerDictionary { get; set; } = new Dictionary<string, int>();

        [DataMember(290)]
        [Display("Class Dictionary", "Collections")]
        public Dictionary<string, TestClass> ClassDictionary { get; set; } = new Dictionary<string, TestClass>();

        [DataMember(300)]
        [Display("Abstract Dictionary", "Collections")]
        public Dictionary<string, TestAbstractClass> AbstractDictionary { get; set; } = new Dictionary<string, TestAbstractClass>();

        [Display("Category", "Categories")]
        [Category]
        public TestClass CategoryClass { get; set; } = new TestClass();

        [Display("Enabled Category", "Categories")]
        [Category]
        public TestEnabledClass EnabledCategory { get; set; } = new TestEnabledClass();

        //[DataMember(800)]
        //public PropertyKey PropertyKey { get; set; }

        [DataMember(850)]
        public ParameterKey ParameterKey { get; set; }

        public static TestAsset CreateNew()
        {
            var testAsset = new TestAsset
            {
                String = "The name",
                Enum = TestEnum.ThirdValue,
                Character = 'f',
                Size = 16.5f,
                IntegerList = { 4, 6, 8 },
                Structure = new TestStruct { Float = 1.0f, Integer = 2, String = "Inner string" },
                StructureList =
                {
                    new TestStruct { Float = 2.0f, Integer = 1, String = "Inner string1" },
                    new TestStruct { Float = 4.0f, Integer = 3, String = "Inner string2" },
                    new TestStruct { Float = 8.0f, Integer = 6, String = "Inner string3" },
                },
                Class = new TestClass { Float = 8.0f, String = "Inner class string" },
                ClassList =
                {
                    new TestClass { Float = 18.0f, String = "Inner class string1" },
                    new TestClass { Float = 28.0f, String = "Inner class string2" },
                    new TestClass { Float = 38.0f, String = "Inner class string3" },
                },
                ClassDictionary =
                {
                    { "First", new TestClass { Float = 21.0f, String = "Inner class string1" } },
                    { "Second", new TestClass { Float = 31.0f, String = "Inner class string2" } },
                    { "Third", new TestClass { Float = 41.0f, String = "Inner class string3" } },
                },
                //ListOfListsInt =
                //{
                //    new List<int> { 2, 4 },
                //    new List<int> { 6, 8 }
                //},
                //ListOfListsStruct =
                //{
                //    new List<TestStruct> { new TestStruct() },
                //    new List<TestStruct> { new TestStruct() }
                //},
                //ListOfListsClass =
                //{
                //    new List<TestClass> { new TestClass() },
                //    new List<TestClass> { new TestClass() }
                //}
            };

            return testAsset;
        }
    }
}

#endif

