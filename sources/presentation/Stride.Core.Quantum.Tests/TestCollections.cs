// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum.Tests
{
    /// <summary>
    /// This fixture tests advanced scenario that uses collections.
    /// </summary>
    public class TestCollections
    {
        public class SimpleObject
        {
            public string Name;
            public override string ToString() => $"{{SimpleObject: {Name}}}";
        }

        public class ListContainer
        {
            public List<SimpleObject> List { get; set; } = new List<SimpleObject>();
            public List<object> ObjectList { get; set; } = new List<object>();

            public object ObjectMember { get; set; }
        }

        [Fact]
        public void TestOverwriteCollection()
        {
            var nodeContainer = new NodeContainer();
            var container = new ListContainer { List = { new SimpleObject(), new SimpleObject() } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var oldList = container.List;
            var oldItem1Node = containerNode[nameof(ListContainer.List)].Target.IndexedTarget(new NodeIndex(0));
            var oldItem2Node = containerNode[nameof(ListContainer.List)].Target.IndexedTarget(new NodeIndex(1));
            containerNode[nameof(ListContainer.List)].Update(new List<SimpleObject> { new SimpleObject(), new SimpleObject() });
            Assert.NotEqual(oldList, containerNode[nameof(ListContainer.List)].Retrieve());
            Assert.NotEqual(oldItem1Node, containerNode[nameof(ListContainer.List)].Target.IndexedTarget(new NodeIndex(0)));
            Assert.NotEqual(oldItem1Node.Retrieve(), containerNode[nameof(ListContainer.List)].Target.IndexedTarget(new NodeIndex(0)).Retrieve());
            Assert.NotEqual(oldItem2Node, containerNode[nameof(ListContainer.List)].Target.IndexedTarget(new NodeIndex(1)));
            Assert.NotEqual(oldItem2Node.Retrieve(), containerNode[nameof(ListContainer.List)].Target.IndexedTarget(new NodeIndex(1)).Retrieve());
        }

        [Fact]
        public void TestMultipleTimesSameObject()
        {
            var items = new[] { new SimpleObject(), new SimpleObject() };
            var nodeContainer = new NodeContainer();
            var container = new ListContainer { List = { items[0], items[1], items[0], items[1] } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(ListContainer.List)];
            VerifyItem(memberNode, 0, items[0], false);
            VerifyItem(memberNode, 1, items[1], false);
            VerifyItem(memberNode, 2, items[0], false);
            VerifyItem(memberNode, 3, items[1], false);

            memberNode.Target.Add(items[0]);
            VerifyItem(memberNode, 0, items[0], false);
            VerifyItem(memberNode, 1, items[1], false);
            VerifyItem(memberNode, 2, items[0], false);
            VerifyItem(memberNode, 3, items[1], false);
            VerifyItem(memberNode, 4, items[0], false);

            memberNode.Target.Update(items[1], new NodeIndex(2));
            VerifyItem(memberNode, 0, items[0], false);
            VerifyItem(memberNode, 1, items[1], false);
            VerifyItem(memberNode, 2, items[1], false);
            VerifyItem(memberNode, 3, items[1], false);
            VerifyItem(memberNode, 4, items[0], false);
        }

        [Fact]
        public void TestObjectListWithSimpleObjects()
        {
            var items = new[] { new SimpleObject(), new SimpleObject(), new SimpleObject(), new SimpleObject() };
            var obj = new ListContainer { ObjectList = { items[0], items[1] } };
            var nodeContainer = new NodeContainer();
            var containerNode = nodeContainer.GetOrCreateNode(obj);
            var memberNode = containerNode[nameof(ListContainer.ObjectList)];
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.ObjectList, nameof(ListContainer.ObjectList), true);
            VerifyItem(memberNode, 0, items[0], false);
            VerifyItem(memberNode, 1, items[1], false);

            // Add a new item
            memberNode.Target.Add(items[2]);
            VerifyItem(memberNode, 0, items[0], false);
            VerifyItem(memberNode, 1, items[1], false);
            VerifyItem(memberNode, 2, items[2], false);

            // Update existing item (with a different type here)
            memberNode.Target.Update(items[3], new NodeIndex(2));
            VerifyItem(memberNode, 0, items[0], false);
            VerifyItem(memberNode, 1, items[1], false);
            VerifyItem(memberNode, 2, items[3], false);
        }

        [Fact]
        public void TestObjectListWithBoxedPrimitiveValue()
        {
            var obj = new ListContainer { ObjectList = { 1.0f, 2.0f } };
            var nodeContainer = new NodeContainer();

            // Construction
            var containerNode = nodeContainer.GetOrCreateNode(obj);
            var memberNode = containerNode[nameof(ListContainer.ObjectList)];
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.ObjectList, nameof(ListContainer.ObjectList), true);
            VerifyItem(memberNode, 0, 1.0f, true);
            VerifyItem(memberNode, 1, 2.0f, true);

            // Add a new item
            memberNode.Target.Add(3.0f);
            VerifyItem(memberNode, 0, 1.0f, true);
            VerifyItem(memberNode, 1, 2.0f, true);
            VerifyItem(memberNode, 2, 3.0f, true);

            // Update existing item (with a different type here)
            memberNode.Target.Update(4.0, new NodeIndex(2));
            VerifyItem(memberNode, 0, 1.0f, true);
            VerifyItem(memberNode, 1, 2.0f, true);
            VerifyItem(memberNode, 2, 4.0, true);
        }

        [Fact(Skip = "Update for object members not fully supported yet")]
        public void TestListInObjectMember()
        {
            var nodeContainer = new NodeContainer();
            var values = new object[] { new SimpleObject(), new List<SimpleObject> { new SimpleObject(), new SimpleObject() }, new SimpleObject() };
            var container = new ListContainer { ObjectMember = values[0] };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Assert.Equal(container.ObjectMember, containerNode[nameof(ListContainer.ObjectMember)].Retrieve());
            containerNode[nameof(ListContainer.ObjectMember)].Update(values[1]);
            Assert.Equal(values[1], container.ObjectMember);
            Assert.Equal(container.ObjectMember, containerNode[nameof(ListContainer.ObjectMember)].Retrieve());
            Assert.True(containerNode[nameof(ListContainer.ObjectMember)].IsReference);
            Assert.NotNull(containerNode[nameof(ListContainer.ObjectMember)].Target.Indices);
            Assert.Equal(2, containerNode[nameof(ListContainer.ObjectMember)].Target.Indices.Count());
            Assert.Equal(new NodeIndex(0), containerNode[nameof(ListContainer.ObjectMember)].Target.Indices.ToList()[0]);
            Assert.Equal(new NodeIndex(1), containerNode[nameof(ListContainer.ObjectMember)].Target.Indices.ToList()[1]);
            Assert.Equal(TypeDescriptorFactory.Default.Find(typeof(object)), containerNode[nameof(ListContainer.ObjectMember)].Descriptor);
            Assert.Equal(((IList)values[1])[0], containerNode[nameof(ListContainer.ObjectMember)].Target.IndexedTarget(new NodeIndex(0)));
            Assert.Equal(((IList)values[1])[1], containerNode[nameof(ListContainer.ObjectMember)].Target.IndexedTarget(new NodeIndex(1)));
            Assert.Equal(typeof(object), containerNode[nameof(ListContainer.ObjectMember)].Type);
            // TODO: more things could be checked!
            containerNode[nameof(ListContainer.ObjectMember)].Update(values[2]);
            Assert.Equal(values[2], container.ObjectMember);
            Assert.Equal(container.ObjectMember, containerNode[nameof(ListContainer.ObjectMember)]);
        }

        private static void VerifyItem(IMemberNode listMemberNode, int index, object expectedValue, bool isPrimitive)
        {
            var targetNode = listMemberNode.Target;
            var enumRef = targetNode.ItemReferences;
            var indexValue = new NodeIndex(index);

            Assert.NotNull(enumRef);
            Assert.NotNull(targetNode.Indices);
            Assert.Equal(indexValue, targetNode.Indices.ToList()[index]);
            Assert.Equal(indexValue, enumRef.Indices.ToList()[index]);
            Assert.Equal(indexValue, enumRef.ToList()[index].Index);
            Assert.Equal(expectedValue, enumRef.ToList()[index].ObjectValue);
            Assert.NotNull(enumRef.ToList()[index].TargetNode);
            Assert.Equal(expectedValue, enumRef.ToList()[index].TargetNode.Retrieve());
            Assert.Equal(TypeDescriptorFactory.Default.Find(expectedValue.GetType()), enumRef.ToList()[index].TargetNode.Descriptor);
            Assert.False(enumRef.ToList()[index].TargetNode.IsReference);
            Assert.Equal(expectedValue.GetType(), enumRef.ToList()[index].TargetNode.Type);
            Assert.Equal(expectedValue, enumRef.ToList()[index].TargetNode.Retrieve());
        }
    }
}
