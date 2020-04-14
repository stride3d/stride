// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xunit;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum.Tests
{
    public class TestPrimitiveMembers
    {
        public enum TestEnum
        {
            Value1,
            Value2,
            Value3,
        }

        public class IntMember
        {
            public int Member { get; set; }
        }

        public class StringMember
        {
            public string Member { get; set; }
        }

        public class GuidMember
        {
            public Guid Member { get; set; }
        }

        public class EnumMember
        {
            public TestEnum Member { get; set; }
        }

        public class PrimitiveClass
        {
            public int Value { get; set; }
        }

        public struct PrimitiveStruct
        {
            public int Value { get; set; }
        }

        public class RegisteredPrimitiveClassMember
        {
            public PrimitiveClass Member { get; set; }
        }
        public class RegisteredPrimitiveStructMember
        {
            public PrimitiveStruct Member { get; set; }
        }

        public class BoxedPrimitiveMember
        {
            public object Member { get; set; }
        }

        [Fact]
        public void TestIntMember()
        {
            var obj = new IntMember { Member = 5 };
            var container = new NodeContainer();

            // Construction
            var containerNode = container.GetOrCreateNode(obj);
            Helper.TestNonCollectionObjectNode(containerNode, obj, 1);
            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(IntMember.Member), false);

            // Update from object
            obj.Member = 6;
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(IntMember.Member), false);

            // Update from Quantum
            containerNode.Members.First().Update(7);
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(IntMember.Member), false);
        }

        [Fact]
        public void TestStringMember()
        {
            var obj = new StringMember { Member = "aaa" };
            var container = new NodeContainer();

            // Construction
            var containerNode = container.GetOrCreateNode(obj);
            Helper.TestNonCollectionObjectNode(containerNode, obj, 1);
            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(StringMember.Member), false);

            // Update from object
            obj.Member = "bbb";
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(StringMember.Member), false);

            // Update from Quantum
            containerNode.Members.First().Update("ccc");
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(StringMember.Member), false);
        }

        [Fact]
        public void TestGuidMember()
        {
            var obj = new GuidMember { Member = Guid.NewGuid() };
            var container = new NodeContainer();

            // Construction
            var containerNode = container.GetOrCreateNode(obj);
            Helper.TestNonCollectionObjectNode(containerNode, obj, 1);
            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(GuidMember.Member), false);

            // Update from object
            obj.Member = Guid.NewGuid();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(EnumMember.Member), false);

            // Update from Quantum
            containerNode.Members.First().Update(Guid.NewGuid());
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(EnumMember.Member), false);
        }

        [Fact]
        public void TestEnumMember()
        {
            var obj = new EnumMember { Member = TestEnum.Value1 };
            var container = new NodeContainer();

            // Construction
            var containerNode = container.GetOrCreateNode(obj);
            Helper.TestNonCollectionObjectNode(containerNode, obj, 1);
            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(EnumMember.Member), false);

            // Update from object
            obj.Member = TestEnum.Value2;
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(EnumMember.Member), false);

            // Update from Quantum
            containerNode.Members.First().Update(TestEnum.Value3);
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(EnumMember.Member), false);
        }

        [Fact]
        public void TestRegisteredPrimitiveClassMember()
        {
            var obj = new RegisteredPrimitiveClassMember { Member = new PrimitiveClass { Value = 1 } };
            var container = new NodeContainer();
            container.NodeBuilder.RegisterPrimitiveType(typeof(PrimitiveClass));

            // Construction
            var containerNode = container.GetOrCreateNode(obj);
            Helper.TestNonCollectionObjectNode(containerNode, obj, 1);
            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(RegisteredPrimitiveClassMember.Member), false);

            // Update from object
            obj.Member = new PrimitiveClass { Value = 2 };
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(RegisteredPrimitiveClassMember.Member), false);

            // Update from Quantum
            containerNode.Members.First().Update(new PrimitiveClass { Value = 3 });
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(RegisteredPrimitiveClassMember.Member), false);
        }

        [Fact]
        public void TestRegisteredPrimitiveStructMember()
        {
            var obj = new RegisteredPrimitiveStructMember { Member = new PrimitiveStruct { Value = 1 } };
            var container = new NodeContainer();
            container.NodeBuilder.RegisterPrimitiveType(typeof(PrimitiveStruct));

            // Construction
            var containerNode = container.GetOrCreateNode(obj);
            Helper.TestNonCollectionObjectNode(containerNode, obj, 1);
            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(RegisteredPrimitiveStructMember.Member), false);

            // Update from object
            obj.Member = new PrimitiveStruct { Value = 2 };
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(RegisteredPrimitiveStructMember.Member), false);

            // Update from Quantum
            containerNode.Members.Last().Update(new PrimitiveStruct { Value = 3 });
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(RegisteredPrimitiveStructMember.Member), false);
        }

        [Fact]
        public void TestBoxedPrimitiveMember()
        {
            var obj = new BoxedPrimitiveMember { Member = 1.0f };
            var container = new NodeContainer();

            // Construction
            var containerNode = container.GetOrCreateNode(obj);
            Helper.TestNonCollectionObjectNode(containerNode, obj, 1);
            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(BoxedPrimitiveMember.Member), true);
            Assert.NotNull(memberNode.Target);
            Assert.Equal(0, memberNode.Target.Members.Count);
            Assert.Null(memberNode.Target.Indices);
            Assert.Equal(TypeDescriptorFactory.Default.Find(typeof(float)), memberNode.Target.Descriptor);
            Assert.False(memberNode.Target.IsReference);
            Assert.Null(memberNode.Target.ItemReferences);
            Assert.Equal(typeof(float), memberNode.Target.Type);
            Assert.Equal(1.0f, memberNode.Target.Retrieve());

            // Update from object (note: value WILL mismatch here due to the boxing node, so we don't test the value of the target node)
            obj.Member = 2.0f;
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(BoxedPrimitiveMember.Member), true);
            Assert.True(memberNode.IsReference);
            Assert.NotNull(memberNode.Target);
            Assert.Equal(0, memberNode.Target.Members.Count);
            Assert.Null(memberNode.Target.Indices);
            Assert.Equal(TypeDescriptorFactory.Default.Find(typeof(float)), memberNode.Target.Descriptor);
            Assert.False(memberNode.Target.IsReference);
            Assert.Null(memberNode.Target.ItemReferences);
            Assert.Equal(typeof(float), memberNode.Target.Type);

            // Update from Quantum
            containerNode.Members.Last().Update(3.0f);
            Helper.TestMemberNode(containerNode, memberNode, obj, obj.Member, nameof(BoxedPrimitiveMember.Member), true);
            Assert.True(memberNode.IsReference);
            Assert.NotNull(memberNode.Target);
            Assert.Equal(0, memberNode.Target.Members.Count);
            Assert.Null(memberNode.Target.Indices);
            Assert.Equal(TypeDescriptorFactory.Default.Find(typeof(float)), memberNode.Target.Descriptor);
            Assert.False(memberNode.Target.IsReference);
            Assert.Null(memberNode.Target.ItemReferences);
            Assert.Equal(typeof(float), memberNode.Target.Type);
            Assert.Equal(3.0f, memberNode.Target.Retrieve());
        }
    }
}
