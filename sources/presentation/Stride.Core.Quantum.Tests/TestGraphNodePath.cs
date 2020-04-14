// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xunit;

namespace Stride.Core.Quantum.Tests
{
    public class TestGraphNodePath
    {
        public struct Struct
        {
            public string StringMember;
            public Class ClassMember;
        }
        public class Class
        {
            public int IntMember;
            public Struct StructMember;
            public Class ClassMember;
            public List<Class> ListMember = new List<Class>();
        }

        [Fact]
        public void TestConstructor()
        {
            var obj = new Class();
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);
            var path = new GraphNodePath(rootNode);
            Assert.True(path.IsEmpty);
            AssertAreEqual(rootNode, path.RootNode);
        }

        [Fact]
        public void TestEquals()
        {
            // Note: comparing GraphNodePath.GetHashCode() returns true when the root node is equivalent. This is because the root node is the only invariant.

            var obj = new Class { StructMember = { StringMember = "aa" }, ClassMember = new Class(), ListMember = { new Class(), new Class(), new Class() } };
            var nodeContainer = new NodeContainer();
            var path1 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path1.PushMember(nameof(Class.IntMember));
            var path2 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path2.PushMember(nameof(Class.IntMember));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreEqual(path1, path2);

            path1 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path1.PushMember(nameof(Class.ClassMember));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreNotEqual(path1, path2);
            path2 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path2.PushMember(nameof(Class.ClassMember));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreEqual(path1, path2);

            path1 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path1.PushMember(nameof(Class.ClassMember));
            path1.PushTarget();
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreNotEqual(path1, path2);
            path2 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path2.PushMember(nameof(Class.ClassMember));
            path2.PushTarget();
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreEqual(path1, path2);

            path1 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path1.PushMember(nameof(Class.ClassMember));
            path1.PushTarget();
            path1.PushMember(nameof(Class.IntMember));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreNotEqual(path1, path2);
            path2 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path2.PushMember(nameof(Class.ClassMember));
            path2.PushTarget();
            path2.PushMember(nameof(Class.IntMember));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreEqual(path1, path2);

            path1 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path1.PushMember(nameof(Class.ListMember));
            path1.PushIndex(new NodeIndex(0));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreNotEqual(path1, path2);
            path2 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path2.PushMember(nameof(Class.ListMember));
            path2.PushIndex(new NodeIndex(0));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreEqual(path1, path2);

            path2 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            path2.PushMember(nameof(Class.ListMember));
            path2.PushIndex(new NodeIndex(1));
            AssertAreEqual(path1.GetHashCode(), path2.GetHashCode());
            AssertAreNotEqual(path1, path2);
        }

        [Fact]
        public void TestClone()
        {
            var obj = new Class { ClassMember = new Class(), ListMember = { new Class(), new Class(), new Class() } };
            var nodeContainer = new NodeContainer();
            var path1 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj));
            var clone = path1.Clone();
            AssertAreEqual(path1, clone);
            AssertAreEqual(path1.GetHashCode(), clone.GetHashCode());
            AssertAreEqual(path1.RootNode, clone.RootNode);
            AssertAreEqual(path1.IsEmpty, clone.IsEmpty);
            AssertAreEqual(path1.GetNode(), clone.GetNode());
            var path2 = path1.Clone();
            path2.PushMember(nameof(Class.ClassMember));
            path2.PushTarget();
            path2.PushMember(nameof(Class.IntMember));
            clone = path2.Clone();
            AssertAreEqual(path2, clone);
            AssertAreEqual(path2.RootNode, clone.RootNode);
            AssertAreEqual(path2.IsEmpty, clone.IsEmpty);
            AssertAreEqual(path2.GetNode(), clone.GetNode());
            var path3 = path1.Clone();
            path3.PushMember(nameof(Class.ListMember));
            path3.PushTarget();
            path3.PushIndex(new NodeIndex(1));
            path3.PushMember(nameof(Class.IntMember));
            clone = path3.Clone();
            AssertAreEqual(path3, clone);
            AssertAreEqual(path3.RootNode, clone.RootNode);
            AssertAreEqual(path3.IsEmpty, clone.IsEmpty);
            AssertAreEqual(path3.GetNode(), clone.GetNode());
        }

        [Fact]
        public void TestCloneNewRoot()
        {
            var obj1 = new Class { ClassMember = new Class(), ListMember = { new Class(), new Class(), new Class() } };
            var obj2 = new Class { ClassMember = new Class(), ListMember = { new Class(), new Class(), new Class() } };
            var nodeContainer = new NodeContainer();
            var newRoot = nodeContainer.GetOrCreateNode(obj2);
            var path1 = new GraphNodePath(nodeContainer.GetOrCreateNode(obj1));
            var clone = path1.Clone(newRoot);
            AssertAreNotEqual(path1, clone);
            AssertAreNotEqual(path1.GetHashCode(), clone.GetHashCode());
            AssertAreNotEqual(newRoot, path1.RootNode);
            AssertAreEqual(newRoot, clone.RootNode);
            AssertAreEqual(path1.IsEmpty, clone.IsEmpty);
            var path2 = path1.Clone();
            path2.PushMember(nameof(Class.ClassMember));
            path2.PushTarget();
            path2.PushMember(nameof(Class.IntMember));
            clone = path2.Clone(newRoot);
            AssertAreNotEqual(path2, clone);
            AssertAreNotEqual(path2.GetHashCode(), clone.GetHashCode());
            AssertAreNotEqual(newRoot, path2.RootNode);
            AssertAreEqual(newRoot, clone.RootNode);
            AssertAreEqual(path2.IsEmpty, clone.IsEmpty);
            var path3 = path1.Clone();
            path3.PushMember(nameof(Class.ListMember));
            path3.PushIndex(new NodeIndex(1));
            path3.PushMember(nameof(Class.IntMember));
            clone = path3.Clone(newRoot);
            AssertAreNotEqual(path3, clone);
            AssertAreNotEqual(path3.GetHashCode(), clone.GetHashCode());
            AssertAreNotEqual(newRoot, path3.RootNode);
            AssertAreEqual(newRoot, clone.RootNode);
            AssertAreEqual(path3.IsEmpty, clone.IsEmpty);
        }

        [Fact]
        public void TestPushMember()
        {
            var obj = new Class();
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);
            var path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.IntMember));
            var intNode = rootNode[nameof(Class.IntMember)];
            var nodes = new IGraphNode[] { rootNode, intNode };
            Assert.NotNull(intNode);
            Assert.False(path.IsEmpty);
            AssertAreEqual(rootNode, path.RootNode);
            AssertAreEqual(intNode, path.GetNode());
            var i = 0;
            foreach (var node in path)
            {
                AssertAreEqual(nodes[i++], node);
            }
            AssertAreEqual(nodes.Length, i);
        }

        [Fact]
        public void TestPushStructMember()
        {
            var obj = new Class { StructMember = { StringMember = "aa" } };
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);
            var path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.StructMember));
            path.PushTarget();
            path.PushMember(nameof(Struct.StringMember));
            var structNode = rootNode[nameof(Class.StructMember)];
            var targetNode = rootNode[nameof(Class.StructMember)].Target;
            var memberNode = rootNode[nameof(Class.StructMember)].Target[nameof(Struct.StringMember)];
            var nodes = new IGraphNode[] { rootNode, structNode, targetNode, memberNode };
            Assert.NotNull(targetNode);
            Assert.NotNull(memberNode);
            Assert.False(path.IsEmpty);
            AssertAreEqual(rootNode, path.RootNode);
            AssertAreEqual(memberNode, path.GetNode());
            var i = 0;
            foreach (var node in path)
            {
                AssertAreEqual(nodes[i++], node);
            }
            AssertAreEqual(nodes.Length, i);
        }

        [Fact]
        public void TestPushTarget()
        {
            var obj = new Class { ClassMember = new Class() };
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);
            var path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ClassMember));
            path.PushTarget();
            var targetNode = nodeContainer.GetNode(obj.ClassMember);
            var nodes = new IGraphNode[] { rootNode, rootNode[nameof(Class.ClassMember)], targetNode };
            Assert.NotNull(targetNode);
            Assert.False(path.IsEmpty);
            AssertAreEqual(rootNode, path.RootNode);
            AssertAreEqual(targetNode, path.GetNode());
            var i = 0;
            foreach (var node in path)
            {
                AssertAreEqual(nodes[i++], node);
            }
            AssertAreEqual(nodes.Length, i);
        }

        [Fact]
        public void TestPushTargetAndMember()
        {
            var obj = new Class { ClassMember = new Class() };
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);
            var path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ClassMember));
            path.PushTarget();
            path.PushMember(nameof(Class.IntMember));
            var targetNode = nodeContainer.GetNode(obj.ClassMember);
            var intNode = targetNode[nameof(Class.IntMember)];
            var nodes = new IGraphNode[] { rootNode, rootNode[nameof(Class.ClassMember)], targetNode, intNode };
            Assert.NotNull(targetNode);
            Assert.NotNull(intNode);
            Assert.False(path.IsEmpty);
            AssertAreEqual(rootNode, path.RootNode);
            AssertAreEqual(intNode, path.GetNode());
            var i = 0;
            foreach (var node in path)
            {
                AssertAreEqual(nodes[i++], node);
            }
            AssertAreEqual(nodes.Length, i);
        }

        [Fact]
        public void TestPushIndex()
        {
            var obj = new Class { ListMember = { new Class(), new Class(), new Class() } };
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);
            var path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ListMember));
            path.PushTarget();
            path.PushIndex(new NodeIndex(1));
            var targetNode = nodeContainer.GetNode(obj.ListMember[1]);
            var nodes = new IGraphNode[] { rootNode, rootNode[nameof(Class.ListMember)], rootNode[nameof(Class.ListMember)].Target, targetNode };
            Assert.NotNull(targetNode);
            Assert.False(path.IsEmpty);
            AssertAreEqual(rootNode, path.RootNode);
            AssertAreEqual(targetNode, path.GetNode());
            var i = 0;
            foreach (var node in path)
            {
                AssertAreEqual(nodes[i++], node);
            }
            AssertAreEqual(nodes.Length, i);
        }

        [Fact]
        public void TestPushIndexAndMember()
        {
            var obj = new Class { ListMember = { new Class(), new Class(), new Class() } };
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);
            var path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ListMember));
            path.PushTarget();
            path.PushIndex(new NodeIndex(1));
            path.PushMember(nameof(Class.IntMember));
            var targetNode = nodeContainer.GetNode(obj.ListMember[1]);
            var intNode = targetNode[nameof(Class.IntMember)];
            var nodes = new IGraphNode[] { rootNode, rootNode[nameof(Class.ListMember)], rootNode[nameof(Class.ListMember)].Target, targetNode, intNode };
            Assert.NotNull(targetNode);
            Assert.NotNull(intNode);
            Assert.False(path.IsEmpty);
            AssertAreEqual(rootNode, path.RootNode);
            AssertAreEqual(intNode, path.GetNode());
            var i = 0;
            foreach (var node in path)
            {
                AssertAreEqual(nodes[i++], node);
            }
            AssertAreEqual(nodes.Length, i);
        }

        [Fact]
        public void TestGetParent()
        {
            var obj = new Class { StructMember = { StringMember = "aa" }, ClassMember = new Class(), ListMember = { new Class(), new Class(), new Class() } };
            var nodeContainer = new NodeContainer();
            var rootNode = nodeContainer.GetOrCreateNode(obj);

            var path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.IntMember));
            var parentPath = new GraphNodePath(rootNode);
            AssertAreEqual(parentPath, path.GetParent());

            path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.StructMember));
            path.PushMember(nameof(Struct.StringMember));
            parentPath = new GraphNodePath(rootNode);
            parentPath.PushMember(nameof(Class.StructMember));
            AssertAreEqual(parentPath, path.GetParent());

            path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ClassMember));
            path.PushTarget();
            parentPath = new GraphNodePath(rootNode);
            parentPath.PushMember(nameof(Class.ClassMember));
            AssertAreEqual(parentPath, path.GetParent());

            path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ClassMember));
            path.PushTarget();
            path.PushMember(nameof(Class.IntMember));
            parentPath = new GraphNodePath(rootNode);
            parentPath.PushMember(nameof(Class.ClassMember));
            parentPath.PushTarget();
            AssertAreEqual(parentPath, path.GetParent());

            path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ListMember));
            path.PushIndex(new NodeIndex(1));
            parentPath = new GraphNodePath(rootNode);
            parentPath.PushMember(nameof(Class.ListMember));
            AssertAreEqual(parentPath, path.GetParent());

            path = new GraphNodePath(rootNode);
            path.PushMember(nameof(Class.ListMember));
            path.PushIndex(new NodeIndex(1));
            path.PushMember(nameof(Class.IntMember));
            parentPath = new GraphNodePath(rootNode);
            parentPath.PushMember(nameof(Class.ListMember));
            parentPath.PushIndex(new NodeIndex(1));
            AssertAreEqual(parentPath, path.GetParent());
        }

        // NUnit does not use the Equals method for objects that implement IEnumerable, but that's what we want to use for GraphNodePath
        // ReSharper disable UnusedParameter.Local
        private static void AssertAreEqual(object expected, object actual)
        {
            Assert.True(expected.Equals(actual));
        }

        private static void AssertAreNotEqual(object expected, object actual)
        {
            Assert.False(expected.Equals(actual));
        }
        // ReSharper restore UnusedParameter.Local
    }
}
