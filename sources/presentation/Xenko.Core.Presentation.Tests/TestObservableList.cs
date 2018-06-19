// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Collections.Specialized;
using NUnit.Framework;
using Xenko.Core.Presentation.Collections;

namespace Xenko.Core.Presentation.Tests
{
    [TestFixture]
    class TestObservableList
    {
        [Test]
        public void TestEnumerableConstructor()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.AreEqual(set.Count, list.Count);
            Assert.AreEqual(set[0], "aaa");
            Assert.AreEqual(set[1], "bbb");
            Assert.AreEqual(set[2], "ccc");
        }

        [Test]
        public void TestIndexerSet()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list) { [1] = "ddd" };
            Assert.AreEqual(set.Count, 3);
            Assert.That(set.Contains("aaa"));
            Assert.That(set.Contains("ddd"));
            Assert.That(set.Contains("ccc"));
        }

        [Test]
        public void TestAdd()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            Assert.AreEqual(set.Count, list.Count);
            set.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(e.PropertyName, nameof(ObservableList<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.AreEqual(e.Action, NotifyCollectionChangedAction.Add);
                Assert.AreEqual(e.NewStartingIndex, 3);
                Assert.NotNull(e.NewItems);
                Assert.AreEqual(e.NewItems.Count, 1);
                Assert.AreEqual(e.NewItems[0], "ddd");
                collectionChangedInvoked = true;
            };
            set.Add("ddd");
            Assert.AreEqual(set[0], "aaa");
            Assert.AreEqual(set[1], "bbb");
            Assert.AreEqual(set[2], "ccc");
            Assert.AreEqual(set[3], "ddd");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Test]
        public void TestAddRange()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.AreEqual(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(e.PropertyName, nameof(ObservableList<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.AreEqual(e.Action, NotifyCollectionChangedAction.Add);
                Assert.AreEqual(e.NewStartingIndex, 3);
                Assert.NotNull(e.NewItems);
                Assert.AreEqual(e.NewItems.Count, 2);
                Assert.AreEqual(e.NewItems[0], "ddd");
                Assert.AreEqual(e.NewItems[1], "eee");
                collectionChangedInvoked = true;
            };
            set.AddRange(new[] { "ddd", "eee" });
            Assert.AreEqual(set.Count, 5);
            Assert.AreEqual(set[0], "aaa");
            Assert.AreEqual(set[1], "bbb");
            Assert.AreEqual(set[2], "ccc");
            Assert.AreEqual(set[3], "ddd");
            Assert.AreEqual(set[4], "eee");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Test]
        public void TestClear()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.AreEqual(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(e.PropertyName, nameof(ObservableList<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.AreEqual(e.Action, NotifyCollectionChangedAction.Reset);
                collectionChangedInvoked = true;
            };
            set.Clear();
            Assert.AreEqual(set.Count, 0);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Test]
        public void TestContains()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.That(set.Contains("aaa"));
            Assert.That(set.Contains("bbb"));
            Assert.That(set.Contains("ccc"));
            Assert.That(!set.Contains("ddd"));
        }

        [Test]
        public void TestRemove()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.AreEqual(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(e.PropertyName, nameof(ObservableList<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.AreEqual(e.Action, NotifyCollectionChangedAction.Remove);
                Assert.AreEqual(e.OldStartingIndex, 1);
                Assert.NotNull(e.OldItems);
                Assert.AreEqual(e.OldItems.Count, 1);
                Assert.AreEqual(e.OldItems[0], "bbb");
                collectionChangedInvoked = true;
            };
            set.Remove("bbb");
            Assert.AreEqual(set.Count, 2);
            Assert.AreEqual(set[0], "aaa");
            Assert.AreEqual(set[1], "ccc");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Test]
        public void TestIndexOf()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.AreEqual(set.IndexOf("aaa"), 0);
            Assert.AreEqual(set.IndexOf("bbb"), 1);
            Assert.AreEqual(set.IndexOf("ccc"), 2);
            Assert.AreEqual(set.IndexOf("ddd"), -1);
            set.Add("ddd");
            Assert.AreEqual(set.IndexOf("ddd"), 3);
            set.Remove("bbb");
            Assert.AreEqual(set.IndexOf("bbb"), -1);
            Assert.AreEqual(set.IndexOf("ddd"), 2);
        }

        [Test]
        public void TestInsert()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.AreEqual(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(e.PropertyName, nameof(ObservableList<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.AreEqual(e.Action, NotifyCollectionChangedAction.Add);
                Assert.AreEqual(e.NewStartingIndex, 1);
                Assert.NotNull(e.NewItems);
                Assert.AreEqual(e.NewItems.Count, 1);
                Assert.AreEqual(e.NewItems[0], "ddd");
                collectionChangedInvoked = true;
            };
            set.Insert(1, "ddd");
            Assert.AreEqual(set.Count, 4);
            Assert.AreEqual(set[0], "aaa");
            Assert.AreEqual(set[1], "ddd");
            Assert.AreEqual(set[2], "bbb");
            Assert.AreEqual(set[3], "ccc");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Test]
        public void TestRemoveAt()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.AreEqual(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(e.PropertyName, nameof(ObservableList<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.AreEqual(e.Action, NotifyCollectionChangedAction.Remove);
                Assert.AreEqual(e.OldStartingIndex, 1);
                Assert.NotNull(e.OldItems);
                Assert.AreEqual(e.OldItems.Count, 1);
                Assert.AreEqual(e.OldItems[0], "bbb");
                collectionChangedInvoked = true;
            };
            set.RemoveAt(1);
            Assert.AreEqual(set.Count, 2);
            Assert.AreEqual(set[0], "aaa");
            Assert.AreEqual(set[1], "ccc");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }
    }
}
