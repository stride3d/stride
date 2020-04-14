// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Xenko.Core;

namespace Xenko.Rendering
{
    public static class ParameterKeys
    {
        private static long keysVersion = 0;
        private static long keyByNamesVersion = 0;
        private static readonly List<ParameterKey> keys = new List<ParameterKey>();
        private static readonly Dictionary<string, ParameterKey> keyByNames = new Dictionary<string, ParameterKey>();
        private static readonly Dictionary<ParameterComposedKey, ParameterKey> composedKeys = new Dictionary<ParameterComposedKey, ParameterKey>();

        /// <summary>
        /// Returns property keys matching a given type
        /// </summary>
        /// <param name="keyType">Type of the key.</param>
        /// <returns></returns>
        public static IEnumerable<ParameterKey> GetKeys()
        {
            lock (keys)
            lock (keyByNames)
            {
                if (keyByNamesVersion != keysVersion)
                {
                    // If anything changed, repopulate the list (we can't do incrementally since dictionary aren't ordered)
                    keys.Clear();
                    foreach (var key in keyByNames)
                    {
                        // Ignore key whose name contains more than one . or a [ (they are composed keys)
                        if (key.Key.Count(c => c == '.') > 1 || key.Key.Contains('['))
                            continue;

                        keys.Add(key.Value);
                    }
                    keysVersion = keyByNamesVersion;
                }

                return keys.ToList();
            }
        }

        public static PermutationParameterKey<T> NewPermutation<T>(T defaultValue = default(T), string name = null)
        {
            if (name == null)
                name = string.Empty;

            var length = typeof(T).IsArray ? (defaultValue != null ? ((Array)(object)defaultValue).Length : -1) : 1;
            return new PermutationParameterKey<T>(name, length, new ParameterKeyValueMetadata<T>(defaultValue));
        }

        public static ValueParameterKey<T> NewValue<T>(T defaultValue = default(T), string name = null) where T : struct
        {
            if (name == null)
                name = string.Empty;

            return new ValueParameterKey<T>(name, 1, new ParameterKeyValueMetadata<T>(defaultValue));
        }

        public static ObjectParameterKey<T> NewObject<T>(T defaultValue = default(T), string name = null)
        {
            if (name == null)
                name = string.Empty;

            var length = typeof(T).IsArray ? (defaultValue != null ? ((Array)(object)defaultValue).Length : -1) : 1;
            return new ObjectParameterKey<T>(name, length, new ParameterKeyValueMetadata<T>(defaultValue));
        }

        /// <summary>
        /// Creates the key with specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public static T IndexedKey<T>(T key, int index) where T : ParameterKey
        {
            if (index == 0)
                return key;

            string keyName;

            if (key.Name[key.Name.Length - 1] == '0')
            {
                keyName = key.Name.Substring(0, key.Name.Length - 1) + index;
            }
            else
            {
                keyName = key.Name + index;
            }

            return (T)Activator.CreateInstance(typeof(T), keyName, key.Length, key.Metadatas);
        }

        public static ParameterKey Merge(ParameterKey key, Type ownerType, string name)
        {
            lock (keyByNames)
            {
                /*if (keyByNames.TryGetValue(name, out duplicateKey))
                {
                    if (duplicateKey.PropertyType != key.PropertyType)
                    {
                        // TODO: For now, throw an exception, but we should be nicer about it
                        // (log and allow the two keys to co-exist peacefully?)
                        throw new InvalidOperationException("Two ParameterKey with same name but different types have been initialized.");
                    }
                    return key;
                }*/

                if (string.IsNullOrEmpty(key.Name))
                    key.SetName(name);
                keyByNames[name] = key;
                keyByNamesVersion++;
                
                if (key.OwnerType == null && ownerType != null)
                    key.SetOwnerType(ownerType);
            }
            return key;
        }

        /// <summary>
        /// Compose a key with a name (e.g  if key is `MyKey.MyKeyName` and name is `MyName`, the result key will be `MyKey.MyKeyName.MyName`)
        /// </summary>
        /// <typeparam name="T">Type of the key value</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="name">The name to append to the key.</param>
        /// <returns>The composition of key and name</returns>
        /// <exception cref="System.ArgumentNullException">
        /// key
        /// or
        /// name
        /// </exception>
        public static T ComposeWith<T>(this T key, string name) where T : ParameterKey
        {
            if (key == null) throw new ArgumentNullException("key");
            if (name == null) throw new ArgumentNullException("name");
            var composedKey = new ParameterComposedKey(key, name, -1);
            return (T)GetOrCreateComposedKey(ref composedKey);
        }

        /// <summary>
        /// Compose a key with a name and index (e.g  if key is `MyKey.MyKeyName` and name is `MyName` and index is 5, the result key will be `MyKey.MyKeyName.MyName[5]`)
        /// </summary>
        /// <typeparam name="T">Type of the key value</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="name">The name to append to the key.</param>
        /// <param name="index">The index.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// key
        /// or
        /// name
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">index;Must be >= 0</exception>
        public static T ComposeIndexer<T>(this T key, string name, int index) where T : ParameterKey
        {
            if (key == null) throw new ArgumentNullException("key");
            if (name == null) throw new ArgumentNullException("name");
            var composedKey = new ParameterComposedKey(key, name, index);
            return (T)GetOrCreateComposedKey(ref composedKey);
        }

        private static ParameterKey GetOrCreateComposedKey(ref ParameterComposedKey composedKey)
        {
            ParameterKey result;
            lock (composedKeys)
            {
                if (!composedKeys.TryGetValue(composedKey, out result))
                {
                    var keyName = composedKey.Key.Name;

                    var builder = new StringBuilder(keyName.Length + 10 + composedKey.Name.Length);
                    builder.Append(keyName);
                    builder.Append('.');
                    builder.Append(composedKey.Name);
                    if (composedKey.Indexer >= 0)
                    {
                        builder.Append('[');
                        builder.Append(composedKey.Indexer);
                        builder.Append(']');
                    }

                    result = ComposeWith(composedKey.Key, builder);
                    composedKeys.Add(composedKey, result);
                }
            }
            return result;
        }

        private static ParameterKey ComposeWith(ParameterKey key, StringBuilder builder)
        {
            var newKey = FindByName(builder.ToString());
            if (newKey == null)
            {
                throw new ArgumentException("Key [{0}] must be a registered key".ToFormat(key));
            }
            return newKey;
        }

        public static ParameterKey TryFindByName(string name)
        {
            if (name == null)
            {
                return null;
            }

            lock (keyByNames)
            {
                ParameterKey key;
                keyByNames.TryGetValue(name, out key);
                return key;
            }
        }

        public static ParameterKey FindByName(string name)
        {
            // name must be XXX.YYY{.ZZZ}
            // where ZZZ can be any identifiers separated by dots but we don't check this.
            lock (keyByNames)
            {
                ParameterKey key;
                keyByNames.TryGetValue(name, out key);
                if (key == null)
                {
                    // firstDot index between XXX and YYY
                    var firstDot = name.IndexOf('.');
                    if (firstDot == -1)
                        return null;

                    // dot after YYY
                    var subKeyNameIndex = name.IndexOf('.', firstDot + 1);

                    // keyName => XXX.YYY
                    string keyName = name; 
                    string subKeyName = null;
                    if (subKeyNameIndex >= 0)
                    {
                        keyName = name.Substring(0, subKeyNameIndex);
                        // subKeyName => ZZZ
                        subKeyName = name.Substring(subKeyNameIndex);
                    }

                    // It is possible this key has been appended with mixin path (i.e. Test becomes Test.mixin[0])
                    // if it was not a "stage" value
                    if (keyByNames.TryGetValue(keyName, out key) && subKeyName != null)
                    {
                        var baseParameterKeyType = key.GetType();
                        while (baseParameterKeyType.GetGenericTypeDefinition() != typeof(ParameterKey<>))
                            baseParameterKeyType = baseParameterKeyType.GetTypeInfo().BaseType;

                        // Get default value and use it for the new subkey
                        var defaultValue = key.DefaultValueMetadata.GetDefaultValue();

                        // Create metadata
                        var metadataParameters = defaultValue != null ? new[] { defaultValue } : new object[0]; 
                        var metadata = Activator.CreateInstance(typeof(ParameterKeyValueMetadata<>).MakeGenericType(baseParameterKeyType.GetTypeInfo().GenericTypeArguments[0]), metadataParameters);

                        var args = new[] { name, key.Length, metadata };
                        key = (ParameterKey)Activator.CreateInstance(key.GetType(), args);

                        // Register key. Also register real name in case it was remapped.
                        keyByNames[name] = key;
                        keyByNamesVersion++;
                    }
                }

                return key;
            }
        }

        private struct ParameterComposedKey : IEquatable<ParameterComposedKey>
        {
            public readonly ParameterKey Key;

            public readonly string Name;

            public readonly int Indexer;

            private readonly int hashCode;

            public ParameterComposedKey(ParameterKey key, string name, int indexer)
            {
                Key = key;
                Name = name;
                Indexer = indexer;

                unchecked
                {
                    hashCode = Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ Name.GetHashCode();
                    hashCode = (hashCode * 397) ^ Indexer;
                }
            }

            public bool Equals(ParameterComposedKey other)
            {
                return Key.Equals(other.Key) && string.Equals(Name, other.Name) && Indexer == other.Indexer;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ParameterComposedKey && Equals((ParameterComposedKey)obj);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public static bool operator ==(ParameterComposedKey left, ParameterComposedKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ParameterComposedKey left, ParameterComposedKey right)
            {
                return !left.Equals(right);
            }
        }
    }
}
