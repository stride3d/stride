// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Xenko.Core.Annotations;

namespace Xenko.Core.Quantum.Tests
{
    [TestFixture]
    public class TestGraphNodeChangeListener
    {
        public class SimpleClass
        {
            public int Member1;
            public SimpleClass Member2;
        }

        public class ComplexClass
        {
            public int Member1;
            public SimpleClass Member2;
            public object Member3;
            public Struct Member4;
            public List<string> Member5;
            public List<SimpleClass> Member6;
            public List<Struct> Member7;
        }

        public struct Struct
        {
            public string Member1;
            public SimpleClass Member2;
        }

        [Test]
        public void TestChangePrimitiveMember()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ComplexClass { Member1 = 3 };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member1)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member1)], ContentChangeType.ValueChange, Index.Empty, 3, 4, x => x.Update(4));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member1)], ContentChangeType.ValueChange, Index.Empty, 4, 5, x => x.Update(5));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member1)]);
        }

        [Test]
        public void TestChangeReferenceMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member2 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member2)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member2)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member2)]);
        }

        [Test]
        public void TestChangeReferenceMemberToNull()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), null, new SimpleClass() };
            var instance = new ComplexClass { Member2 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member2)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member2)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member2)]);
        }

        [Test]
        public void TestChangeBoxedPrimitiveMember()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ComplexClass { Member3 = 3 };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member3)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, Index.Empty, 3, 4, x => x.Update(4));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member3)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, Index.Empty, 4, 5, x => x.Update(5));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member3)]);
        }

        [Test]
        public void TestChangeReferenceInObjectMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member3 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member3)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member3)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member3)]);
        }

        [Test]
        public void TestChangeStruct()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member4 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member4)];
            Assert.AreEqual("aa", node.Target[nameof(Struct.Member1)].Retrieve());
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual("bb", node.Target[nameof(Struct.Member1)].Retrieve());
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member4)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual("cc", node.Target[nameof(Struct.Member1)].Retrieve());
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member4)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)], ContentChangeType.ValueChange, Index.Empty, "cc", "dd", x => x.Update("dd"));
            Assert.AreEqual("dd", node.Target[nameof(Struct.Member1)].Retrieve());
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member4)]);
        }

        [Test]
        public void TestChangeStructMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member4 = new Struct { Member1 = obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var targetNode = rootNode[nameof(ComplexClass.Member4)].Target;
            var node = targetNode[nameof(Struct.Member1)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(targetNode, rootNode[nameof(ComplexClass.Member4)].Target);
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(targetNode, rootNode[nameof(ComplexClass.Member4)].Target);
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)]);
        }

        [Test]
        public void TestChangePrimitiveList()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new List<string> { "aa" }, new List<string> { "bb" }, new List<string> { "cc" } };
            var instance = new ComplexClass { Member5 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.AreEqual("aa", node.Retrieve(new Index(0)));
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member5)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual("bb", node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member5)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual("cc", node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionUpdate, new Index(0), "cc", "dd", x => x.Update("dd", new Index(0)));
            Assert.AreEqual("dd", node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Test]
        public void TestChangePrimitiveListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member5 = new List<string> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[0], obj[1], x => x.Update(obj[1], new Index(0)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[1], obj[2], x => x.Update(obj[2], new Index(0)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Test]
        public void TestAddPrimitiveListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member5 = new List<string> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionAdd, new Index(1), null, obj[1], x => x.Add(obj[1], new Index(1)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(1)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionAdd, new Index(2), null, obj[2], x => x.Add(obj[2], new Index(2)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(2)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Test]
        public void TestRemovePrimitiveListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member5 = new List<string> { obj[0], obj[1], obj[2] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(1)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(2)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionRemove, new Index(1), obj[1], null, x => x.Remove(obj[1], new Index(1)));
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(1)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionRemove, new Index(1), obj[2], null, x => x.Remove(obj[2], new Index(1)));
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Test]
        public void TestChangeReferenceList()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new List<SimpleClass> { new SimpleClass() }, new List<SimpleClass> { new SimpleClass() }, new List<SimpleClass> { new SimpleClass() } };
            var instance = new ComplexClass { Member6 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.AreEqual(obj[0][0], node.Retrieve(new Index(0)));
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(obj[1][0], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(obj[2][0], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
            var newItem = new SimpleClass();
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[2][0], newItem, x => x.Update(newItem, new Index(0)));
            Assert.AreEqual(newItem, node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Test]
        public void TestChangeReferenceListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[0], obj[1], x => x.Update(obj[1], new Index(0)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[1], obj[2], x => x.Update(obj[2], new Index(0)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Test]
        public void TestAddReferenceListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionAdd, new Index(1), null, obj[1], x => x.Add(obj[1], new Index(1)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(1)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionAdd, new Index(2), null, obj[2], x => x.Add(obj[2], new Index(2)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(2)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Test]
        public void TestRemoveReferenceListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { obj[0], obj[1], obj[2] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(1)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(2)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionRemove, new Index(1), obj[1], null, x => x.Remove(obj[1], new Index(1)));
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(1)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionRemove, new Index(1), obj[2], null, x => x.Remove(obj[2], new Index(1)));
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Test]
        public void TestChangeReferenceListItemMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { 3, 4, 5 };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { new SimpleClass(), new SimpleClass { Member1 = obj[0] } } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            Index index = new Index(1);
            var node = rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)];
            Assert.AreEqual(obj[0], node.Retrieve());
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(obj[1], node.Retrieve());
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(obj[2], node.Retrieve());
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)]);
        }

        [Test]
        public void TestChangeStructList()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new List<Struct> { new Struct() }, new List<Struct> { new Struct() }, new List<Struct> { new Struct() } };
            var instance = new ComplexClass { Member7 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            Assert.AreEqual(obj[0][0], node.Retrieve(new Index(0)));
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(obj[1][0], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(obj[2][0], node.Retrieve(new Index(0)));
            var newItem = new Struct();
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
            var itemNode = rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new Index(0));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[2][0], newItem, x => x.Update(newItem, new Index(0)));
            Assert.AreEqual(newItem, node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
            Assert.AreEqual(itemNode, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new Index(0)));
        }

        [Test]
        public void TestChangeStructListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member7 = new List<Struct> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            var itemNode = rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new Index(0));
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[0], obj[1], x => x.Update(obj[1], new Index(0)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
            // TODO: would be nice to be able to keep the same boxed node!
            Assert.AreEqual(itemNode, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new Index(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionUpdate, new Index(0), obj[1], obj[2], x => x.Update(obj[2], new Index(0)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
            // TODO: would be nice to be able to keep the same boxed node!
            Assert.AreEqual(itemNode, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new Index(0)));
        }

        [Test]
        public void TestAddStructListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member7 = new List<Struct> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionAdd, new Index(1), null, obj[1], x => x.Add(obj[1], new Index(1)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(1)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionAdd, new Index(2), null, obj[2], x => x.Add(obj[2], new Index(2)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(2)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
        }

        [Test]
        public void TestRemoveStructListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member7 = new List<Struct> { obj[0], obj[1], obj[2] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(obj[1], node.Retrieve(new Index(1)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(2)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionRemove, new Index(1), obj[1], null, x => x.Remove(obj[1], new Index(1)));
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(obj[2], node.Retrieve(new Index(1)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionRemove, new Index(1), obj[2], null, x => x.Remove(obj[2], new Index(1)));
            Assert.AreEqual(obj[0], node.Retrieve(new Index(0)));
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)]);
        }

        [Test]
        public void TestChangeStructListItemMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member7 = new List<Struct> { new Struct(), new Struct { Member1 = obj[0] } } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            Index index = new Index(1);
            var node = rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)];
            Assert.AreEqual(obj[0], node.Retrieve());
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, Index.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.AreEqual(obj[1], node.Retrieve());
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new Index(1))[nameof(SimpleClass.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, Index.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.AreEqual(obj[2], node.Retrieve());
            Assert.AreEqual(node, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new Index(1))[nameof(SimpleClass.Member1)]);
        }

        [Test]
        public void TestDiscardedReferenceMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member2 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var obj0Node = nodeContainer.GetOrCreateNode(obj[0]);
            var obj1Node = nodeContainer.GetOrCreateNode(obj[1]);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            int changingCount = 0;
            int changedCount = 0;
            listener.ValueChanging += (sender, e) => ++changingCount;
            listener.ValueChanged += (sender, e) => ++changedCount;
            obj0Node[nameof(SimpleClass.Member1)].Update(1);
            Assert.AreEqual(1, changingCount);
            Assert.AreEqual(1, changedCount);
            rootNode[nameof(ComplexClass.Member2)].Update(obj[1]);
            Assert.AreEqual(2, changingCount);
            Assert.AreEqual(2, changedCount);
            obj0Node[nameof(SimpleClass.Member1)].Update(2);
            Assert.AreEqual(2, changingCount);
            Assert.AreEqual(2, changedCount);
            obj1Node[nameof(SimpleClass.Member1)].Update(3);
            Assert.AreEqual(3, changingCount);
            Assert.AreEqual(3, changedCount);
        }

        private static void VerifyListenerEvent(INodeChangeEventArgs e, IGraphNode nodeOwner, ContentChangeType type, Index index, object oldValue, object newValue, bool changeApplied)
        {
            Assert.NotNull(e);
            Assert.NotNull(nodeOwner);
            Assert.AreEqual(type, e.ChangeType);
            Assert.AreEqual(nodeOwner, e.Node);
            Assert.AreEqual(index, (e as ItemChangeEventArgs)?.Index ?? Index.Empty);
            Assert.AreEqual(newValue, e.NewValue);
            Assert.AreEqual(oldValue, e.OldValue);
            if (type == ContentChangeType.ValueChange)
            {
                Assert.AreEqual(changeApplied ? newValue : oldValue, nodeOwner.Retrieve(index));
            }
        }

        private static void TestContentChange([NotNull] GraphNodeChangeListener listener, [NotNull] Func<IMemberNode> fetchNode, ContentChangeType type, Index index, object oldValue, object newValue, [NotNull] Action<IMemberNode> change)
        {
            var i = 0;
            var contentOwner = fetchNode();
            var changing = new EventHandler<MemberNodeChangeEventArgs>((sender, e) => { Assert.AreEqual(0, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, false); ++i; });
            var changed = new EventHandler<MemberNodeChangeEventArgs>((sender, e) => { Assert.AreEqual(1, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, true); ++i; });
            listener.ValueChanging += changing;
            listener.ValueChanged += changed;
            change(contentOwner);
            Assert.AreEqual(2, i);
            listener.ValueChanging -= changing;
            listener.ValueChanged -= changed;
        }

        private static void TestItemChange([NotNull] GraphNodeChangeListener listener, [NotNull] Func<IObjectNode> fetchNode, ContentChangeType type, Index index, object oldValue, object newValue, [NotNull] Action<IObjectNode> change)
        {
            var i = 0;
            var contentOwner = fetchNode();
            var changing = new EventHandler<ItemChangeEventArgs>((sender, e) => { Assert.AreEqual(0, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, false); ++i; });
            var changed = new EventHandler<ItemChangeEventArgs>((sender, e) => { Assert.AreEqual(1, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, true); ++i; });
            listener.ItemChanging += changing;
            listener.ItemChanged += changed;
            change(contentOwner);
            Assert.AreEqual(2, i);
            listener.ItemChanging -= changing;
            listener.ItemChanged -= changed;
        }
    }
}
