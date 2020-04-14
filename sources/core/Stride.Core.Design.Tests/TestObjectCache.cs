// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Stride.Core.Design.Tests
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
    public class TestObjectCache
    {
        [Fact]
        public void TestConstruction()
        {
            var cache = new ObjectCache<Guid, object>();
            Assert.Equal(ObjectCache<Guid, object>.DefaultCacheSize, cache.Size);
            Assert.Empty(cache.History());
            Assert.Empty(cache.Cache());
            Assert.Equal(0, cache.CurrentAccessCount());

            cache = new ObjectCache<Guid, object>(16);
            Assert.Equal(16, cache.Size);
            Assert.Empty(cache.History());
            Assert.Empty(cache.Cache());
            Assert.Equal(0, cache.CurrentAccessCount());
        }

        [Fact]
        public void TestTryGet()
        {
            var cache = new ObjectCache<Guid, object>(4);
            var guid = Guid.NewGuid();
            var obj = new object();
            cache.Cache(guid, obj);
            var retrieved = cache.TryGet(guid);
            Assert.Single(cache.History());
            Assert.Single(cache.Cache());
            Assert.Equal(2, cache.CurrentAccessCount());
            Assert.Equal(2, cache.History().Single().Key);
            Assert.Equal(guid, cache.History().Single().Value);
            Assert.True(cache.Cache().ContainsKey(guid));
            Assert.Equal(obj, cache.Cache()[guid]);
            Assert.Equal(obj, retrieved);
            retrieved = cache.TryGet(Guid.NewGuid());
            Assert.Single(cache.History());
            Assert.Single(cache.Cache());
            Assert.Equal(2, cache.CurrentAccessCount());
            Assert.Equal(2, cache.History().Single().Key);
            Assert.Equal(guid, cache.History().Single().Value);
            Assert.True(cache.Cache().ContainsKey(guid));
            Assert.Equal(obj, cache.Cache()[guid]);
            Assert.Null(retrieved);
        }

        [Fact]
        public void TestCache()
        {
            var cache = new ObjectCache<Guid, object>(4);
            var guid = Guid.NewGuid();
            var obj = new object();
            cache.Cache(guid, obj);
            Assert.Single(cache.History());
            Assert.Single(cache.Cache());
            Assert.Equal(1, cache.CurrentAccessCount());
            Assert.Equal(1, cache.History().Single().Key);
            Assert.Equal(guid, cache.History().Single().Value);
            Assert.True(cache.Cache().ContainsKey(guid));
            Assert.Equal(obj, cache.Cache()[guid]);
        }

        [Fact]
        public void TestCache2Objs()
        {
            var cache = new ObjectCache<Guid, object>(2);
            var guid1 = Guid.NewGuid();
            var obj1 = new object();
            var guid2 = Guid.NewGuid();
            var obj2 = new object();
            cache.Cache(guid1, obj1);
            cache.Cache(guid2, obj2);
            Assert.Equal(2, cache.History().Count);
            Assert.Equal(2, cache.Cache().Count);
            Assert.Equal(2, cache.CurrentAccessCount());
            Assert.Equal(1, cache.History().First().Key);
            Assert.Equal(guid1, cache.History().First().Value);
            Assert.True(cache.Cache().ContainsKey(guid1));
            Assert.Equal(obj1, cache.Cache()[guid1]);
            Assert.Equal(2, cache.History().Last().Key);
            Assert.Equal(guid2, cache.History().Last().Value);
            Assert.Equal(guid2, cache.Cache().Last().Key);
            Assert.Equal(obj2, cache.Cache().Last().Value);
        }

        [Fact]
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
            Assert.Equal(2, cache.History().Count);
            Assert.Equal(2, cache.History().First().Key);
            Assert.Equal(guid2, cache.History().First().Value);
            Assert.True(cache.Cache().ContainsKey(guid2));
            Assert.Equal(obj2, cache.Cache()[guid2]);
            Assert.Equal(3, cache.History().Last().Key);
            Assert.Equal(guid3, cache.History().Last().Value);
            Assert.True(cache.Cache().ContainsKey(guid3));
            Assert.Equal(obj3, cache.Cache()[guid3]);
            Assert.Equal(3, cache.CurrentAccessCount());
        }
        [Fact]
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
            Assert.Equal(2, cache.History().Count);
            Assert.Equal(3, cache.History().First().Key);
            Assert.Equal(guid1, cache.History().First().Value);
            Assert.True(cache.Cache().ContainsKey(guid1));
            Assert.Equal(obj1, cache.Cache()[guid1]);
            Assert.Equal(4, cache.History().Last().Key);
            Assert.Equal(guid3, cache.History().Last().Value);
            Assert.True(cache.Cache().ContainsKey(guid3));
            Assert.Equal(obj3, cache.Cache()[guid3]);
            Assert.Equal(4, cache.CurrentAccessCount());
        }
    }
}
