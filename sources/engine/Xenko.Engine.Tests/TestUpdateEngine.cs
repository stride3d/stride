// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Xenko.Core;
using Xenko.Engine.Design;
using Xenko.Updater;
using Xenko.Rendering;

namespace Xenko.Engine.Tests
{
    [TestFixture]
    public class TestUpdateEngine
    {
        [Test]
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

            Assert.That(test.IntField, Is.EqualTo(123));
        }

        [Test]
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

            Assert.That(test.IntProperty, Is.EqualTo(123));
        }

        [Test]
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

            Assert.That(test.ObjectField, Is.EqualTo(test2));
        }

        [Test]
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

            Assert.That(test.ObjectProperty, Is.EqualTo(test2));
        }

        [Test]
        public void TestCastQualifiedName()
        {
            var test = new TestClass()
            {
                ObjectField = new TestClass(),
                ObjectProperty = new TestClass(),
            };

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("ObjectField.(Xenko.Engine.Tests.TestClass,Xenko.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("ObjectProperty.(Xenko.Engine.Tests.TestClass,Xenko.Engine.Tests).IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(((TestClass)test.ObjectField).IntField, Is.EqualTo(123));
            Assert.That(((TestClass)test.ObjectProperty).IntField, Is.EqualTo(456));
        }


        [Test]
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

            Assert.That(test.IntArray[0], Is.EqualTo(123));
            Assert.That(test.IntArray[1], Is.EqualTo(0));
            Assert.That(test.IntArray[2], Is.EqualTo(123));
            Assert.That(test.IntArray[3], Is.EqualTo(456));
        }

        [Test]
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

            Assert.That(test.IntList[0], Is.EqualTo(123));
            Assert.That(test.IntList[1], Is.EqualTo(0));
            Assert.That(test.IntList[2], Is.EqualTo(123));
            Assert.That(test.IntList[3], Is.EqualTo(456));
        }

        [Test]
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

            Assert.That(test.BlittableStructField.IntField, Is.EqualTo(123));
            Assert.That(test.BlittableStructField.IntProperty, Is.EqualTo(456));
            Assert.That(test.BlittableStructProperty.IntField, Is.EqualTo(123));
            Assert.That(test.BlittableStructProperty.IntProperty, Is.EqualTo(456));
        }

        [Test]
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

            Assert.That(test.NonBlittableStructField.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructField.TestClassProperty, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassProperty, Is.EqualTo(test2));
        }

        [Test]
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

            Assert.That(test.TestClassArray[0], Is.EqualTo(test));
            Assert.That(test.TestClassArray[0].IntField, Is.EqualTo(123));
            Assert.That(test.TestClassArray[1], Is.EqualTo(test2));
            Assert.That(test.TestClassArray[1].IntField, Is.EqualTo(456));
        }

        [Test]
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

            Assert.That(test.TestClassList[0], Is.EqualTo(test));
            Assert.That(test.TestClassList[0].IntField, Is.EqualTo(123));
            Assert.That(test.TestClassList[1], Is.EqualTo(test2));
            Assert.That(test.TestClassList[1].IntField, Is.EqualTo(456));
        }

        [Test]
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

            Assert.That(test.IntField, Is.EqualTo(123));
            Assert.That(test.IntProperty, Is.EqualTo(456));
            Assert.That(test.NonBlittableStructField.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructField.TestClassProperty, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassProperty, Is.EqualTo(test2));
            Assert.That(test.ObjectField, Is.EqualTo(test2));
            Assert.That(test.ObjectProperty, Is.EqualTo(test2));
            Assert.That(test.IntArray[0], Is.EqualTo(123));
            Assert.That(test.IntArray[1], Is.EqualTo(0));
            Assert.That(test.IntArray[2], Is.EqualTo(123));
            Assert.That(test.IntArray[3], Is.EqualTo(456));
            Assert.That(test.IntList[0], Is.EqualTo(123));
            Assert.That(test.IntList[1], Is.EqualTo(0));
            Assert.That(test.IntList[2], Is.EqualTo(123));
            Assert.That(test.IntList[3], Is.EqualTo(456));
            Assert.That(test.TestClassArray[0], Is.EqualTo(test2));
            Assert.That(test.TestClassArray[0].IntField, Is.EqualTo(123));
            Assert.That(test.TestClassArray[1], Is.EqualTo(test3));
            Assert.That(test.TestClassArray[1].IntField, Is.EqualTo(456));
        }

        [Test]
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
                new UpdateMemberInfo("ObjectField.(Xenko.Engine.Tests.TestClass,Xenko.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("ObjectProperty.(Xenko.Engine.Tests.TestClass,Xenko.Engine.Tests).IntField", 0),
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

            Assert.That(test.IntField, Is.EqualTo(123));
            Assert.That(test.IntProperty, Is.EqualTo(123));

            // Also try with null array
            test.TestClassArray = null;
            blittableData[0] = 456;
            RunUpdateEngine(test, updateMemberInfo, blittableData, null);
            Assert.That(test.IntField, Is.EqualTo(456));
            Assert.That(test.IntProperty, Is.EqualTo(456));
        }

        [Test]
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
            Assert.That(test.IntArray.Length, Is.EqualTo(4));
            Assert.That(test.IntList.Count, Is.EqualTo(4));
            Assert.That(test.TestClassArray.Length, Is.EqualTo(2));
            Assert.That(test.TestClassList.Count, Is.EqualTo(2));

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
            Assert.That(test.IntArray[0], Is.EqualTo(0));
            Assert.That(test.IntList[0], Is.EqualTo(0));
            Assert.That(test.TestClassArray[0].IntField, Is.EqualTo(0));
            Assert.That(test.TestClassList[0].IntField, Is.EqualTo(0));
        }

        internal static unsafe void RunUpdateEngine(object test, List<UpdateMemberInfo> updateMemberInfo, TestData[] blittableData, UpdateObjectData[] objectData)
        {
            var compiledUpdate = UpdateEngine.Compile(test.GetType(), updateMemberInfo);

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

    [DataContract]
    public class TestClass
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

        public TestClass()
        {
            IntArray = new int[4];
            IntList = new List<int> { 0, 0, 0, 0 };
            TestClassArray = new TestClass[2];
            TestClassList = new List<TestClass> { null, null };
        }
    }

}
