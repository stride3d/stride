// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Xenko.Core.Design.Tests
{
    public static class ObjectCacheExtension
    {
        public static SortedList<long, TKey> History<TKey, TValue>(this ObjectCache<TKey, TValue> cache) where TKey : IEquatable<TKey> where TValue : class
        {
            return (SortedList<long, TKey>)GetField(cache, "accessHistory").GetValue(cache);
        }

        public static Dictionary<TKey, TValue> Cache<TKey, TValue>(this ObjectCache<TKey, TValue> cache) where TKey : IEquatable<TKey> where TValue : class
        {
            return (Dictionary<TKey, TValue>)GetField(cache, "cache").GetValue(cache);
        }

        public static long CurrentAccessCount<TKey, TValue>(this ObjectCache<TKey, TValue> cache) where TKey : IEquatable<TKey> where TValue : class
        {
            return (long)GetField(cache, "currentAccessCount").GetValue(cache);
        }

        private static FieldInfo GetField<TKey, TValue>(ObjectCache<TKey, TValue> cache, string fieldName) where TKey : IEquatable<TKey> where TValue : class
        {
            var field = cache.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new TypeLoadException();
            return field;
        }
    }

    /// <summary>
    /// Tests for the <see cref="ObjectCache{TKey, TValue}"/> class.
    /// </summary>
    [TestFixture]
    public class TestObjectCache
    {
        [Test]
        public void TestConstruction()
        {
            var cache = new ObjectCache<Guid, object>();
            Assert.AreEqual(ObjectCache<Guid, object>.DefaultCacheSize, cache.Size);
            Assert.AreEqual(0, cache.History().Count);
            Assert.AreEqual(0, cache.Cache().Count);
            Assert.AreEqual(0, cache.CurrentAccessCount());

            cache = new ObjectCache<Guid, object>(16);
            Assert.AreEqual(16, cache.Size);
            Assert.AreEqual(0, cache.History().Count);
            Assert.AreEqual(0, cache.Cache().Count);
            Assert.AreEqual(0, cache.CurrentAccessCount());
        }

        [Test]
        public void TestTryGet()
        {
            var cache = new ObjectCache<Guid, object>(4);
            var guid = Guid.NewGuid();
            var obj = new object();
            cache.Cache(guid, obj);
            var retrieved = cache.TryGet(guid);
            Assert.AreEqual(1, cache.History().Count);
            Assert.AreEqual(1, cache.Cache().Count);
            Assert.AreEqual(2, cache.CurrentAccessCount());
            Assert.AreEqual(2, cache.History().Single().Key);
            Assert.AreEqual(guid, cache.History().Single().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid));
            Assert.AreEqual(obj, cache.Cache()[guid]);
            Assert.AreEqual(obj, retrieved);
            retrieved = cache.TryGet(Guid.NewGuid());
            Assert.AreEqual(1, cache.History().Count);
            Assert.AreEqual(1, cache.Cache().Count);
            Assert.AreEqual(2, cache.CurrentAccessCount());
            Assert.AreEqual(2, cache.History().Single().Key);
            Assert.AreEqual(guid, cache.History().Single().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid));
            Assert.AreEqual(obj, cache.Cache()[guid]);
            Assert.AreEqual(null, retrieved);
        }

        [Test]
        public void TestCache()
        {
            var cache = new ObjectCache<Guid, object>(4);
            var guid = Guid.NewGuid();
            var obj = new object();
            cache.Cache(guid, obj);
            Assert.AreEqual(1, cache.History().Count);
            Assert.AreEqual(1, cache.Cache().Count);
            Assert.AreEqual(1, cache.CurrentAccessCount());
            Assert.AreEqual(1, cache.History().Single().Key);
            Assert.AreEqual(guid, cache.History().Single().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid));
            Assert.AreEqual(obj, cache.Cache()[guid]);
        }

        [Test]
        public void TestCache2Objs()
        {
            var cache = new ObjectCache<Guid, object>(2);
            var guid1 = Guid.NewGuid();
            var obj1 = new object();
            var guid2 = Guid.NewGuid();
            var obj2 = new object();
            cache.Cache(guid1, obj1);
            cache.Cache(guid2, obj2);
            Assert.AreEqual(2, cache.History().Count);
            Assert.AreEqual(2, cache.Cache().Count);
            Assert.AreEqual(2, cache.CurrentAccessCount());
            Assert.AreEqual(1, cache.History().First().Key);
            Assert.AreEqual(guid1, cache.History().First().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid1));
            Assert.AreEqual(obj1, cache.Cache()[guid1]);
            Assert.AreEqual(2, cache.History().Last().Key);
            Assert.AreEqual(guid2, cache.History().Last().Value);
            Assert.AreEqual(guid2, cache.Cache().Last().Key);
            Assert.AreEqual(obj2, cache.Cache().Last().Value);
        }

        [Test]
        public void TestCacheOverflow()
        {
            var cache = new ObjectCache<Guid, object>(2);
            var guid1 = Guid.NewGuid();
            var obj1 = new object();
            var guid2 = Guid.NewGuid();
            var obj2 = new object();
            var guid3 = Guid.NewGuid();
            var obj3 = new object();
            cache.Cache(guid1, obj1);
            cache.Cache(guid2, obj2);
            cache.Cache(guid3, obj3);
            Assert.AreEqual(2, cache.History().Count);
            Assert.AreEqual(2, cache.History().First().Key);
            Assert.AreEqual(guid2, cache.History().First().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid2));
            Assert.AreEqual(obj2, cache.Cache()[guid2]);
            Assert.AreEqual(3, cache.History().Last().Key);
            Assert.AreEqual(guid3, cache.History().Last().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid3));
            Assert.AreEqual(obj3, cache.Cache()[guid3]);
            Assert.AreEqual(3, cache.CurrentAccessCount());
        }
        [Test]
        public void TestCacheAccessAndOverflow()
        {
            var cache = new ObjectCache<Guid, object>(2);
            var guid1 = Guid.NewGuid();
            var obj1 = new object();
            var guid2 = Guid.NewGuid();
            var obj2 = new object();
            var guid3 = Guid.NewGuid();
            var obj3 = new object();
            cache.Cache(guid1, obj1);
            cache.Cache(guid2, obj2);
            cache.TryGet(guid1);
            cache.Cache(guid3, obj3);
            Assert.AreEqual(2, cache.History().Count);
            Assert.AreEqual(3, cache.History().First().Key);
            Assert.AreEqual(guid1, cache.History().First().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid1));
            Assert.AreEqual(obj1, cache.Cache()[guid1]);
            Assert.AreEqual(4, cache.History().Last().Key);
            Assert.AreEqual(guid3, cache.History().Last().Value);
            Assert.AreEqual(true, cache.Cache().ContainsKey(guid3));
            Assert.AreEqual(obj3, cache.Cache()[guid3]);
            Assert.AreEqual(4, cache.CurrentAccessCount());
        }
    }
}
