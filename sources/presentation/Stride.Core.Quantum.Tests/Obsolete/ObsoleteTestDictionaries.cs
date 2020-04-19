// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Stride.Core;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum.Tests.Obsolete
{
    class ObsoleteTestDictionaries
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

        public class ClassWithDictionaries
        {
            public ClassWithDictionaries()
            {
                StringIntDic = new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } };
                StringClassDic = new Dictionary<string, SimpleClass> { { "a", new SimpleClass() }, { "b", new SimpleClass() } };
                // TODO: test with primitive struct
                // TODO: test with nested struct
                //SimpleStructList = new List<SimpleStruct> { new SimpleStruct(), new SimpleStruct() };
                //NestedStructList = new List<NestedStruct> { new NestedStruct(), new NestedStruct() };
                //ListOfSimpleStructLists = new List<List<SimpleStruct>> { new List<SimpleStruct> { new SimpleStruct() }, new List<SimpleStruct> { new SimpleStruct() } };
                //ListOfNestedStructLists = new List<List<NestedStruct>> { new List<NestedStruct> { new NestedStruct() }, new List<NestedStruct> { new NestedStruct() } };
            }

            [DataMember(1)]
            public Dictionary<string, int> StringIntDic { get; private set; }

            [DataMember(2)]
            public Dictionary<string, SimpleClass> StringClassDic { get; private set; }

            //[DataMember(3)]
            //public List<SimpleStruct> SimpleStructList { get; private set; }

            //[DataMember(4)]
            //public List<NestedStruct> NestedStructList { get; private set; }

            //[DataMember(5)]
            //public List<List<SimpleStruct>> ListOfSimpleStructLists { get; private set; }

            //[DataMember(6)]
            //public List<List<NestedStruct>> ListOfNestedStructLists { get; private set; }
        }
        #endregion Test class definitions

        [Fact]
        public void TestConstruction()
        {
            var obj = new ClassWithDictionaries();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);

            Assert.That(model["StringIntDic"].Retrieve(), Is.SameAs(obj.StringIntDic));
            Assert.That(model["StringIntDic"].IsReference, Is.False);
            Assert.That(model["StringClassDic"].Retrieve(), Is.SameAs(obj.StringClassDic));
            //Assert.That(model["StringClassDic"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            var enumerator = obj.StringClassDic.GetEnumerator();
            foreach (var reference in model["StringClassDic"].Target.ItemReferences)
            {
                enumerator.MoveNext();
                var keyValuePair = enumerator.Current;
                Assert.Equal(keyValuePair.Key, reference.Index);
                Assert.Equal(keyValuePair.Value, reference.ObjectValue);
            }
            //Assert.Equal(0, model.GetChild("SimpleStructList").Children.Count);
            //Assert.That(model.GetChild("SimpleStructList").Content.Value, Is.SameAs(obj.SimpleStructList));
            //Assert.That(model.GetChild("SimpleStructList").Content.Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            //Assert.Equal(0, model.GetChild("NestedStructList").Children.Count);
            //Assert.That(model.GetChild("NestedStructList").Content.Value, Is.SameAs(obj.NestedStructList));
            //Assert.That(model.GetChild("NestedStructList").Content.Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            //Assert.Equal(0, model.GetChild("ListOfSimpleStructLists").Children.Count);
            //Assert.That(model.GetChild("ListOfSimpleStructLists").Content.Value, Is.SameAs(obj.ListOfSimpleStructLists));
            //Assert.That(model.GetChild("ListOfSimpleStructLists").Content.Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            //foreach (var reference in (ReferenceEnumerable)model.GetChild("ListOfSimpleStructLists").Content.Reference)
            //{
            //    Assert.That(reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            //}
            //Assert.Equal(0, model.GetChild("ListOfNestedStructLists").Children.Count);
            //Assert.That(model.GetChild("ListOfNestedStructLists").Content.Value, Is.SameAs(obj.ListOfNestedStructLists));
            //Assert.That(model.GetChild("ListOfNestedStructLists").Content.Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            //foreach (var reference in (ReferenceEnumerable)model.GetChild("ListOfNestedStructLists").Content.Reference)
            //{
            //    Assert.That(reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            //}

            //Assert.That(container.GetNode(obj.ClassList[0]), !Is.Null);
            //Assert.Equal(10, container.Guids.Count());
        }

        [Fact]
        public void TestPrimitiveItemUpdate()
        {
            var obj = new ClassWithDictionaries();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            ((Dictionary<string, int>)model["StringIntDic"].Retrieve())["b"] = 42;
            ((Dictionary<string, int>)model["StringIntDic"].Retrieve()).Add("d", 26);
            Assert.Equal(4, obj.StringIntDic.Count);
            Assert.Equal(42, obj.StringIntDic["b"]);
            Assert.Equal(26, obj.StringIntDic["d"]);
        }

    }
}
