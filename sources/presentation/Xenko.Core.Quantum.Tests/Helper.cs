// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Xenko.Core.Quantum.References;

namespace Xenko.Core.Quantum.Tests
{
    public static class Helper
    {
        /// <summary>
        /// Tests the validity of a node that is an object that is not a collection
        /// </summary>
        /// <param name="node">The node to validate.</param>
        /// <param name="obj">The value represented by this node.</param>
        /// <param name="childCount">The number of members expected in the node.</param>
        public static void TestNonCollectionObjectNode(IGraphNode node, object obj, int childCount)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            // Check that the content is of the expected type.
            Assert.IsInstanceOf<ObjectNode>(node);
            // A node with an ObjectNode should have the related object as value of its content.
            Assert.AreEqual(obj, node.Retrieve());
            // A node with an ObjectNode should not contain a reference if it does not represent a collection.
            Assert.AreEqual(false, node.IsReference);
            // Check that we have the expected number of children.
            Assert.AreEqual(childCount, ((IObjectNode)node).Members.Count);
        }

        /// <summary>
        /// Tests the validity of a node that is an object that is a collection
        /// </summary>
        /// <param name="node">The node to validate.</param>
        /// <param name="obj">The value represented by this node.</param>
        /// <param name="isReference">Indicate whether the node is expected to contain an enumerable reference to the collection items.</param>
        public static void TestCollectionObjectContentNode(IGraphNode node, object obj, bool isReference)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            // Check that the content is of the expected type.
            Assert.IsInstanceOf<ObjectNode>(node);
            // A node with an ObjectNode should have the related object as value of its content.
            Assert.AreEqual(obj, node.Retrieve());
            if (isReference)
            {
                // A node with an ObjectNode representing a collection of reference types should contain an enumerable reference.
                Assert.AreEqual(true, node.IsReference);
                Assert.NotNull(((IObjectNode)node).ItemReferences);
            }
            else
            {
                // A node with an ObjectNode representing a collection of primitive or struct types should not contain a refernce.
                Assert.AreEqual(false, node.IsReference);            
            }
            // A node with an ObjectNode representing a collection should not have any child.
            Assert.AreEqual(0, ((IObjectNode)node).Members.Count);
        }

        /// <summary>
        /// Tests the validity of a node that is a member of an object.
        /// </summary>
        /// <param name="containerNode">The node of the container of this member.</param>
        /// <param name="memberNode">The memeber node to validate.</param>
        /// <param name="container">The value represented by the container node.</param>
        /// <param name="member">The value of the member represented by the member node.</param>
        /// <param name="memberName">The name of the member to validate.</param>
        /// <param name="isReference">Indicate whether the member node is expected to contain a reference to the value it represents.</param>
        public static void TestMemberNode(IGraphNode containerNode, IGraphNode memberNode, object container, object member, string memberName, bool isReference)
        {
            if (containerNode == null) throw new ArgumentNullException(nameof(containerNode));
            if (memberNode == null) throw new ArgumentNullException(nameof(memberNode));
            if (container == null) throw new ArgumentNullException(nameof(container));

            // Check that the content is of the expected type.
            Assert.AreEqual(typeof(MemberNode), memberNode.GetType());
            // A node with a MemberNode should have the same name that the member in the container.
            Assert.AreEqual(memberName, ((IMemberNode)memberNode).Name);
            // A node with a MemberNode should have its container as parent.
            Assert.AreEqual(containerNode, ((IMemberNode)memberNode).Parent);
            // A node with a MemberNode should have the member value as value of its content.
            Assert.AreEqual(member, memberNode.Retrieve());
            // A node with a primitive MemberNode should not contain a reference.
            Assert.AreEqual(isReference, memberNode.IsReference);
        }

        /// <summary>
        /// Tests the validity of a reference to a non-null target object.
        /// </summary>
        /// <param name="reference">The reference to test.</param>
        /// <param name="targetValue">The actual value pointed by the reference.</param>
        /// <param name="hasIndex">Indicates whether the reference has an index.</param>
        public static void TestNonNullObjectReference(ObjectReference reference, object targetValue, bool hasIndex)
        {
            // Check that the reference is not null.
            Assert.IsNotNull(reference);
            // Check that the values match.
            Assert.AreEqual(targetValue, reference.TargetNode.Retrieve());
            // Check that the values match.
            Assert.AreEqual(targetValue, reference.ObjectValue);
            // Check that that we have an index if expected.
            Assert.AreEqual(hasIndex, !reference.Index.IsEmpty);
            // Check that the target is an object content node.
            TestNonCollectionObjectNode(reference.TargetNode, targetValue, reference.TargetNode.Members.Count);
        }

        /// <summary>
        /// Tests the validity of a reference to a non-null target node.
        /// </summary>
        /// <param name="reference">The reference to test.</param>
        /// <param name="targetNode">The node that is supposed to be the target of the reference.</param>
        /// <param name="targetValue">The actual value pointed by the reference.</param>
        public static void TestNonNullObjectReference(ObjectReference reference, IGraphNode targetNode, object targetValue)
        {
            if (targetNode == null) throw new ArgumentNullException(nameof(targetNode));

            // Check that the reference is not null.
            Assert.IsNotNull(reference);
            // Check that the target node is of the expected type.
            Assert.IsInstanceOf<ObjectNode>(targetNode);
            // Check that the Guids match.
            Assert.AreEqual(targetNode.Guid, reference.TargetGuid);
            // Check that the nodes match.
            Assert.AreEqual(targetNode, reference.TargetNode);
            // Check that the values match.
            Assert.AreEqual(targetValue, reference.ObjectValue);
            // Check that the target is an object content node.
            TestNonCollectionObjectNode(targetNode, targetValue, ((IObjectNode)targetNode).Members.Count);
        }

        /// <summary>
        /// Tests the validity of a reference to a null target object.
        /// </summary>
        /// <param name="reference">The reference to test.</param>
        public static void TestNullObjectReference(ObjectReference reference)
        {
            // Check that the reference is not null.
            Assert.IsNotNull(reference);
            // Check that the Guids match.
            Assert.AreEqual(Guid.Empty, reference.TargetGuid);
            // Check that the nodes match.
            Assert.AreEqual(null, reference.TargetNode);
            // Check that the values match.
            Assert.AreEqual(null, reference.ObjectValue);
        }

        /// <summary>
        /// Tests the validity of an enumerable reference.
        /// </summary>
        /// <param name="reference">The reference to test.</param>
        /// <param name="targetValue">The actual collection pointed by the reference.</param>
        public static void TestReferenceEnumerable(ReferenceEnumerable reference, object targetValue)
        {
            var collection = (ICollection)targetValue;

            // Check that the reference is not null.
            Assert.IsNotNull(reference);
            // Check that the counts match.
            Assert.AreEqual(collection.Count, reference.Count);
            Assert.AreEqual(collection.Count, reference.Indices.Count);
            // Check that the object references match.
            foreach (var objReference in reference.Zip(collection.Cast<object>(), Tuple.Create))
            {
                Assert.AreEqual(objReference.Item2, objReference.Item1.ObjectValue);
                if (objReference.Item2 != null)
                {
                    TestNonNullObjectReference(objReference.Item1, objReference.Item2, true);
                }
                else
                {
                    TestNullObjectReference(objReference.Item1);
                }
            }
            // TODO: rework reference system and enable this
            //Assert.IsNull(reference.Index);
        }
    }
}
