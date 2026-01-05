// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Stride.Rendering
{
    public static partial class ParameterKeys
    {
        private static long keysVersion = 0;
        private static long keyByNamesVersion = 0;

        private static readonly List<ParameterKey> keys = [];  // All the parameter keys

        private static readonly Dictionary<string, ParameterKey> keyByNames = [];
        private static readonly Dictionary<ParameterComposedKey, ParameterKey> composedKeys = [];


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
                    // If anything changed, repopulate the list (we can't do incrementally since dictionaries aren't ordered)
                    keys.Clear();
                    foreach ((string keyName, ParameterKey key) in keyByNames)
                    {
                        // Ignore the key whose name contains more than one '.' or a '[' (they are composed keys)
                        if (keyName.AsSpan().Count('.') > 1 || keyName.Contains('['))
                            continue;

                        keys.Add(key);
                    }
                    keysVersion = keyByNamesVersion;
                }

                return [.. keys];
            }
        }


        public static PermutationParameterKey<T> NewPermutation<T>(T defaultValue = default, string? name = null)
        {
            name ??= string.Empty;

            var length = typeof(T).IsArray
                ? defaultValue is not null ? ((Array)(object) defaultValue).Length : -1
                : 1;

            return new PermutationParameterKey<T>(name, length, new ParameterKeyValueMetadata<T>(defaultValue));
        }

        public static ValueParameterKey<T> NewValue<T>(T defaultValue = default, string? name = null) where T : unmanaged
        {
            name ??= string.Empty;

            return new ValueParameterKey<T>(name, 1, new ParameterKeyValueMetadata<T>(defaultValue));
        }

        public static ObjectParameterKey<T> NewObject<T>(T defaultValue = default, string? name = null)
        {
            name ??= string.Empty;

            var length = typeof(T).IsArray
                ? defaultValue is not null ? ((Array)(object) defaultValue).Length : -1
                : 1;

            return new ObjectParameterKey<T>(name, length, new ParameterKeyValueMetadata<T>(defaultValue));
        }


        /// <summary>
        /// Creates the key with specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public static T NewIndexedKey<T>(T key, int index) where T : ParameterKey
        {
            if (index == 0)
                return key;

            var keyName = key.Name[^1] == '0'
                ? key.Name[..^1] + index
                : key.Name + index;

            return (T) Activator.CreateInstance(typeof(T), keyName, key.Length, key.Metadatas);
        }


        public static ParameterKey Merge(ParameterKey key, Type? ownerType, string name)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            lock (keyByNames)
            {
                // TODO: We should probably check if the key is already registered with the same name and type. Could it not cause problems in case of name clash?

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

                // Ensure the name is properly interned and cached / hashed
                if (string.IsNullOrEmpty(key.Name))
                    key.SetName(name);

                keyByNames[name] = key;
                keyByNamesVersion++;

                // Ensure the key is registered with a valid owner type
                if (key.OwnerType is null && ownerType is not null)
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
        public static TKey ComposeWith<TKey>(this TKey parameterKey, string name) where TKey : ParameterKey
        {
            ArgumentNullException.ThrowIfNull(parameterKey);
            ArgumentNullException.ThrowIfNull(name);

            var composedKey = new ParameterComposedKey(parameterKey, name, indexer: -1);
            return (TKey) GetOrCreateComposedKey(ref composedKey);
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
        public static TKey ComposeIndexer<TKey>(this TKey parameterKey, string name, int index) where TKey : ParameterKey
        {
            ArgumentNullException.ThrowIfNull(parameterKey);
            ArgumentNullException.ThrowIfNull(name);

            var composedKey = new ParameterComposedKey(parameterKey, name, index);
            return (TKey) GetOrCreateComposedKey(in composedKey);
        }

        private static ParameterKey GetOrCreateComposedKey(ref readonly ParameterComposedKey composedKey)
        {
            lock (composedKeys)
            {
                // If already registered the composition, return it
                if (composedKeys.TryGetValue(composedKey, out ParameterKey result))
                    return result;

                // Otherwise, create a new key based on the composed key
                var keyName = composedKey.Key.Name;

                var composedNameLength = keyName.Length + 1 + composedKey.Name.Length;

                scoped Span<char> indexerSpan = [];

                if (composedKey.Indexer is int indexer)
                {
                    indexerSpan = stackalloc char[11];  // Max char count for Int32 (-2147483648)

                    if (indexer.TryFormat(indexerSpan, out int charsWritten))
                    {
                        composedNameLength += charsWritten + 2;  // For the brackets []
                    }
                }

                scoped Span<char> composedNameSpan = stackalloc char[composedNameLength];
                scoped Span<char> finalNameSpan = composedNameSpan;

                keyName.AsSpan().CopyTo(composedNameSpan);
                composedNameSpan[keyName.Length] = '.';
                composedNameSpan = composedNameSpan[(keyName.Length + 1)..];

                composedKey.Name.AsSpan().CopyTo(composedNameSpan);
                composedNameSpan = composedNameSpan[composedKey.Name.Length..];

                if (!indexerSpan.IsEmpty)
                {
                    composedNameSpan[0] = '[';
                    indexerSpan.CopyTo(composedNameSpan[1..]);
                    composedNameSpan[^1] = ']';
                }

                string composedName = finalNameSpan.ToString();

                var newComposedParameterKey = FindByName(composedName);
                if (newComposedParameterKey is null)
                    ThrowComposedKeyNotFoundException(composedKey.Key);

                composedKeys.Add(composedKey, newComposedParameterKey);
                return newComposedParameterKey;

                //
                // Throws an exception indicating that the composed key was not found.
                //
                [DoesNotReturn]
                static void ThrowComposedKeyNotFoundException(ParameterKey parameterKey)
                {
                    throw new ArgumentException($"Parameter key [{parameterKey}] must be a registered key.");
                }
            }
        }

        public static ParameterKey? TryFindByName(string name)
        {
            if (name is null)
                return null;

            lock (keyByNames)
            {
                keyByNames.TryGetValue(name, out ParameterKey key);
                return key;
            }
        }

        public static ParameterKey FindByName(string name)
        {
            // Name must be XXX.YYY{.ZZZ}
            //   where ZZZ can be any identifiers separated by dots (but we don't check this).

            lock (keyByNames)
            {
                if (keyByNames.TryGetValue(name, out ParameterKey key))
                    return key;

                // Index of first '.' between XXX and YYY
                var firstDot = name.IndexOf('.');
                if (firstDot == -1)
                    // No dot found, so it is not a valid key name
                    return null;

                // Index of '.' after YYY
                var subKeyNameIndex = name.IndexOf('.', firstDot + 1);

                string keyName = subKeyNameIndex < 0
                    ? name
                    : name[..subKeyNameIndex];  // XXX.YYY
                string subKeyName = subKeyNameIndex < 0
                    ? null
                    : name[subKeyNameIndex..];  // .ZZZ

                // TODO: subKeyName is not used, and includes the dot. Is this intended?

                // It is possible this key has been appended with mixin path
                // (i.e. Test becomes Test.mixin[0]) if it was not a "stage" value
                if (keyByNames.TryGetValue(keyName, out key) && subKeyName is not null)
                {
                    var baseParameterKeyType = key.GetType();
                    while (baseParameterKeyType.GetGenericTypeDefinition() != typeof(ParameterKey<>))
                        baseParameterKeyType = baseParameterKeyType.BaseType;

                    var baseParameterKeyTypeGenericArgument = baseParameterKeyType.GenericTypeArguments[0];

                    // Get default value and use it for the new subkey
                    var defaultValue = key.DefaultValueMetadata.GetDefaultValue();

                    // Create metadata
                    object[] metadataParameters = defaultValue is not null ? [ defaultValue ] : [];
                    var metadataType = typeof(ParameterKeyValueMetadata<>).MakeGenericType(baseParameterKeyTypeGenericArgument);

                    var metadata = Activator.CreateInstance(metadataType, metadataParameters);

                    // Create new key with the subkey name
                    object[] args = [ name, key.Length, metadata ];
                    key = (ParameterKey) Activator.CreateInstance(key.GetType(), args);

                    // Register key. Also register real name in case it was remapped
                    keyByNames[name] = key;
                    keyByNamesVersion++;
                }
                return key;
            }
        }
    }
}
