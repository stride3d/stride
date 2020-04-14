// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xunit;
using Xenko.Core.Annotations;

namespace Xenko.Core.Quantum.Tests
{
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

        [Fact]
        public void TestChangePrimitiveMember()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ComplexClass { Member1 = 3 };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member1)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, 3, 4, x => x.Update(4));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, 4, 5, x => x.Update(5));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member1)]);
        }

        [Fact]
        public void TestChangeReferenceMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member2 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member2)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member2)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member2)]);
        }

        [Fact]
        public void TestChangeReferenceMemberToNull()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), null, new SimpleClass() };
            var instance = new ComplexClass { Member2 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member2)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member2)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member2)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member2)]);
        }

        [Fact]
        public void TestChangeBoxedPrimitiveMember()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ComplexClass { Member3 = 3 };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member3)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, NodeIndex.Empty, 3, 4, x => x.Update(4));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member3)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, NodeIndex.Empty, 4, 5, x => x.Update(5));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member3)]);
        }

        [Fact]
        public void TestChangeReferenceInObjectMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member3 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member3)];
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member3)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member3)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member3)]);
        }

        [Fact]
        public void TestChangeStruct()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member4 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member4)];
            Assert.Equal("aa", node.Target[nameof(Struct.Member1)].Retrieve());
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal("bb", node.Target[nameof(Struct.Member1)].Retrieve());
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member4)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal("cc", node.Target[nameof(Struct.Member1)].Retrieve());
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member4)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, "cc", "dd", x => x.Update("dd"));
            Assert.Equal("dd", node.Target[nameof(Struct.Member1)].Retrieve());
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member4)]);
        }

        [Fact]
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
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(targetNode, rootNode[nameof(ComplexClass.Member4)].Target);
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(targetNode, rootNode[nameof(ComplexClass.Member4)].Target);
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member4)].Target[nameof(Struct.Member1)]);
        }

        [Fact]
        public void TestChangePrimitiveList()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new List<string> { "aa" }, new List<string> { "bb" }, new List<string> { "cc" } };
            var instance = new ComplexClass { Member5 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.Equal("aa", node.Retrieve(new NodeIndex(0)));
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member5)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal("bb", node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member5)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal("cc", node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), "cc", "dd", x => x.Update("dd", new NodeIndex(0)));
            Assert.Equal("dd", node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Fact]
        public void TestChangePrimitiveListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member5 = new List<string> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[0], obj[1], x => x.Update(obj[1], new NodeIndex(0)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[1], obj[2], x => x.Update(obj[2], new NodeIndex(0)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Fact]
        public void TestAddPrimitiveListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member5 = new List<string> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionAdd, new NodeIndex(1), null, obj[1], x => x.Add(obj[1], new NodeIndex(1)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionAdd, new NodeIndex(2), null, obj[2], x => x.Add(obj[2], new NodeIndex(2)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(2)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Fact]
        public void TestRemovePrimitiveListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member5 = new List<string> { obj[0], obj[1], obj[2] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member5)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(2)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionRemove, new NodeIndex(1), obj[1], null, x => x.Remove(obj[1], new NodeIndex(1)));
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member5)].Target, ContentChangeType.CollectionRemove, new NodeIndex(1), obj[2], null, x => x.Remove(obj[2], new NodeIndex(1)));
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member5)]);
        }

        [Fact]
        public void TestChangeReferenceList()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new List<SimpleClass> { new SimpleClass() }, new List<SimpleClass> { new SimpleClass() }, new List<SimpleClass> { new SimpleClass() } };
            var instance = new ComplexClass { Member6 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.Equal(obj[0][0], node.Retrieve(new NodeIndex(0)));
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(obj[1][0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(obj[2][0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
            var newItem = new SimpleClass();
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[2][0], newItem, x => x.Update(newItem, new NodeIndex(0)));
            Assert.Equal(newItem, node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Fact]
        public void TestChangeReferenceListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[0], obj[1], x => x.Update(obj[1], new NodeIndex(0)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[1], obj[2], x => x.Update(obj[2], new NodeIndex(0)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Fact]
        public void TestAddReferenceListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionAdd, new NodeIndex(1), null, obj[1], x => x.Add(obj[1], new NodeIndex(1)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionAdd, new NodeIndex(2), null, obj[2], x => x.Add(obj[2], new NodeIndex(2)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(2)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Fact]
        public void TestRemoveReferenceListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new SimpleClass(), new SimpleClass(), new SimpleClass() };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { obj[0], obj[1], obj[2] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member6)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(2)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionRemove, new NodeIndex(1), obj[1], null, x => x.Remove(obj[1], new NodeIndex(1)));
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target, ContentChangeType.CollectionRemove, new NodeIndex(1), obj[2], null, x => x.Remove(obj[2], new NodeIndex(1)));
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)]);
        }

        [Fact]
        public void TestChangeReferenceListItemMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { 3, 4, 5 };
            var instance = new ComplexClass { Member6 = new List<SimpleClass> { new SimpleClass(), new SimpleClass { Member1 = obj[0] } } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            NodeIndex index = new NodeIndex(1);
            var node = rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)];
            Assert.Equal(obj[0], node.Retrieve());
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(obj[1], node.Retrieve());
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(obj[2], node.Retrieve());
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member6)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)]);
        }

        [Fact]
        public void TestChangeStructList()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new List<Struct> { new Struct() }, new List<Struct> { new Struct() }, new List<Struct> { new Struct() } };
            var instance = new ComplexClass { Member7 = obj[0] };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            Assert.Equal(obj[0][0], node.Retrieve(new NodeIndex(0)));
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(obj[1][0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(obj[2][0], node.Retrieve(new NodeIndex(0)));
            var newItem = new Struct();
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
            var itemNode = rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new NodeIndex(0));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[2][0], newItem, x => x.Update(newItem, new NodeIndex(0)));
            Assert.Equal(newItem, node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
            Assert.Equal(itemNode, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new NodeIndex(0)));
        }

        [Fact]
        public void TestChangeStructListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member7 = new List<Struct> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            var itemNode = rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new NodeIndex(0));
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[0], obj[1], x => x.Update(obj[1], new NodeIndex(0)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
            // TODO: would be nice to be able to keep the same boxed node!
            Assert.Equal(itemNode, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new NodeIndex(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionUpdate, new NodeIndex(0), obj[1], obj[2], x => x.Update(obj[2], new NodeIndex(0)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
            // TODO: would be nice to be able to keep the same boxed node!
            Assert.Equal(itemNode, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new NodeIndex(0)));
        }

        [Fact]
        public void TestAddStructListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member7 = new List<Struct> { obj[0] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionAdd, new NodeIndex(1), null, obj[1], x => x.Add(obj[1], new NodeIndex(1)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionAdd, new NodeIndex(2), null, obj[2], x => x.Add(obj[2], new NodeIndex(2)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(2)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
        }

        [Fact]
        public void TestRemoveStructListItem()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { new Struct { Member1 = "aa" }, new Struct { Member1 = "bb" }, new Struct { Member1 = "cc" } };
            var instance = new ComplexClass { Member7 = new List<Struct> { obj[0], obj[1], obj[2] } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            var node = rootNode[nameof(ComplexClass.Member7)];
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(obj[1], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(2)));
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionRemove, new NodeIndex(1), obj[1], null, x => x.Remove(obj[1], new NodeIndex(1)));
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(obj[2], node.Retrieve(new NodeIndex(1)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
            TestItemChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target, ContentChangeType.CollectionRemove, new NodeIndex(1), obj[2], null, x => x.Remove(obj[2], new NodeIndex(1)));
            Assert.Equal(obj[0], node.Retrieve(new NodeIndex(0)));
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)]);
        }

        [Fact]
        public void TestChangeStructListItemMember()
        {
            var nodeContainer = new NodeContainer();
            var obj = new[] { "aa", "bb", "cc" };
            var instance = new ComplexClass { Member7 = new List<Struct> { new Struct(), new Struct { Member1 = obj[0] } } };
            var rootNode = nodeContainer.GetOrCreateNode(instance);
            var listener = new GraphNodeChangeListener(rootNode);
            listener.Initialize();
            NodeIndex index = new NodeIndex(1);
            var node = rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)];
            Assert.Equal(obj[0], node.Retrieve());
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[0], obj[1], x => x.Update(obj[1]));
            Assert.Equal(obj[1], node.Retrieve());
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member1)]);
            TestContentChange(listener, () => rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(index)[nameof(SimpleClass.Member1)], ContentChangeType.ValueChange, NodeIndex.Empty, obj[1], obj[2], x => x.Update(obj[2]));
            Assert.Equal(obj[2], node.Retrieve());
            Assert.Equal(node, rootNode[nameof(ComplexClass.Member7)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member1)]);
        }

        [Fact]
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
            Assert.Equal(1, changingCount);
            Assert.Equal(1, changedCount);
            rootNode[nameof(ComplexClass.Member2)].Update(obj[1]);
            Assert.Equal(2, changingCount);
            Assert.Equal(2, changedCount);
            obj0Node[nameof(SimpleClass.Member1)].Update(2);
            Assert.Equal(2, changingCount);
            Assert.Equal(2, changedCount);
            obj1Node[nameof(SimpleClass.Member1)].Update(3);
            Assert.Equal(3, changingCount);
            Assert.Equal(3, changedCount);
        }

        private static void VerifyListenerEvent(INodeChangeEventArgs e, IGraphNode nodeOwner, ContentChangeType type, NodeIndex index, object oldValue, object newValue, bool changeApplied)
        {
            Assert.NotNull(e);
            Assert.NotNull(nodeOwner);
            Assert.Equal(type, e.ChangeType);
            Assert.Equal(nodeOwner, e.Node);
            Assert.Equal(index, (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty);
            Assert.Equal(newValue, e.NewValue);
            Assert.Equal(oldValue, e.OldValue);
            if (type == ContentChangeType.ValueChange)
            {
                Assert.Equal(changeApplied ? newValue : oldValue, nodeOwner.Retrieve(index));
            }
        }

        private static void TestContentChange([NotNull] GraphNodeChangeListener listener, [NotNull] Func<IMemberNode> fetchNode, ContentChangeType type, NodeIndex index, object oldValue, object newValue, [NotNull] Action<IMemberNode> change)
        {
            var i = 0;
            var contentOwner = fetchNode();
            var changing = new EventHandler<MemberNodeChangeEventArgs>((sender, e) => { Assert.Equal(0, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, false); ++i; });
            var changed = new EventHandler<MemberNodeChangeEventArgs>((sender, e) => { Assert.Equal(1, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, true); ++i; });
            listener.ValueChanging += changing;
            listener.ValueChanged += changed;
            change(contentOwner);
            Assert.Equal(2, i);
            listener.ValueChanging -= changing;
            listener.ValueChanged -= changed;
        }

        private static void TestItemChange([NotNull] GraphNodeChangeListener listener, [NotNull] Func<IObjectNode> fetchNode, ContentChangeType type, NodeIndex index, object oldValue, object newValue, [NotNull] Action<IObjectNode> change)
        {
            var i = 0;
            var contentOwner = fetchNode();
            var changing = new EventHandler<ItemChangeEventArgs>((sender, e) => { Assert.Equal(0, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, false); ++i; });
            var changed = new EventHandler<ItemChangeEventArgs>((sender, e) => { Assert.Equal(1, i); VerifyListenerEvent(e, contentOwner, type, index, oldValue, newValue, true); ++i; });
            listener.ItemChanging += changing;
            listener.ItemChanged += changed;
            change(contentOwner);
            Assert.Equal(2, i);
            listener.ItemChanging -= changing;
            listener.ItemChanged -= changed;
        }
    }
}
