// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Stride.Core;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum.Tests.Obsolete
{
    public class ObsoleteTestLists
    {
        #region Test class definitions
        public class SimpleClass
        {
            [DataMember(1)]
            public int FirstValue { get; set; }

            [DataMember(2)]
            public int SecondValue { get; set; }
        }

        public struct SimpleStruct
        {
            [DataMember(1)]
            public int FirstValue { get; set; }

            [DataMember(2)]
            public int SecondValue { get; set; }
        }

        public struct NestedStruct
        {
            [DataMember(1)]
            public SimpleStruct Struct { get; set; }
        }

        public class ClassWithLists
        {
            public ClassWithLists()
            {
                IntList = new List<int> { 1, 2, 3 };
                ClassList = new List<SimpleClass> { new SimpleClass() };
                SimpleStructList = new List<SimpleStruct> { new SimpleStruct(), new SimpleStruct() };
                NestedStructList = new List<NestedStruct> { new NestedStruct(), new NestedStruct() };
                ListOfSimpleStructLists = new List<List<SimpleStruct>> { new List<SimpleStruct> { new SimpleStruct() }, new List<SimpleStruct> { new SimpleStruct() } };
                ListOfNestedStructLists = new List<List<NestedStruct>> { new List<NestedStruct> { new NestedStruct() }, new List<NestedStruct> { new NestedStruct() } };
            }

            [DataMember(1)]
            public List<int> IntList { get; }

            [DataMember(2)]
            public List<SimpleClass> ClassList { get; }

            [DataMember(3)]
            public List<SimpleStruct> SimpleStructList { get; }

            [DataMember(4)]
            public List<NestedStruct> NestedStructList { get; }

            [DataMember(5)]
            public List<List<SimpleStruct>> ListOfSimpleStructLists { get; }

            [DataMember(6)]
            public List<List<NestedStruct>> ListOfNestedStructLists { get; }
        }

        public class ClassWithNullLists
        {
            [DataMember(1)]
            public List<int> IntList { get; set; }

            [DataMember(2)]
            public List<SimpleClass> ClassList { get; set; }

            [DataMember(3)]
            public List<SimpleStruct> SimpleStructList { get; set; }

            [DataMember(4)]
            public List<NestedStruct> NestedStructList { get; set; }

            [DataMember(5)]
            public List<List<SimpleStruct>> ListOfSimpleStructLists { get; set; }

            [DataMember(6)]
            public List<List<NestedStruct>> ListOfNestedStructLists { get; set; }
        }

        #endregion Test class definitions

        [Fact]
        public void TestConstruction()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);

            Assert.That(model["IntList"].Retrieve(), Is.SameAs(obj.IntList));
            Assert.That(model["IntList"].IsReference, Is.False);
            Assert.That(model["ClassList"].Retrieve(), Is.SameAs(obj.ClassList));
            //Assert.That(model["ClassList"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            Assert.That(model["SimpleStructList"].Retrieve(), Is.SameAs(obj.SimpleStructList));
            //Assert.That(model["SimpleStructList"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            Assert.That(model["NestedStructList"].Retrieve(), Is.SameAs(obj.NestedStructList));
            //Assert.That(model["NestedStructList"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            Assert.That(model["ListOfSimpleStructLists"].Retrieve(), Is.SameAs(obj.ListOfSimpleStructLists));
            //Assert.That(model["ListOfSimpleStructLists"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            foreach (var reference in model["ListOfSimpleStructLists"].Target.ItemReferences)
            {
                Assert.That(reference, Is.AssignableFrom(typeof(ObjectReference)));
            }
            Assert.That(model["ListOfNestedStructLists"].Retrieve(), Is.SameAs(obj.ListOfNestedStructLists));
            //Assert.That(model["ListOfNestedStructLists"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            foreach (var reference in model["ListOfNestedStructLists"].Target.ItemReferences)
            {
                Assert.That(reference, Is.AssignableFrom(typeof(ObjectReference)));
            }

            Assert.That(container.GetNode(obj.ClassList[0]), !Is.Null);
        }

        [Fact]
        public void TestPrimitiveItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            ((List<int>)model["IntList"].Retrieve())[1] = 42;
            ((List<int>)model["IntList"].Retrieve()).Add(26);
            Assert.Equal(4, obj.IntList.Count);
            Assert.Equal(42, obj.IntList[1]);
            Assert.Equal(26, obj.IntList[3]);
        }

        [Fact]
        public void TestObjectItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var objRef = ((ReferenceEnumerable)model["ClassList"].Reference).First();
            //objRef.TargetNode["SecondValue"].Update(32);
            Assert.Equal(32, obj.ClassList[0].SecondValue);
        }

        [Fact]
        public void TestStructItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var objRef = ((ReferenceEnumerable)model["SimpleStructList"].Reference).First();
            //objRef.TargetNode["SecondValue"].Update(32);
            Assert.Equal(32, obj.SimpleStructList[0].SecondValue);
        }

        [Fact]
        public void TestNestedStructItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var objRef = ((ReferenceEnumerable)model["NestedStructList"].Reference).First();
            //var structNode = container.GetNode(((ObjectReference)objRef.TargetNode["Struct"].Reference).TargetGuid);
            //structNode["SecondValue"].Update(32);
            Assert.Equal(32, obj.NestedStructList[0].Struct.SecondValue);
            //var visitor = new ModelConsistencyCheckVisitor(container.NodeBuilder);
            //visitor.Check((GraphNode)model, obj, typeof(ClassWithLists), true);
        }

        [Fact]
        public void TestListOfSimpleStructListsUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var listRef = ((ReferenceEnumerable)model["ListOfSimpleStructLists"].Reference).Last();
            //var objRef = ((ReferenceEnumerable)listRef.TargetNode.Reference).Last();
            //objRef.TargetNode["SecondValue"].Update(32);
            Assert.Equal(32, obj.ListOfSimpleStructLists[1][0].SecondValue);
        }

        [Fact]
        public void TestListOfNestedStructListsUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var listRef = ((ReferenceEnumerable)model["ListOfNestedStructLists"].Reference).Last();
            //var objRef = ((ReferenceEnumerable)listRef.TargetNode.Reference).Last();
            //var structNode = container.GetNode(((ObjectReference)objRef.TargetNode["Struct"].Reference).TargetGuid);
            //structNode["SecondValue"].Update(32);
            Assert.Equal(32, obj.ListOfNestedStructLists[1][0].Struct.SecondValue);
        }
    }
}
