// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xunit;
using Stride.Core;
using Stride.Core.Presentation.Quantum.Tests.Helpers;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Tests
{
    public class TestNodePresenterProperties
    {
        [DataContract]
        public class SimpleMember
        {
            public float FloatValue { get; set; }
        }

        [DataContract]
        public class SimpleMemberWithContract
        {
            [DataMember(10)]
            public float FloatValue { get; set; }
        }

        public class NestedMemberClass
        {
            [DataMember(20)]
            public SimpleMemberWithContract MemberClass { get; set; } = new SimpleMemberWithContract();
        }

        public class NestedReadonlyMemberClass
        {
            [DataMember(30)]
            public SimpleMemberWithContract MemberClass { get; } = new SimpleMemberWithContract();
        }

        public class ListMember
        {
            [DataMember(40)]
            public List<string> List { get; set; }
        }

        [Fact]
        public void TestSimpleMember()
        {
            var instance = new SimpleMember { FloatValue = 1.0f };
            var context = BuildContext(instance);
            var root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
            var member = root[nameof(SimpleMember.FloatValue)];
            Assert.Equal(0, member.Children.Count);
            Assert.Equal(nameof(SimpleMember.FloatValue), member.DisplayName);
            Assert.Equal(NodeIndex.Empty, member.Index);
            Assert.False(member.IsEnumerable);
            Assert.False(member.IsReadOnly);
            Assert.True(member.IsVisible);
            Assert.Equal(nameof(SimpleMember.FloatValue), member.Name);
            Assert.Null(member.Order);
            Assert.Equal(root, member.Parent);
            Assert.Equal(1.0f, member.Value);
        }

        [Fact]
        public void TestSimpleMemberWithContract()
        {
            var instance = new SimpleMemberWithContract { FloatValue = 1.0f };
            var context = BuildContext(instance);
            var root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
            var member = root[nameof(SimpleMember.FloatValue)];
            Assert.Equal(0, member.Children.Count);
            Assert.Equal(nameof(SimpleMember.FloatValue), member.DisplayName);
            Assert.Equal(NodeIndex.Empty, member.Index);
            Assert.False(member.IsEnumerable);
            Assert.False(member.IsReadOnly);
            Assert.True(member.IsVisible);
            Assert.Equal(nameof(SimpleMember.FloatValue), member.Name);
            Assert.Equal(10, member.Order);
            Assert.Equal(root, member.Parent);
            Assert.Equal(1.0f, member.Value);
        }

        [Fact]
        public void TestNestedMember()
        {
            var instance = new NestedMemberClass { MemberClass = { FloatValue = 1.0f } };
            var context = BuildContext(instance);
            var root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
            var member = root[nameof(NestedMemberClass.MemberClass)];
            Assert.Equal(1, member.Children.Count);
            Assert.Equal(nameof(NestedMemberClass.MemberClass), member.DisplayName);
            Assert.Equal(NodeIndex.Empty, member.Index);
            Assert.False(member.IsEnumerable);
            Assert.False(member.IsReadOnly);
            Assert.True(member.IsVisible);
            Assert.Equal(nameof(NestedMemberClass.MemberClass), member.Name);
            Assert.Equal(20, member.Order);
            Assert.Equal(root, member.Parent);
            Assert.Equal(instance.MemberClass, member.Value);
            var innerMember = member[nameof(SimpleMember.FloatValue)];
            Assert.Equal(0, innerMember.Children.Count);
            Assert.Equal(nameof(SimpleMember.FloatValue), innerMember.DisplayName);
            Assert.Equal(NodeIndex.Empty, innerMember.Index);
            Assert.False(innerMember.IsEnumerable);
            Assert.False(innerMember.IsReadOnly);
            Assert.True(innerMember.IsVisible);
            Assert.Equal(nameof(SimpleMember.FloatValue), innerMember.Name);
            Assert.Equal(10, innerMember.Order);
            Assert.Equal(member, innerMember.Parent);
            Assert.Equal(1.0f, innerMember.Value);
        }

        [Fact]
        public void TestNestedReadOnlyMember()
        {
            var instance = new NestedReadonlyMemberClass { MemberClass = { FloatValue = 1.0f } };
            var context = BuildContext(instance);
            var root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
            var member = root[nameof(NestedMemberClass.MemberClass)];
            Assert.Equal(1, member.Children.Count);
            Assert.Equal(nameof(NestedMemberClass.MemberClass), member.DisplayName);
            Assert.Equal(NodeIndex.Empty, member.Index);
            Assert.False(member.IsEnumerable);
            Assert.True(member.IsReadOnly);
            Assert.True(member.IsVisible);
            Assert.Equal(nameof(NestedMemberClass.MemberClass), member.Name);
            Assert.Equal(30, member.Order);
            Assert.Equal(root, member.Parent);
            Assert.Equal(instance.MemberClass, member.Value);
            var innerMember = member[nameof(SimpleMember.FloatValue)];
            Assert.Equal(0, innerMember.Children.Count);
            Assert.Equal(nameof(SimpleMember.FloatValue), innerMember.DisplayName);
            Assert.Equal(NodeIndex.Empty, innerMember.Index);
            Assert.False(innerMember.IsEnumerable);
            Assert.False(innerMember.IsReadOnly);
            Assert.True(innerMember.IsVisible);
            Assert.Equal(nameof(SimpleMember.FloatValue), innerMember.Name);
            Assert.Equal(10, innerMember.Order);
            Assert.Equal(member, innerMember.Parent);
            Assert.Equal(1.0f, innerMember.Value);
        }

        [Fact]
        public void TestListMember()
        {
            var instance = new ListMember { List = new List<string>() };
            var context = BuildContext(instance);
            var root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
            var member = root[nameof(ListMember.List)];
            Assert.Equal(0, member.Children.Count);
            Assert.Equal(nameof(ListMember.List), member.DisplayName);
            Assert.Equal(NodeIndex.Empty, member.Index);
            Assert.True(member.IsEnumerable);
            Assert.False(member.IsReadOnly);
            Assert.True(member.IsVisible);
            Assert.Equal(nameof(ListMember.List), member.Name);
            Assert.Equal(40, member.Order);
            Assert.Equal(root, member.Parent);
            Assert.Equal(instance.List, member.Value);

            instance = new ListMember();
            context = BuildContext(instance);
            root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
            member = root[nameof(ListMember.List)];
            Assert.Equal(0, member.Children.Count);
            Assert.Equal(nameof(ListMember.List), member.DisplayName);
            Assert.Equal(NodeIndex.Empty, member.Index);
            Assert.False(member.IsEnumerable);
            Assert.False(member.IsReadOnly);
            Assert.True(member.IsVisible);
            Assert.Equal(nameof(ListMember.List), member.Name);
            Assert.Equal(40, member.Order);
            Assert.Equal(root, member.Parent);
            Assert.Equal(instance.List, member.Value);
        }

        private static TestInstanceContext BuildContext(object instance)
        {
            var context = new TestContainerContext();
            return context.CreateInstanceContext(instance);
        }
    }
}
