// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Xunit;
using Xenko.Core.Presentation.Collections;

namespace Xenko.Core.Presentation.Tests
{
    public class TestObservableSet
    {
        [Fact]
        public void TestEnumerableConstructor()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.Count, list.Count);
            Assert.Equal(set[0], "aaa");
            Assert.Equal(set[1], "bbb");
            Assert.Equal(set[2], "ccc");
        }

        [Fact]
        public void TestEnumerableConstructorWithDuplicate()
        {
            var list = new List<string> { "aaa", "bbb", "ccc", "bbb" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.Count, 3);
            Assert.Contains("aaa", set);
            Assert.Contains("bbb", set);
            Assert.Contains("ccc", set);
        }

        [Fact]
        public void TestIndexerSet()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list) { [1] = "ddd" };
            Assert.Equal(set.Count, 3);
            Assert.Contains("aaa", set);
            Assert.Contains("ddd", set);
            Assert.Contains("ccc", set);
        }

        [Fact]
        public void TestIndexerSetException()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Throws<InvalidOperationException>(() => set[1] = "ccc");
            Assert.Equal(set[0], "aaa");
            Assert.Equal(set[1], "bbb");
            Assert.Equal(set[2], "ccc");
        }

        [Fact]
        public void TestAdd()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            Assert.Equal(set.Count, list.Count);
            set.PropertyChanged += (sender, e) =>
            {
                Assert.Equal(e.PropertyName, nameof(ObservableSet<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(e.Action, NotifyCollectionChangedAction.Add);
                Assert.Equal(e.NewStartingIndex, 3);
                Assert.NotNull(e.NewItems);
                Assert.Equal(e.NewItems.Count, 1);
                Assert.Equal(e.NewItems[0], "ddd");
                collectionChangedInvoked = true;
            };
            set.Add("ddd");
            Assert.Equal(set[0], "aaa");
            Assert.Equal(set[1], "bbb");
            Assert.Equal(set[2], "ccc");
            Assert.Equal(set[3], "ddd");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestAddRange()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.Equal(e.PropertyName, nameof(ObservableSet<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(e.Action, NotifyCollectionChangedAction.Add);
                Assert.Equal(e.NewStartingIndex, 3);
                Assert.NotNull(e.NewItems);
                Assert.Equal(e.NewItems.Count, 2);
                Assert.Equal(e.NewItems[0], "ddd");
                Assert.Equal(e.NewItems[1], "eee");
                collectionChangedInvoked = true;
            };
            set.AddRange(new[] { "ddd", "eee" });
            Assert.Equal(set.Count, 5);
            Assert.Equal(set[0], "aaa");
            Assert.Equal(set[1], "bbb");
            Assert.Equal(set[2], "ccc");
            Assert.Equal(set[3], "ddd");
            Assert.Equal(set[4], "eee");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestClear()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.Equal(e.PropertyName, nameof(ObservableSet<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(e.Action, NotifyCollectionChangedAction.Reset);
                collectionChangedInvoked = true;
            };
            set.Clear();
            Assert.Equal(set.Count, 0);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestContains()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Contains("aaa", set);
            Assert.Contains("bbb", set);
            Assert.Contains("ccc", set);
            Assert.DoesNotContain("ddd", set);
        }

        [Fact]
        public void TestRemove()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.Equal(e.PropertyName, nameof(ObservableSet<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(e.Action, NotifyCollectionChangedAction.Remove);
                Assert.Equal(e.OldStartingIndex, 1);
                Assert.NotNull(e.OldItems);
                Assert.Equal(e.OldItems.Count, 1);
                Assert.Equal(e.OldItems[0], "bbb");
                collectionChangedInvoked = true;
            };
            set.Remove("bbb");
            Assert.Equal(set.Count, 2);
            Assert.Equal(set[0], "aaa");
            Assert.Equal(set[1], "ccc");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestIndexOf()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.IndexOf("aaa"), 0);
            Assert.Equal(set.IndexOf("bbb"), 1);
            Assert.Equal(set.IndexOf("ccc"), 2);
            Assert.Equal(set.IndexOf("ddd"), -1);
            set.Add("ddd");
            Assert.Equal(set.IndexOf("ddd"), 3);
            set.Remove("bbb");
            Assert.Equal(set.IndexOf("bbb"), -1);
            Assert.Equal(set.IndexOf("ddd"), 2);
        }

        [Fact]
        public void TestInsert()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.Equal(e.PropertyName, nameof(ObservableSet<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(e.Action, NotifyCollectionChangedAction.Add);
                Assert.Equal(e.NewStartingIndex, 1);
                Assert.NotNull(e.NewItems);
                Assert.Equal(e.NewItems.Count, 1);
                Assert.Equal(e.NewItems[0], "ddd");
                collectionChangedInvoked = true;
            };
            set.Insert(1, "ddd");
            Assert.Equal(set.Count, 4);
            Assert.Equal(set[0], "aaa");
            Assert.Equal(set[1], "ddd");
            Assert.Equal(set[2], "bbb");
            Assert.Equal(set[3], "ccc");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestRemoveAt()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableSet<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            set.PropertyChanged += (sender, e) =>
            {
                Assert.Equal(e.PropertyName, nameof(ObservableSet<string>.Count));
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(e.Action, NotifyCollectionChangedAction.Remove);
                Assert.Equal(e.OldStartingIndex, 1);
                Assert.NotNull(e.OldItems);
                Assert.Equal(e.OldItems.Count, 1);
                Assert.Equal(e.OldItems[0], "bbb");
                collectionChangedInvoked = true;
            };
            set.RemoveAt(1);
            Assert.Equal(set.Count, 2);
            Assert.Equal(set[0], "aaa");
            Assert.Equal(set[1], "ccc");
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }
    }
}
