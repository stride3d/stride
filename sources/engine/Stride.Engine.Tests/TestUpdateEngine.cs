// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Updater;
using Stride.Rendering;

namespace Stride.Engine.Tests
{
    public class TestUpdateEngine
    {
        [Fact]
        public void TestIntField()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntField", 0),
            };

            var blittableData = new TestData[] { 123 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.IntField);
        }

        [Fact]
        public void TestIntProperty()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntProperty", 0),
            };

            var blittableData = new TestData[] { 123 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.IntProperty);
        }

        [Fact]
        public void TestObjectField()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("ObjectField", 0),
            };

            var blittableData = new TestData[0];
            var objectData = new[] { new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(test2, test.ObjectField);
        }

        [Fact]
        public void TestObjectProperty()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("ObjectProperty", 0),
            };

            var blittableData = new TestData[0];
            var objectData = new[] { new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(test2, test.ObjectProperty);
        }

        [Fact]
        public void TestCastQualifiedName()
        {
            var test = new TestClass()
            {
                ObjectField = new TestClass(),
                ObjectProperty = new TestClass(),
            };

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("ObjectField.(Stride.Engine.Tests.TestClass,Stride.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("ObjectProperty.(Stride.Engine.Tests.TestClass,Stride.Engine.Tests).IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, ((TestClass)test.ObjectField).IntField);
            Assert.Equal(456, ((TestClass)test.ObjectProperty).IntField);
        }


        [Fact]
        public void TestIntArray()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntArray[0]", 0),
                new UpdateMemberInfo("IntArray[2]", 0),
                new UpdateMemberInfo("IntArray[3]", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.IntArray[0]);
            Assert.Equal(0, test.IntArray[1]);
            Assert.Equal(123, test.IntArray[2]);
            Assert.Equal(456, test.IntArray[3]);
        }

        [Fact]
        public void TestIntList()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntList[0]", 0),
                new UpdateMemberInfo("IntList[2]", 0),
                new UpdateMemberInfo("IntList[3]", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.IntList[0]);
            Assert.Equal(0, test.IntList[1]);
            Assert.Equal(123, test.IntList[2]);
            Assert.Equal(456, test.IntList[3]);
        }

        [Fact]
        public void TestBlittableStruct()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("BlittableStructField.IntField", 0),
                new UpdateMemberInfo("BlittableStructField.IntProperty", 8),
                new UpdateMemberInfo("BlittableStructProperty.IntField", 0),
                new UpdateMemberInfo("BlittableStructProperty.IntProperty", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.BlittableStructField.IntField);
            Assert.Equal(456, test.BlittableStructField.IntProperty);
            Assert.Equal(123, test.BlittableStructProperty.IntField);
            Assert.Equal(456, test.BlittableStructProperty.IntProperty);
        }

        [Fact]
        public void TestNonBlittableStruct()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("NonBlittableStructField.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassProperty", 0),
            };

            var blittableData = new TestData[0];
            var objectData = new[] { new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(test2, test.NonBlittableStructField.TestClassField);
            Assert.Equal(test2, test.NonBlittableStructField.TestClassProperty);
            Assert.Equal(test2, test.NonBlittableStructProperty.TestClassField);
            Assert.Equal(test2, test.NonBlittableStructProperty.TestClassProperty);
        }

        [Fact]
        public void TestTestClassArray()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("TestClassArray[0]", 0),
                new UpdateMemberInfo("TestClassArray[0].IntField", 0),
                new UpdateMemberInfo("TestClassArray[1]", 1),
                new UpdateMemberInfo("TestClassArray[1].IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new[] { new UpdateObjectData(test), new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(test, test.TestClassArray[0]);
            Assert.Equal(123, test.TestClassArray[0].IntField);
            Assert.Equal(test2, test.TestClassArray[1]);
            Assert.Equal(456, test.TestClassArray[1].IntField);
        }

        [Fact]
        public void TestTestClassList()
        {
            UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<TestClass>());

            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("TestClassList[0]", 0),
                new UpdateMemberInfo("TestClassList[0].IntField", 0),
                new UpdateMemberInfo("TestClassList[1]", 1),
                new UpdateMemberInfo("TestClassList[1].IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new[] { new UpdateObjectData(test), new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(test, test.TestClassList[0]);
            Assert.Equal(123, test.TestClassList[0].IntField);
            Assert.Equal(test2, test.TestClassList[1]);
            Assert.Equal(456, test.TestClassList[1].IntField);
        }

        [Fact]
        public void TestManyProperties()
        {
            var test = new TestClass();
            var test2 = new TestClass();
            var test3 = new TestClass();

            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<TestClass>());

            // Combine many of the other tests in one, to easily test if switching from one member to another works well
            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntField", 0),
                new UpdateMemberInfo("IntProperty", 8),
                new UpdateMemberInfo("NonBlittableStructField.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassProperty", 0),
                new UpdateMemberInfo("ObjectField", 0),
                new UpdateMemberInfo("ObjectProperty", 0),
                new UpdateMemberInfo("IntArray[0]", 0),
                new UpdateMemberInfo("IntArray[2]", 0),
                new UpdateMemberInfo("IntArray[3]", 8),
                new UpdateMemberInfo("IntList[0]", 0),
                new UpdateMemberInfo("IntList[2]", 0),
                new UpdateMemberInfo("IntList[3]", 8),
                new UpdateMemberInfo("TestClassArray[0]", 0),
                new UpdateMemberInfo("TestClassArray[0].IntField", 0),
                new UpdateMemberInfo("TestClassArray[1]", 1),
                new UpdateMemberInfo("TestClassArray[1].IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new[] { new UpdateObjectData(test2), new UpdateObjectData(test3) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.IntField);
            Assert.Equal(456, test.IntProperty);
            Assert.Equal(test2, test.NonBlittableStructField.TestClassField);
            Assert.Equal(test2, test.NonBlittableStructField.TestClassProperty);
            Assert.Equal(test2, test.NonBlittableStructProperty.TestClassField);
            Assert.Equal(test2, test.NonBlittableStructProperty.TestClassProperty);
            Assert.Equal(test2, test.ObjectField);
            Assert.Equal(test2, test.ObjectProperty);
            Assert.Equal(123, test.IntArray[0]);
            Assert.Equal(0, test.IntArray[1]);
            Assert.Equal(123, test.IntArray[2]);
            Assert.Equal(456, test.IntArray[3]);
            Assert.Equal(123, test.IntList[0]);
            Assert.Equal(0, test.IntList[1]);
            Assert.Equal(123, test.IntList[2]);
            Assert.Equal(456, test.IntList[3]);
            Assert.Equal(test2, test.TestClassArray[0]);
            Assert.Equal(123, test.TestClassArray[0].IntField);
            Assert.Equal(test3, test.TestClassArray[1]);
            Assert.Equal(456, test.TestClassArray[1].IntField);
        }

        [Fact]
        public void TestNullSkip()
        {
            var test = new TestClass { IntList = null, IntArray = null };

            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<TestClass>());

            // Combine many of the other tests in one, to easily test if switching from one member to another works well
            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("NonBlittableStructField.TestClassField.IntField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassField.IntProperty", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty.IntField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty.IntProperty", 0),
                new UpdateMemberInfo("ObjectField.(Stride.Engine.Tests.TestClass,Stride.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("ObjectProperty.(Stride.Engine.Tests.TestClass,Stride.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("IntArray[0]", 0),
                new UpdateMemberInfo("IntArray[2]", 0),
                new UpdateMemberInfo("IntArray[3]", 0),
                new UpdateMemberInfo("IntField", 0),
                new UpdateMemberInfo("IntList[0]", 0),
                new UpdateMemberInfo("IntList[2]", 0),
                new UpdateMemberInfo("IntList[3]", 0),
                new UpdateMemberInfo("TestClassArray[0].IntField", 0),
                new UpdateMemberInfo("TestClassArray[1].IntField", 0),
                new UpdateMemberInfo("IntProperty", 0),
            };

            var blittableData = new TestData[] { 123 };

            // Just check that it doesn't crash and some set are properly done
            RunUpdateEngine(test, updateMemberInfo, blittableData, null);

            Assert.Equal(123, test.IntField);
            Assert.Equal(123, test.IntProperty);

            // Also try with null array
            test.TestClassArray = null;
            blittableData[0] = 456;
            RunUpdateEngine(test, updateMemberInfo, blittableData, null);
            Assert.Equal(456, test.IntField);
            Assert.Equal(456, test.IntProperty);
        }

        [Fact]
        public void TestOutOfBoundsSkip()
        {
            var test = new TestClass
            {
                TestClassArray =
                {
                    [0] = new TestClass(),
                    [1] = new TestClass()
                },
                TestClassList =
                {
                    [0] = new TestClass(),
                    [1] = new TestClass()
                }
            };

            // Check that ctor of TestClass properly initialized size of array/list to 4 (this test relies on it)
            Assert.Equal(4, test.IntArray.Length);
            Assert.Equal(4, test.IntList.Count);
            Assert.Equal(2, test.TestClassArray.Length);
            Assert.Equal(2, test.TestClassList.Count);

            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<int>());

            // Combine many of the other tests in one, to easily test if switching from one member to another works well
            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntArray[0]", 0),
                new UpdateMemberInfo("IntArray[4]", 0),
                new UpdateMemberInfo("IntList[0]", 0),
                new UpdateMemberInfo("IntList[4]", 0),
                new UpdateMemberInfo("TestClassArray[0].IntField", 0),
                new UpdateMemberInfo("TestClassArray[2].IntField", 0),
                new UpdateMemberInfo("TestClassList[0].IntField", 0),
                new UpdateMemberInfo("TestClassList[2].IntField", 0),
            };

            var blittableData = new TestData[] { 123 };

            // This shouldn't crash
            RunUpdateEngine(test, updateMemberInfo, blittableData, null);

            // Update shouldn't have been done (we skip the whole stuff if it goes out of bound)
            Assert.Equal(0, test.IntArray[0]);
            Assert.Equal(0, test.IntList[0]);
            Assert.Equal(0, test.TestClassArray[0].IntField);
            Assert.Equal(0, test.TestClassList[0].IntField);
        }

        [Fact]
        public void TestInterfaceProperty()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("TestInterface.InterfaceMember", 0),
            };

            var blittableData = new TestData[] { 123 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.TestInterface.InterfaceMember);
        }

        [Fact]
        public void TestAbstractProperty()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntPropertyAbstract", 0),
            };

            var blittableData = new TestData[] { 123 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.Equal(123, test.IntPropertyAbstract);

            // Test again with using TestClassBase type
            test.IntPropertyAbstract = 0;
            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData, typeof(TestClassBase));

            Assert.Equal(123, test.IntPropertyAbstract);
        }

        internal static unsafe void RunUpdateEngine(object test, List<UpdateMemberInfo> updateMemberInfo, TestData[] blittableData, UpdateObjectData[] objectData, Type typeOverride = null)
        {
            var compiledUpdate = UpdateEngine.Compile(typeOverride ?? test.GetType(), updateMemberInfo);

            fixed (TestData* dataPtr = blittableData)
            {
                UpdateEngine.Run(test, compiledUpdate, (IntPtr)dataPtr, objectData);
            }
        }

        internal struct TestData
        {
            public float Factor;
            public int Value;

            public static implicit operator TestData(int value)
            {
                return new TestData { Factor = 1.0f, Value = value };
            }
        }
    }

    [DataContract]
    public struct NonBlittableStruct
    {
        public TestClass TestClassField;
        public TestClass TestClassProperty { get; set; }
    }

    [DataContract]
    public struct BlittableStruct
    {
        public int IntField;
        public int IntProperty { get; set; }
    }

    public interface ITestInterface
    {
        int InterfaceMember { get; set; }
    }

    [DataContract]
    public class TestInterfaceImpl : ITestInterface
    {
        public int InterfaceMember { get; set; }
    }

    [DataContract]
    public abstract class TestClassBase
    {
        public abstract int IntPropertyAbstract { get; set; }
    }

    [DataContract]
    public class TestClass : TestClassBase
    {
        public int IntField;
        public int IntProperty { get; set; }

        public object ObjectField;
        public object ObjectProperty { get; set; }

        public BlittableStruct BlittableStructField;
        public BlittableStruct BlittableStructProperty { get; set; }

        public NonBlittableStruct NonBlittableStructField;
        public NonBlittableStruct NonBlittableStructProperty { get; set; }
        public IList<int> NonBlittableStructList;

        public int[] IntArray;
        public TestClass[] TestClassArray;

        public IList<int> IntList;
        public IList<TestClass> TestClassList;

        public ITestInterface TestInterface = new TestInterfaceImpl();

        public override int IntPropertyAbstract { get; set; }

        public TestClass()
        {
            IntArray = new int[4];
            IntList = new List<int> { 0, 0, 0, 0 };
            TestClassArray = new TestClass[2];
            TestClassList = new List<TestClass> { null, null };
        }
    }

}
