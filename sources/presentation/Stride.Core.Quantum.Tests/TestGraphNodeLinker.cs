// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum.Tests
{
    public class TestGraphNodeLinker
    {
        public class SimpleClass
        {
            public int Member1;
            public SimpleClass Member2;
        }

        public class InterfaceMember
        {
            public int Member1;
            public IInterface Member2;
        }

        public interface IInterface
        {
            int Member1Common { get; set; }
        }

        public class Implem1 : IInterface
        {
            public int Member1Common { get; set; }
            public SimpleClass Member2Implem1 { get; set; }
        }

        public class Implem2 : IInterface
        {
            public int Member1Common { get; set; }
            public SimpleClass Member2Implem2 { get; set; }
        }

        public class StructClass
        {
            public int Member1;
            public Struct Member2;
        }

        public class PrimitiveListClass
        {
            public int Member1;
            public List<string> Member2;
        }

        public class ObjectListClass
        {
            public int Member1;
            public List<SimpleClass> Member2;
        }

        public class StructListClass
        {
            public int Member1;
            public List<Struct> Member2;
        }

        public struct Struct
        {
            public string Member1;
            public SimpleClass Member2;
        }

        public class TestLinker : GraphNodeLinker
        {
            public Dictionary<IGraphNode, IGraphNode> LinkedNodes = new Dictionary<IGraphNode, IGraphNode>();

            protected override void LinkNodes(IGraphNode sourceNode, IGraphNode targetNode)
            {
                LinkedNodes.Add(sourceNode, targetNode);
                base.LinkNodes(sourceNode, targetNode);
            }
        }

        public class CustomFindTargetLinker : TestLinker
        {
            private readonly IObjectNode root;

            public CustomFindTargetLinker(NodeContainer container, IObjectNode root)
            {
                this.root = root;
                CustomTarget = container.GetOrCreateNode(new SimpleClass());
            }

            public IObjectNode CustomTarget { get; }

            protected override IGraphNode FindTarget(IGraphNode sourceNode)
            {
                if (sourceNode is IObjectNode && sourceNode.Type == typeof(SimpleClass) && sourceNode != root)
                {
                    return CustomTarget;
                }
                return base.FindTarget(sourceNode);
            }
        }

        public class CustomFindTargetReferenceLinker : TestLinker
        {
            public override ObjectReference FindTargetReference(IGraphNode sourceNode, IGraphNode targetNode, ObjectReference sourceReference)
            {
                if (sourceReference.Index.IsEmpty)
                    return base.FindTargetReference(sourceNode, targetNode, sourceReference);

                var matchValue = 0;
                if (sourceReference.TargetNode != null)
                    matchValue = (int)sourceReference.TargetNode[nameof(SimpleClass.Member1)].Retrieve();

                var targetReference = (targetNode as IObjectNode)?.ItemReferences;
                return targetReference?.FirstOrDefault(x => (int)x.TargetNode[nameof(SimpleClass.Member1)].Retrieve() == matchValue);
            }
        }

        [Fact]
        public void TestSimpleObject()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new SimpleClass { Member1 = 3, Member2 = new SimpleClass() };
            var instance2 = new SimpleClass { Member1 = 4, Member2 = new SimpleClass() };
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new TestLinker();
            linker.LinkGraph(source, target);
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target, target[nameof(SimpleClass.Member2)].Target },
                { source[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member2)] },
            };
            VerifyLinks(expectedLinks, linker);
        }

        [Fact]
        public void TestObjectWithListOfReferences()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new ObjectListClass { Member1 = 3, Member2 = new List<SimpleClass> { new SimpleClass(), new SimpleClass() } };
            var instance2 = new ObjectListClass { Member1 = 3, Member2 = new List<SimpleClass> { new SimpleClass(), new SimpleClass() } };
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new TestLinker();
            linker.LinkGraph(source, target);
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target, target[nameof(SimpleClass.Member2)].Target },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0)), target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0)) },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1)), target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1)) },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member2)] },
            };
            VerifyLinks(expectedLinks, linker);
        }

        [Fact]
        public void TestSimpleObjectWithNullInTarget()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new SimpleClass { Member1 = 3, Member2 = new SimpleClass() };
            var instance2 = new SimpleClass { Member1 = 4, Member2 = null };
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new TestLinker();
            linker.LinkGraph(source, target);
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target, null },
                { source[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member1)], null },
                { source[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member2)], null },
            };
            VerifyLinks(expectedLinks, linker);
        }

        [Fact]
        public void TestObjectWithStruct()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new StructClass { Member1 = 3, Member2 = new Struct { Member2 = new SimpleClass() } };
            var instance2 = new StructClass { Member1 = 3, Member2 = new Struct { Member2 = new SimpleClass() } };
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new TestLinker();
            linker.LinkGraph(source, target);
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(StructClass.Member1)], target[nameof(StructClass.Member1)] },
                { source[nameof(StructClass.Member2)], target[nameof(StructClass.Member2)] },
                { source[nameof(StructClass.Member2)].Target, target[nameof(StructClass.Member2)].Target },
                { source[nameof(StructClass.Member2)].Target[nameof(Struct.Member1)], target[nameof(StructClass.Member2)].Target[nameof(Struct.Member1)] },
                { source[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)], target[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)] },
                { source[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)].Target, target[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)].Target },
                { source[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)].Target[nameof(SimpleClass.Member1)], target[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)].Target[nameof(SimpleClass.Member1)] },
                { source[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)].Target[nameof(SimpleClass.Member2)], target[nameof(StructClass.Member2)].Target[nameof(Struct.Member2)].Target[nameof(SimpleClass.Member2)] },
            };
            VerifyLinks(expectedLinks, linker);
        }

        [Fact]
        public void TestInterfaceMemberDifferentImplementations()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new InterfaceMember { Member1 = 3, Member2 = new Implem1 { Member2Implem1 = new SimpleClass() } };
            var instance2 = new InterfaceMember { Member1 = 3, Member2 = new Implem2 { Member2Implem2 = new SimpleClass() } };
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new TestLinker();
            linker.LinkGraph(source, target);
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(InterfaceMember.Member1)], target[nameof(InterfaceMember.Member1)] },
                { source[nameof(InterfaceMember.Member2)], target[nameof(InterfaceMember.Member2)] },
                { source[nameof(InterfaceMember.Member2)].Target, target[nameof(InterfaceMember.Member2)].Target },
                { source[nameof(InterfaceMember.Member2)].Target[nameof(Implem1.Member1Common)], target[nameof(InterfaceMember.Member2)].Target[nameof(Implem1.Member1Common)] },
                { source[nameof(InterfaceMember.Member2)].Target[nameof(Implem1.Member2Implem1)], null },
                { source[nameof(InterfaceMember.Member2)].Target[nameof(Implem1.Member2Implem1)].Target, null },
                { source[nameof(InterfaceMember.Member2)].Target[nameof(Implem1.Member2Implem1)].Target[nameof(SimpleClass.Member1)], null },
                { source[nameof(InterfaceMember.Member2)].Target[nameof(Implem1.Member2Implem1)].Target[nameof(SimpleClass.Member2)], null },
            };
            VerifyLinks(expectedLinks, linker);
        }

        [Fact]
        public void TestCustomFindTarget()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new SimpleClass { Member1 = 3, Member2 = new SimpleClass() };
            var instance2 = new SimpleClass { Member1 = 4, Member2 = new SimpleClass() };
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new CustomFindTargetLinker(nodeContainer, source);
            linker.LinkGraph(source, target);
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target, linker.CustomTarget },
                { source[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member1)], linker.CustomTarget[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)].Target[nameof(SimpleClass.Member2)], linker.CustomTarget[nameof(SimpleClass.Member2)] },
            };
            VerifyLinks(expectedLinks, linker);
        }

        [Fact]
        public void TestCustomFindTargetReference()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new ObjectListClass { Member1 = 3, Member2 = new List<SimpleClass> { new SimpleClass { Member1 = 1 }, new SimpleClass { Member1 = 2 }, new SimpleClass { Member1 = 3 } } };
            var instance2 = new ObjectListClass { Member1 = 3, Member2 = new List<SimpleClass> { new SimpleClass { Member1 = 2 }, new SimpleClass { Member1 = 4 }, new SimpleClass { Member1 = 1 } } };
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new CustomFindTargetReferenceLinker();
            linker.LinkGraph(source, target);
            // Expected links by index: 0 -> 2, 1 -> 0, 2 -> null
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target, target[nameof(SimpleClass.Member2)].Target },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0)), target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(2)) },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(2))[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(2))[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1)), target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0)) },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(1))[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(0))[nameof(SimpleClass.Member2)] },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(2)), null },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(2))[nameof(SimpleClass.Member1)], null },
                { source[nameof(SimpleClass.Member2)].Target.IndexedTarget(new NodeIndex(2))[nameof(SimpleClass.Member2)], null },
            };
            VerifyLinks(expectedLinks, linker);
        }

        [Fact]
        public void TestReentrancy()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new SimpleClass { Member1 = 3 };
            var instance2 = new SimpleClass { Member1 = 4 };
            instance1.Member2 = instance1;
            instance2.Member2 = instance2;
            var source = nodeContainer.GetOrCreateNode(instance1);
            var target = nodeContainer.GetOrCreateNode(instance2);
            var linker = new TestLinker();
            linker.LinkGraph(source, target);
            var expectedLinks = new Dictionary<IGraphNode, IGraphNode>
            {
                { source, target },
                { source[nameof(SimpleClass.Member1)], target[nameof(SimpleClass.Member1)] },
                { source[nameof(SimpleClass.Member2)], target[nameof(SimpleClass.Member2)] },
            };
            VerifyLinks(expectedLinks, linker);
        }

        private static void VerifyLinks(Dictionary<IGraphNode, IGraphNode> expectedLinks, TestLinker linker)
        {
            Assert.Equal(expectedLinks.Count, linker.LinkedNodes.Count);
            foreach (var link in expectedLinks)
            {
                IGraphNode actualTarget;
                Assert.True(linker.LinkedNodes.TryGetValue(link.Key, out actualTarget));
                Assert.Equal(link.Value, actualTarget);
            }
        }
    }
}
