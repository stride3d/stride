// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Xenko.Core.Quantum.Tests
{
    public class TestReferences
    {
        public class TestObject
        {
            public string Name;
            public override string ToString() => $"{{TestObject: {Name}}}";
        }

        public class ObjectContainer
        {
            public object Instance { get; set; }
            public override string ToString() => "{ObjectContainer}";
        }

        public class ObjectsContainer
        {
            public TestObject Instance1 { get; set; }
            public TestObject Instance2 { get; set; }
            public override string ToString() => $"{{ObjectsContainer: {Instance1}, {Instance2}}}";
        }

        public class MultipleObjectContainer
        {
            public List<TestObject> Instances { get; set; } = new List<TestObject>();
            public override string ToString() => $"{{MultipleObjectContainer: {string.Join(", ", Instances.Select(x => x.ToString()))}}}";
        }

        /// <summary>
        /// This test creates an object referencing another and verifies that the reference is properly created in the node graph.
        /// </summary>
        [Fact]
        public void TestObjectReference()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, instance, nameof(ObjectContainer.Instance), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, instance, false);
            var instanceNode = nodeContainer.GetNode(instance);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, instanceNode, instance);

            memberNode = instanceNode.Members.First();
            Helper.TestMemberNode(instanceNode, memberNode, instance, instance.Name, nameof(TestObject.Name), false);
        }

        /// <summary>
        /// This test creates an object with a reference that is null and verifies that the node graph is consistent.
        /// </summary>
        [Fact]
        public void TestNullObjectReference()
        {
            var nodeContainer = new NodeContainer();
            var container = new ObjectContainer { Instance = null };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, null, nameof(ObjectContainer.Instance), true);
            Helper.TestNullObjectReference(memberNode.TargetReference);
        }

        /// <summary>
        /// This test creates an object referencing another and verifies that the reference is properly created in the node graph.
        /// </summary>
        [Fact]
        public void TestObjectReferenceUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode.Members.First();
            var instanceNode = memberNode.TargetReference.TargetNode;

            // Update to a new instance
            var newInstance = new TestObject { Name = "Test2" };
            memberNode.Update(newInstance);
            Helper.TestMemberNode(containerNode, memberNode, container, newInstance, nameof(ObjectContainer.Instance), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, newInstance, false);

            var newInstanceNode = nodeContainer.GetNode(newInstance);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, newInstanceNode, newInstance);
            Assert.NotEqual(instanceNode.Guid, newInstanceNode.Guid);

            // Update to null
            memberNode.Update(null);
            Helper.TestMemberNode(containerNode, memberNode, container, null, nameof(ObjectContainer.Instance), true);
            Helper.TestNullObjectReference(memberNode.TargetReference);

            // Update back to the initial instance
            memberNode.Update(instance);
            Helper.TestMemberNode(containerNode, memberNode, container, instance, nameof(ObjectContainer.Instance), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, instanceNode, instance);
        }

        /// <summary>
        /// This test creates an object containing a collection of references to other objects and verifies that
        /// the references are properly created in the node graph.
        /// </summary>
        [Fact]
        public void TestEnumerableReference()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new TestObject { Name = "Test1" };
            var instance2 = new TestObject { Name = "Test2" };
            var container = new MultipleObjectContainer { Instances = { instance1, instance2 } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, container.Instances, nameof(MultipleObjectContainer.Instances), true);
            Helper.TestReferenceEnumerable(memberNode.Target.ItemReferences, container.Instances);

            Assert.Equal(container.Instances, memberNode.Retrieve());
            Assert.Equal(instance1, memberNode.Retrieve(new NodeIndex(0)));
            Assert.Equal(instance2, memberNode.Retrieve(new NodeIndex(1)));

            var reference1 = memberNode.Target.ItemReferences.First();
            Helper.TestMemberNode(reference1.TargetNode, reference1.TargetNode.Members.First(), instance1, instance1.Name, nameof(TestObject.Name), false);

            var reference2 = memberNode.Target.ItemReferences.Last();
            Helper.TestMemberNode(reference2.TargetNode, reference2.TargetNode.Members.First(), instance2, instance2.Name, nameof(TestObject.Name), false);
        }

        /// <summary>
        /// This test creates an object containing a collection of references to null and verifies that
        /// the references in the node graph are consistent.
        /// </summary>
        [Fact]
        public void TestNullEnumerableReference()
        {
            var nodeContainer = new NodeContainer();
            var container = new MultipleObjectContainer { Instances = { null, null } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, container.Instances, nameof(MultipleObjectContainer.Instances), true);
            Helper.TestReferenceEnumerable(memberNode.Target.ItemReferences, container.Instances);

            Assert.Equal(container.Instances, memberNode.Retrieve());
            Assert.Null(memberNode.Retrieve(new NodeIndex(0)));
            Assert.Null(memberNode.Retrieve(new NodeIndex(1)));
        }

        /// <summary>
        /// This test creates an object containing a collection of references to other objects and verifies that
        /// the references are properly created in the node graph.
        /// </summary>
        [Fact]
        public void TestEnumerableReferenceUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new TestObject { Name = "Test1" };
            var instance2 = new TestObject { Name = "Test2" };
            var container = new MultipleObjectContainer { Instances = { instance1, instance2 } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode.Members.First();
            var reference = memberNode.Target.ItemReferences;
            var reference1 = reference.First();
            var reference2 = reference.Last();

            // Update item 0 to a new instance and item 1 to null
            var newInstance = new TestObject { Name = "Test3" };
            memberNode.Target.Update(newInstance, new NodeIndex(0));
            memberNode.Target.Update(null, new NodeIndex(1));
            Assert.Equal(container.Instances, memberNode.Retrieve());
            Assert.Equal(newInstance, memberNode.Retrieve(new NodeIndex(0)));
            Assert.Null(memberNode.Retrieve(new NodeIndex(1)));
            Helper.TestReferenceEnumerable(memberNode.Target.ItemReferences, container.Instances);

            var newReference = memberNode.Target.ItemReferences;
            Assert.Equal(reference, newReference);
            Assert.Equal(2, newReference.Count);
            var newReference1 = newReference.First();
            var newReference2 = newReference.Last();

            Assert.NotEqual(reference1, newReference1);
            Assert.NotEqual(reference1.TargetGuid, newReference1.TargetGuid);
            Assert.NotEqual(reference1.TargetNode, newReference1.TargetNode);
            Assert.NotEqual(reference1.ObjectValue, newReference1.ObjectValue);
            Assert.NotEqual(reference2, newReference2);
            Assert.NotEqual(reference2.TargetGuid, newReference2.TargetGuid);
            Assert.NotEqual(reference2.TargetNode, newReference2.TargetNode);
            Assert.NotEqual(reference2.ObjectValue, newReference2.ObjectValue);
        }

        /// <summary>
        /// This test creates an object referencing another twice and verifies that the reference is properly created in the node graph.
        /// </summary>
        [Fact]
        public void TestObjectReferenceSameInstance()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectsContainer { Instance1 = instance, Instance2 = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 2);

            var member1Node = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, member1Node, container, instance, nameof(ObjectsContainer.Instance1), true);
            Helper.TestNonNullObjectReference(member1Node.TargetReference, instance, false);

            var member2Node = containerNode.Members.Last();
            Helper.TestMemberNode(containerNode, member2Node, container, instance, nameof(ObjectsContainer.Instance2), true);
            Helper.TestNonNullObjectReference(member2Node.TargetReference, instance, false);

            var reference1 = member1Node.TargetReference;
            var reference2 = member2Node.TargetReference;
            Assert.Equal(reference1.TargetGuid, reference2.TargetGuid);
            Assert.Equal(reference1.TargetNode, reference2.TargetNode);
            Assert.Equal(reference1.ObjectValue, reference2.ObjectValue);
        }

        /// <summary>
        /// This test creates an object containing a collection of references to the same object and verifies that
        /// the references are properly created in the node graph.
        /// </summary>
        [Fact]
        public void TestEnumerableReferenceSameInstance()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new MultipleObjectContainer { Instances = { instance, instance } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, container.Instances, nameof(MultipleObjectContainer.Instances), true);
            Helper.TestReferenceEnumerable(memberNode.Target.ItemReferences, container.Instances);

            var reference = memberNode.Target.ItemReferences;
            Assert.Equal(2, reference.Count);
            var reference1 = reference.First();
            var reference2 = reference.Last();
            Assert.Equal(reference1.ObjectValue, reference2.ObjectValue);
            Assert.Equal(reference1.TargetGuid, reference2.TargetGuid);
            Assert.Equal(reference1.TargetNode, reference2.TargetNode);
        }

        /// <summary>
        /// This test creates two objects, one referencing the other. It verifies that when constructing the node of the referenced object first,
        /// the referencer object will reuse the same node as target of the reference
        /// </summary>
        [Fact]
        public void TestUseExistingNodeAsReference()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Assert.Equal(1, containerNode.Members.Count);
            var memberNode = containerNode.Members.First();
            Assert.Equal(instance, memberNode.Retrieve());
            Assert.True(memberNode.IsReference);
            Assert.Equal(instanceNode, memberNode.TargetReference.TargetNode);
        }

        /// <summary>
        /// This test creates two objects, one referencing the other. It verifies that when constructing the node of the referencer object first,
        /// the referenced object will reuse the same node as target of the reference
        /// </summary>
        [Fact]
        public void TestUseExistingReferenceAsNode()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            Assert.Equal(1, containerNode.Members.Count);
            var memberNode = containerNode.Members.First();
            Assert.Equal(instance, memberNode.Retrieve());
            Assert.True(memberNode.IsReference);
            Assert.Equal(instanceNode, memberNode.TargetReference.TargetNode);
        }

        /// <summary>
        /// This test creates a container object that reference multiples other object. It verifies that the same nodes are reused between the instances
        /// of objects and the references
        /// </summary>
        [Fact]
        public void TestUseExistingNodesAsReference()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new TestObject { Name = "Test1" };
            var instance2 = new TestObject { Name = "Test2" };
            var container = new MultipleObjectContainer { Instances = { instance1, instance2 } };
            var instance1Node = nodeContainer.GetOrCreateNode(instance1);
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var instance2Node = nodeContainer.GetOrCreateNode(instance2);
            Assert.Equal(1, containerNode.Members.Count);
            var memberNode = containerNode.Members.First();
            Assert.True(memberNode.IsReference);
            var reference = memberNode.Target.ItemReferences;
            Assert.Equal(2, reference.Indices.Count);
            var reference1 = reference.First();
            var reference2 = reference.Last();
            Assert.Equal(instance1Node, reference1.TargetNode);
            Assert.Equal(instance2Node, reference2.TargetNode);
        }

        /// <summary>
        /// This test creates two objects referencing each other. It verifies that the same nodes are reused between instances and references.
        /// </summary>
        [Fact]
        public void TestCircularReferences()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new ObjectContainer();
            var instance2 = new ObjectContainer();
            instance1.Instance = instance2;
            instance2.Instance = instance1;
            var instance1Node = nodeContainer.GetOrCreateNode(instance1);
            var instance2Node = nodeContainer.GetOrCreateNode(instance2);
            Assert.Equal(1, instance1Node.Members.Count);
            Assert.Equal(1, instance2Node.Members.Count);
            Assert.Equal(instance1Node.Members.First().TargetReference.TargetNode, instance2Node);
            Assert.Equal(instance2Node.Members.First().TargetReference.TargetNode, instance1Node);
        }

        /// <summary>
        /// This test creates two objects and updates them to referencing each other. It verifies that the same nodes are reused between instances and references.
        /// </summary>
        [Fact]
        public void TestCircularReferencesUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new ObjectContainer();
            var instance2 = new ObjectContainer();
            var instance1Node = nodeContainer.GetOrCreateNode(instance1);
            var instance2Node = nodeContainer.GetOrCreateNode(instance2);
            instance1Node.Members.First().Update(instance2);
            instance2Node.Members.First().Update(instance1);
            Assert.Equal(instance1Node.Members.First().TargetReference.TargetNode, instance2Node);
            Assert.Equal(instance2Node.Members.First().TargetReference.TargetNode, instance1Node);
        }

        /// <summary>
        /// This test creates an object and make it reference itself. It verifies that the same nodes are reused.
        /// </summary>
        [Fact]
        public void TestSelfReference()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ObjectContainer();
            instance.Instance = instance;
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            Assert.Equal(1, instanceNode.Members.Count);
            Assert.Equal(instanceNode.Members.First().TargetReference.TargetNode, instanceNode);
        }

        /// <summary>
        /// This test creates an object and update it to make it reference itself. It verifies that the same nodes are reused.
        /// </summary>
        [Fact]
        public void TestSelfReferenceUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ObjectContainer();
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            instanceNode.Members.First().Update(instance);
            instance.Instance = instance;
            Assert.Equal(1, instanceNode.Members.Count);
            Assert.Equal(instanceNode.Members.First().TargetReference.TargetNode, instanceNode);
        }
    }
}
