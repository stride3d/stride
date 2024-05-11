// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Stride.Core.Presentation.Collections;
using Xunit;

namespace Stride.Core.Presentation.Tests
{
    public class TestObservableList
    {
        private static readonly string[] collectionPropertyNames = ["Count", "Item[]"];

        [Fact]
        public void TestEnumerableConstructor()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Equal(list.Count, set.Count);
            Assert.Equal("aaa", set[0]);
            Assert.Equal("bbb", set[1]);
            Assert.Equal("ccc", set[2]);
        }

        [Fact]
        public void TestIndexerSet()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list) { [1] = "ddd" };
            Assert.Equal(3, set.Count);
            Assert.Contains("aaa", set);
            Assert.Contains("ddd", set);
            Assert.Contains("ccc", set);
        }

        [Fact]
        public void TestAdd()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            Assert.Equal(set.Count, list.Count);
            ((INotifyPropertyChanged)set).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, collectionPropertyNames);
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(3, e.NewStartingIndex);
                Assert.NotNull(e.NewItems);
                Assert.Single(e.NewItems);
                Assert.Equal("ddd", e.NewItems[0]);
                collectionChangedInvoked = true;
            };
            set.Add("ddd");
            Assert.Equal("aaa", set[0]);
            Assert.Equal("bbb", set[1]);
            Assert.Equal("ccc", set[2]);
            Assert.Equal("ddd", set[3]);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestAddRange()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            ((INotifyPropertyChanged)set).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, collectionPropertyNames);
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
#if SUPPORT_RANGE_ACTION
                Assert.Equal(3, e.NewStartingIndex);
#else
                Assert.Contains(e.NewStartingIndex, new[] { 3, 4 });
#endif
                Assert.NotNull(e.NewItems);
#if SUPPORT_RANGE_ACTION
                Assert.Equal(2, e.NewItems.Count);
                Assert.Equal("ddd", e.NewItems[0]);
                Assert.Equal("eee", e.NewItems[1]);
#else
                Assert.Single(e.NewItems);
                Assert.Contains(e.NewItems[0], new[] { "ddd", "eee" });
#endif
                collectionChangedInvoked = true;
            };
            set.AddRange(["ddd", "eee"]);
            Assert.Equal(5, set.Count);
            Assert.Equal("aaa", set[0]);
            Assert.Equal("bbb", set[1]);
            Assert.Equal("ccc", set[2]);
            Assert.Equal("ddd", set[3]);
            Assert.Equal("eee", set[4]);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestClear()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            ((INotifyPropertyChanged)set).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, collectionPropertyNames);
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
                collectionChangedInvoked = true;
            };
            set.Clear();
            Assert.Empty(set);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestContains()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Contains("aaa", set);
            Assert.Contains("bbb", set);
            Assert.Contains("ccc", set);
            Assert.DoesNotContain("ddd", set);
        }

        [Fact]
        public void TestRemove()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            ((INotifyPropertyChanged)set).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, collectionPropertyNames);
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(1, e.OldStartingIndex);
                Assert.NotNull(e.OldItems);
                Assert.Single(e.OldItems);
                Assert.Equal("bbb", e.OldItems[0]);
                collectionChangedInvoked = true;
            };
            set.Remove("bbb");
            Assert.Equal(2, set.Count);
            Assert.Equal("aaa", set[0]);
            Assert.Equal("ccc", set[1]);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestIndexOf()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Equal(0, set.IndexOf("aaa"));
            Assert.Equal(1, set.IndexOf("bbb"));
            Assert.Equal(2, set.IndexOf("ccc"));
            Assert.Equal(-1, set.IndexOf("ddd"));
            set.Add("ddd");
            Assert.Equal(3, set.IndexOf("ddd"));
            set.Remove("bbb");
            Assert.Equal(-1, set.IndexOf("bbb"));
            Assert.Equal(2, set.IndexOf("ddd"));
        }

        [Fact]
        public void TestInsert()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            ((INotifyPropertyChanged)set).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, collectionPropertyNames);
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(1, e.NewStartingIndex);
                Assert.NotNull(e.NewItems);
                Assert.Single(e.NewItems);
                Assert.Equal("ddd", e.NewItems[0]);
                collectionChangedInvoked = true;
            };
            set.Insert(1, "ddd");
            Assert.Equal(4, set.Count);
            Assert.Equal("aaa", set[0]);
            Assert.Equal("ddd", set[1]);
            Assert.Equal("bbb", set[2]);
            Assert.Equal("ccc", set[3]);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }

        [Fact]
        public void TestRemoveAt()
        {
            var list = new List<string> { "aaa", "bbb", "ccc" };
            var set = new ObservableList<string>(list);
            Assert.Equal(set.Count, list.Count);
            bool propertyChangedInvoked = false;
            bool collectionChangedInvoked = false;
            ((INotifyPropertyChanged)set).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, collectionPropertyNames);
                propertyChangedInvoked = true;
            };
            set.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(1, e.OldStartingIndex);
                Assert.NotNull(e.OldItems);
                Assert.Single(e.OldItems);
                Assert.Equal("bbb", e.OldItems[0]);
                collectionChangedInvoked = true;
            };
            set.RemoveAt(1);
            Assert.Equal(2, set.Count);
            Assert.Equal("aaa", set[0]);
            Assert.Equal("ccc", set[1]);
            Assert.True(propertyChangedInvoked);
            Assert.True(collectionChangedInvoked);
        }
    }
}
