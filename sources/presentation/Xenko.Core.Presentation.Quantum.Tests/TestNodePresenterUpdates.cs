// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xenko.Core.Presentation.Quantum.Tests.Helpers;
using Xenko.Core.Quantum;

namespace Xenko.Core.Presentation.Quantum.Tests
{
    public class TestNodePresenterUpdates
    {
        [Fact]
        public void TestPrimitiveMemberUpdate()
        {
            var instance = new Types.SimpleType { String = "aaa" };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children.Single().UpdateValue("bbb");
            Assert.Equal(1, presenter.Children.Count);
            var child = presenter.Children.Single();
            Assert.Equal("String", child.Name);
            Assert.Equal(presenter, child.Parent);
            Assert.Equal(0, child.Children.Count);
            Assert.Equal(typeof(string), child.Type);
            Assert.Equal("bbb", child.Value);

            presenter.Children.Single().UpdateValue("ccc");
            Assert.Equal(1, presenter.Children.Count);
            child = presenter.Children.Single();
            Assert.Equal("String", child.Name);
            Assert.Equal(presenter, child.Parent);
            Assert.Equal(0, child.Children.Count);
            Assert.Equal(typeof(string), child.Type);
            Assert.Equal("ccc", child.Value);
        }

        [Fact]
        public void TestReferenceMemberUpdate()
        {
            var instance = new Types.ClassWithRef { String = "aaa", Ref = new Types.ClassWithRef { String = "bbb" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].UpdateValue(new Types.ClassWithRef { String = "ccc" });
            Assert.Equal(2, presenter.Children.Count);
            Assert.Equal(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.Equal(instance.Ref, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.Equal(instance.Ref, presenter.Children[1].Value);
            Assert.Equal("String", presenter.Children[1].Children[0].Name);
            Assert.Equal("ccc", presenter.Children[1].Children[0].Value);
            Assert.Equal(typeof(Types.ClassWithRef), presenter.Children[1].Children[1].Type);
            Assert.Equal(instance.Ref.Ref, presenter.Children[1].Children[1].Value);

            presenter.Children[1].UpdateValue(new Types.ClassWithRef { String = "ddd" });
            Assert.Equal(2, presenter.Children.Count);
            Assert.Equal(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.Equal(instance.Ref, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.Equal(instance.Ref, presenter.Children[1].Value);
            Assert.Equal("String", presenter.Children[1].Children[0].Name);
            Assert.Equal("ddd", presenter.Children[1].Children[0].Value);
            Assert.Equal(typeof(Types.ClassWithRef), presenter.Children[1].Children[1].Type);
            Assert.Equal(instance.Ref.Ref, presenter.Children[1].Children[1].Value);
        }

        [Fact]
        public void TestPrimitiveListUpdate()
        {
            var instance = new Types.ClassWithCollection { String = "aaa", List = { "bbb", "ccc" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter[nameof(Types.ClassWithCollection.List)].Children[1].UpdateValue("ddd");
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[1].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithCollection.List)].Children[1].UpdateValue("eee");
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithCollection.List)].Children[0].UpdateValue("fff");
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("fff", presenter.Children[1].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
        }

        [Fact]
        public void TestPrimitiveListAdd()
        {
            var instance = new Types.ClassWithCollection { String = "aaa", List = { "bbb", "ccc" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].AddItem("ddd", new NodeIndex(2));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(3, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[1].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[2].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].AddItem("eee", new NodeIndex(1));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(4, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("3", presenter.Children[1].Children[3].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[2].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[3].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[3].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.Equal(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].AddItem("fff", new NodeIndex(0));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(5, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("3", presenter.Children[1].Children[3].Name);
            Assert.Equal("4", presenter.Children[1].Children[4].Name);
            Assert.Equal("fff", presenter.Children[1].Children[0].Value);
            Assert.Equal("bbb", presenter.Children[1].Children[1].Value);
            Assert.Equal("eee", presenter.Children[1].Children[2].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[3].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[4].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[3].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[4].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.Equal(instance.List[3], presenter.Children[1].Children[3].Value);
            Assert.Equal(instance.List[4], presenter.Children[1].Children[4].Value);
        }

        [Fact]
        public void TestPrimitiveListRemove()
        {
            var instance = new Types.ClassWithCollection { String = "aaa", List = { "bbb", "ccc", "ddd", "eee", "fff" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].RemoveItem("fff", new NodeIndex(4));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(4, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("3", presenter.Children[1].Children[3].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[1].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[2].Value);
            Assert.Equal("eee", presenter.Children[1].Children[3].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[3].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.Equal(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].RemoveItem("bbb", new NodeIndex(0));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(3, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("ccc", presenter.Children[1].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[1].Value);
            Assert.Equal("eee", presenter.Children[1].Children[2].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].RemoveItem("ddd", new NodeIndex(1));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("ccc", presenter.Children[1].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter.Children[1].RemoveItem("eee", new NodeIndex(1));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(1, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("ccc", presenter.Children[1].Children[0].Value);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);

            presenter.Children[1].RemoveItem("ccc", new NodeIndex(0));
            Assert.Equal(typeof(List<string>), presenter.Children[1].Type);
            Assert.Equal(0, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
        }

        [Fact]
        public void TestReferenceListUpdate()
        {
            var instance = new Types.ClassWithRefCollection { String = "aaa", List = { new Types.SimpleType { String = "bbb" }, new Types.SimpleType { String = "ccc" } } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter[nameof(Types.ClassWithRefCollection.List)].Children[1].UpdateValue(new Types.SimpleType { String = "ddd" });
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithRefCollection.List)].Children[1].UpdateValue(new Types.SimpleType { String = "eee" });
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithRefCollection.List)].Children[0].UpdateValue(new Types.SimpleType { String = "fff" });
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("fff", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
        }

        [Fact]
        public void TestReferenceListAdd()
        {
            var instance = new Types.ClassWithRefCollection { String = "aaa", List = { new Types.SimpleType { String = "bbb" }, new Types.SimpleType { String = "ccc" } } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].AddItem(new Types.SimpleType { String = "ddd" }, new NodeIndex(2));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(3, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[2].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].AddItem(new Types.SimpleType { String = "eee" }, new NodeIndex(1));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(4, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("3", presenter.Children[1].Children[3].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[2].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[3].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[3].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[3].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.Equal(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].AddItem(new Types.SimpleType { String = "fff" }, new NodeIndex(0));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal(5, presenter.Children[1].Children.Count);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("3", presenter.Children[1].Children[3].Name);
            Assert.Equal("4", presenter.Children[1].Children[4].Name);
            Assert.Equal("fff", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("bbb", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[2].Children[0].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[3].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[4].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[3].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[4].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[3].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[4].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.Equal(instance.List[3], presenter.Children[1].Children[3].Value);
            Assert.Equal(instance.List[4], presenter.Children[1].Children[4].Value);
        }

        [Fact]
        public void TestReferenceListRemove()
        {
            var instance = new Types.ClassWithRefCollection { String = "aaa", List = { new Types.SimpleType { String = "bbb" }, new Types.SimpleType { String = "ccc" }, new Types.SimpleType { String = "ddd" }, new Types.SimpleType { String = "eee" }, new Types.SimpleType { String = "fff" }, } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].RemoveItem(instance.List[4], new NodeIndex(4));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(4, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("ccc", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[2].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[3].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[3].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[3].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.Equal(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].RemoveItem("bbb", new NodeIndex(0));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(3, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("2", presenter.Children[1].Children[2].Name);
            Assert.Equal("ccc", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("ddd", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[2].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.Equal(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].RemoveItem("ddd", new NodeIndex(1));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(2, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("1", presenter.Children[1].Children[1].Name);
            Assert.Equal("ccc", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.Equal(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter.Children[1].RemoveItem("eee", new NodeIndex(1));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(1, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
            Assert.Equal("0", presenter.Children[1].Children[0].Name);
            Assert.Equal("ccc", presenter.Children[1].Children[0].Children[0].Value);
            Assert.Equal(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.Equal(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.Equal(instance.List[0], presenter.Children[1].Children[0].Value);

            presenter.Children[1].RemoveItem("ccc", new NodeIndex(0));
            Assert.Equal(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.Equal(0, presenter.Children[1].Children.Count);
            Assert.Equal(instance.List, presenter.Children[1].Value);
        }

        private static TestInstanceContext BuildContext(object instance)
        {
            var context = new TestContainerContext();
            return context.CreateInstanceContext(instance);
        }
    }
}
