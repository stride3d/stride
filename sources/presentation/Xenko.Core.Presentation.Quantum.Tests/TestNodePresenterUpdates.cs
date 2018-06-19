// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xenko.Core.Presentation.Quantum.Tests.Helpers;
using Xenko.Core.Quantum;

namespace Xenko.Core.Presentation.Quantum.Tests
{
    [TestFixture]
    public class TestNodePresenterUpdates
    {
        [Test]
        public void TestPrimitiveMemberUpdate()
        {
            var instance = new Types.SimpleType { String = "aaa" };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children.Single().UpdateValue("bbb");
            Assert.AreEqual(1, presenter.Children.Count);
            var child = presenter.Children.Single();
            Assert.AreEqual("String", child.Name);
            Assert.AreEqual(presenter, child.Parent);
            Assert.AreEqual(0, child.Children.Count);
            Assert.AreEqual(typeof(string), child.Type);
            Assert.AreEqual("bbb", child.Value);

            presenter.Children.Single().UpdateValue("ccc");
            Assert.AreEqual(1, presenter.Children.Count);
            child = presenter.Children.Single();
            Assert.AreEqual("String", child.Name);
            Assert.AreEqual(presenter, child.Parent);
            Assert.AreEqual(0, child.Children.Count);
            Assert.AreEqual(typeof(string), child.Type);
            Assert.AreEqual("ccc", child.Value);
        }

        [Test]
        public void TestReferenceMemberUpdate()
        {
            var instance = new Types.ClassWithRef { String = "aaa", Ref = new Types.ClassWithRef { String = "bbb" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].UpdateValue(new Types.ClassWithRef { String = "ccc" });
            Assert.AreEqual(2, presenter.Children.Count);
            Assert.AreEqual(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.AreEqual(instance.Ref, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.AreEqual(instance.Ref, presenter.Children[1].Value);
            Assert.AreEqual("String", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("ccc", presenter.Children[1].Children[0].Value);
            Assert.AreEqual(typeof(Types.ClassWithRef), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(instance.Ref.Ref, presenter.Children[1].Children[1].Value);

            presenter.Children[1].UpdateValue(new Types.ClassWithRef { String = "ddd" });
            Assert.AreEqual(2, presenter.Children.Count);
            Assert.AreEqual(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.AreEqual(instance.Ref, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual(typeof(Types.ClassWithRef), presenter.Children[1].Type);
            Assert.AreEqual(instance.Ref, presenter.Children[1].Value);
            Assert.AreEqual("String", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("ddd", presenter.Children[1].Children[0].Value);
            Assert.AreEqual(typeof(Types.ClassWithRef), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(instance.Ref.Ref, presenter.Children[1].Children[1].Value);
        }

        [Test]
        public void TestPrimitiveListUpdate()
        {
            var instance = new Types.ClassWithCollection { String = "aaa", List = { "bbb", "ccc" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter[nameof(Types.ClassWithCollection.List)].Children[1].UpdateValue("ddd");
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[1].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithCollection.List)].Children[1].UpdateValue("eee");
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithCollection.List)].Children[0].UpdateValue("fff");
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("fff", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
        }

        [Test]
        public void TestPrimitiveListAdd()
        {
            var instance = new Types.ClassWithCollection { String = "aaa", List = { "bbb", "ccc" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].AddItem("ddd", new Index(2));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(3, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[1].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[2].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].AddItem("eee", new Index(1));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(4, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("3", presenter.Children[1].Children[3].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[2].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[3].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[3].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.AreEqual(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].AddItem("fff", new Index(0));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(5, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("3", presenter.Children[1].Children[3].Name);
            Assert.AreEqual("4", presenter.Children[1].Children[4].Name);
            Assert.AreEqual("fff", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("bbb", presenter.Children[1].Children[1].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[2].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[3].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[4].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[3].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[4].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.AreEqual(instance.List[3], presenter.Children[1].Children[3].Value);
            Assert.AreEqual(instance.List[4], presenter.Children[1].Children[4].Value);
        }

        [Test]
        public void TestPrimitiveListRemove()
        {
            var instance = new Types.ClassWithCollection { String = "aaa", List = { "bbb", "ccc", "ddd", "eee", "fff" } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].RemoveItem("fff", new Index(4));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(4, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("3", presenter.Children[1].Children[3].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[1].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[2].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[3].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[3].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.AreEqual(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].RemoveItem("bbb", new Index(0));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(3, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("ccc", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[1].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[2].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].RemoveItem("ddd", new Index(1));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("ccc", presenter.Children[1].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter.Children[1].RemoveItem("eee", new Index(1));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(1, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("ccc", presenter.Children[1].Children[0].Value);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);

            presenter.Children[1].RemoveItem("ccc", new Index(0));
            Assert.AreEqual(typeof(List<string>), presenter.Children[1].Type);
            Assert.AreEqual(0, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
        }

        [Test]
        public void TestReferenceListUpdate()
        {
            var instance = new Types.ClassWithRefCollection { String = "aaa", List = { new Types.SimpleType { String = "bbb" }, new Types.SimpleType { String = "ccc" } } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter[nameof(Types.ClassWithRefCollection.List)].Children[1].UpdateValue(new Types.SimpleType { String = "ddd" });
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithRefCollection.List)].Children[1].UpdateValue(new Types.SimpleType { String = "eee" });
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter[nameof(Types.ClassWithRefCollection.List)].Children[0].UpdateValue(new Types.SimpleType { String = "fff" });
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("fff", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
        }

        [Test]
        public void TestReferenceListAdd()
        {
            var instance = new Types.ClassWithRefCollection { String = "aaa", List = { new Types.SimpleType { String = "bbb" }, new Types.SimpleType { String = "ccc" } } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].AddItem(new Types.SimpleType { String = "ddd" }, new Index(2));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(3, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[2].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].AddItem(new Types.SimpleType { String = "eee" }, new Index(1));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(4, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("3", presenter.Children[1].Children[3].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[2].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[3].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[3].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[3].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.AreEqual(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].AddItem(new Types.SimpleType { String = "fff" }, new Index(0));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual(5, presenter.Children[1].Children.Count);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("3", presenter.Children[1].Children[3].Name);
            Assert.AreEqual("4", presenter.Children[1].Children[4].Name);
            Assert.AreEqual("fff", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("bbb", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[2].Children[0].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[3].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[4].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[3].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[4].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[3].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[4].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.AreEqual(instance.List[3], presenter.Children[1].Children[3].Value);
            Assert.AreEqual(instance.List[4], presenter.Children[1].Children[4].Value);
        }

        [Test]
        public void TestReferenceListRemove()
        {
            var instance = new Types.ClassWithRefCollection { String = "aaa", List = { new Types.SimpleType { String = "bbb" }, new Types.SimpleType { String = "ccc" }, new Types.SimpleType { String = "ddd" }, new Types.SimpleType { String = "eee" }, new Types.SimpleType { String = "fff" }, } };
            var context = BuildContext(instance);
            var presenter = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));

            presenter.Children[1].RemoveItem(instance.List[4], new Index(4));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(4, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("bbb", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("ccc", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[2].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[3].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[3].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[3].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);
            Assert.AreEqual(instance.List[3], presenter.Children[1].Children[3].Value);

            presenter.Children[1].RemoveItem("bbb", new Index(0));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(3, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("2", presenter.Children[1].Children[2].Name);
            Assert.AreEqual("ccc", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("ddd", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[2].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[2].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[2].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);
            Assert.AreEqual(instance.List[2], presenter.Children[1].Children[2].Value);

            presenter.Children[1].RemoveItem("ddd", new Index(1));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(2, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("1", presenter.Children[1].Children[1].Name);
            Assert.AreEqual("ccc", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual("eee", presenter.Children[1].Children[1].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[1].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[1].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);
            Assert.AreEqual(instance.List[1], presenter.Children[1].Children[1].Value);

            presenter.Children[1].RemoveItem("eee", new Index(1));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(1, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
            Assert.AreEqual("0", presenter.Children[1].Children[0].Name);
            Assert.AreEqual("ccc", presenter.Children[1].Children[0].Children[0].Value);
            Assert.AreEqual(typeof(Types.SimpleType), presenter.Children[1].Children[0].Type);
            Assert.AreEqual(typeof(string), presenter.Children[1].Children[0].Children[0].Type);
            Assert.AreEqual(instance.List[0], presenter.Children[1].Children[0].Value);

            presenter.Children[1].RemoveItem("ccc", new Index(0));
            Assert.AreEqual(typeof(List<Types.SimpleType>), presenter.Children[1].Type);
            Assert.AreEqual(0, presenter.Children[1].Children.Count);
            Assert.AreEqual(instance.List, presenter.Children[1].Value);
        }

        private static TestInstanceContext BuildContext(object instance)
        {
            var context = new TestContainerContext();
            return context.CreateInstanceContext(instance);
        }
    }
}
