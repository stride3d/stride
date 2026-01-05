// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Stride.Rendering
{
    /// <summary>
    ///   Provides methods for creating, managing, and retrieving parameter keys.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <see cref="ParameterKeys"/> class offers a variety of static methods to create different
    ///     types of parameter keys, such as permutation, value, and object keys.
    ///     It also provides the ability to create composed keys with names and indices, indexed keys,
    ///     and finding keys by name.
    ///   </para>
    ///   <para>
    ///     The class also serves like an internal registry for all parameter keys, allowing it
    ///     to manage the keys and ensure their uniqueness and integrity.
    ///   </para>
    /// </remarks>
    public static partial class ParameterKeys
    {
        private static long keysVersion = 0;
        private static long keyByNamesVersion = 0;

        private static readonly List<ParameterKey> keys = [];  // All the parameter keys

        private static readonly Dictionary<string, ParameterKey> keyByNames = [];
        private static readonly Dictionary<ParameterComposedKey, ParameterKey> composedKeys = [];


        /// <summary>
        ///   Gets all the parameter keys.
        /// </summary>
        /// <returns>
        ///   A collection of all the <see cref="ParameterKey"/>s that are not composed keys.
        /// </returns>
        /// <remarks>
        ///   This method returns a snapshot of the keys at the time of the call. If no keys are added or removed,
        ///   the returned collection will remain the same across multiple calls. However, if keys are added or removed,
        ///   the parameter keys will be re-evaluated and the collection will be updated accordingly.
        /// </remarks>
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


        /// <summary>
        ///   Creates a new permutation parameter key.
        /// </summary>
        /// <typeparam name="T">The type of the permutation parameter.</typeparam>
        /// <param name="defaultValue">
        ///   An optional default value for the permutation parameter.
        ///   If <typeparamref name="T"/> is an array, its length is used to determine the key's <see cref="ParameterKey.Length"/>.
        /// </param>
        /// <param name="name">
        ///   An optional name for the permutation parameter key. If <see langword="null"/>,
        ///   an empty string is used.
        /// </param>
        /// <returns>A new <see cref="PermutationParameterKey{T}"/>.</returns>
        public static PermutationParameterKey<T> NewPermutation<T>(T defaultValue = default, string? name = null)
        {
            name ??= string.Empty;

            var length = typeof(T).IsArray
                ? defaultValue is not null ? ((Array)(object) defaultValue).Length : -1
                : 1;

            return new PermutationParameterKey<T>(name, length, new ParameterKeyValueMetadata<T>(defaultValue));
        }

        /// <summary>
        ///   Creates a new value parameter key.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the permutation parameter.
        ///   Must be a <em>blittable</em> type (i.e. <see langword="unmanaged"/>).
        /// </typeparam>
        /// <param name="defaultValue">
        ///   An optional default value for the permutation parameter.
        ///   If not specified, the default value for <typeparamref name="T"/> is used.
        /// </param>
        /// <param name="name">
        ///   An optional name for the permutation parameter key.
        ///   If <see langword="null"/>, an empty string is used.
        /// </param>
        /// <returns>A new <see cref="ValueParameterKey{T}"/>.</returns>
        public static ValueParameterKey<T> NewValue<T>(T defaultValue = default, string? name = null) where T : unmanaged
        {
            name ??= string.Empty;

            return new ValueParameterKey<T>(name, 1, new ParameterKeyValueMetadata<T>(defaultValue));
        }

        /// <summary>
        ///   Creates a new object parameter key.
        /// </summary>
        /// <typeparam name="T">The type of the object parameter.</typeparam>
        /// <param name="defaultValue">
        ///   An optional default value for the object parameter.
        ///   If <typeparamref name="T"/> is an array, its length is used to determine the key's <see cref="ParameterKey.Length"/>.
        /// </param>
        /// <param name="name">
        ///   An optional name for the object parameter key. If <see langword="null"/>,
        ///   an empty string is used.
        /// </param>
        /// <returns>A new <see cref="ObjectParameterKey{T}"/>.</returns>
        public static ObjectParameterKey<T> NewObject<T>(T defaultValue = default, string? name = null)
        {
            name ??= string.Empty;

            var length = typeof(T).IsArray
                ? defaultValue is not null ? ((Array)(object) defaultValue).Length : -1
                : 1;

            return new ObjectParameterKey<T>(name, length, new ParameterKeyValueMetadata<T>(defaultValue));
        }


        /// <summary>
        ///   Creates a key equal to another parameter key with an added index.
        /// </summary>
        /// <param name="index">
        ///   The index.
        ///   If <c>0</c>, the original key is returned.
        /// </param>
        /// <returns>
        ///   A new key with the same name but the <paramref name="index"/> appended to its name.
        /// </returns>
        public static T NewIndexedKey<T>(T key, int index) where T : ParameterKey
        {
            if (index == 0)
                return key;

            var keyName = key.Name[^1] == '0'
                ? key.Name[..^1] + index
                : key.Name + index;

            return (T) Activator.CreateInstance(typeof(T), keyName, key.Length, key.Metadatas);
        }


        /// <summary>
        ///   Registers a parameter key with the provided name and owner type, or
        ///   updates it if it already exists.
        /// </summary>
        /// <param name="key">The <see cref="ParameterKey"/> to be merged.</param>
        /// <param name="ownerType">
        ///   The type that owns the parameter key.
        ///   If <see langword="null"/>, the key will not be assigned an owner type,
        ///   so it will keep its current owner type.
        /// </param>
        /// <param name="name">
        ///   The name to associate with the parameter key.
        ///   Must not be <see langword="null"/> or empty.
        /// </param>
        /// <returns>The registered or updated <see cref="ParameterKey"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/>, or an empty string, or contains only whitespace.
        /// </exception>
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
        ///   Creates a composed parameter key from a parameter key and a name.
        /// </summary>
        /// <typeparam name="TKey">Type of the key value</typeparam>
        /// <param name="parameterKey">The parameter key to compose.</param>
        /// <param name="name">The name to compose the parameter key with.</param>
        /// <returns>The composition of <paramref name="parameterKey"/> and <paramref name="name"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameterKey"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <remarks>
        ///   This method creates a new key that is a composition of the provided key and name.
        ///   For example, if the key is <c>"MyKey.MyKeyName"</c> and the name is <c>"MyName"</c>, the result
        ///   will be <c>"MyKey.MyKeyName.MyName"</c>.
        /// </remarks>
        public static TKey ComposeWith<TKey>(this TKey parameterKey, string name) where TKey : ParameterKey
        {
            ArgumentNullException.ThrowIfNull(parameterKey);
            ArgumentNullException.ThrowIfNull(name);

            var composedKey = new ParameterComposedKey(parameterKey, name, indexer: -1);
            return (TKey) GetOrCreateComposedKey(ref composedKey);
        }

        /// <summary>
        ///   Creates a composed parameter key from a parameter key, a name, and an index.
        /// </summary>
        /// <typeparam name="TKey">Type of the key value</typeparam>
        /// <param name="parameterKey">The parameter key to compose.</param>
        /// <param name="name">The name to compose the parameter key with.</param>
        /// <param name="index">The index to append to the composed key.</param>
        /// <returns>
        ///   The composition of <paramref name="parameterKey"/>, <paramref name="name"/>, and <paramref name="index"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameterKey"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">index;Must be >= 0</exception>
        /// <remarks>
        ///   This method creates a new key that is a composition of the provided key, the name, and the index.
        ///   For example, if the key is <c>"MyKey.MyKeyName"</c>, the name is <c>"MyName"</c>, and the index is <c>5</c>,
        ///   the result will be <c>"MyKey.MyKeyName.MyName[5]"</c>.
        /// </remarks>
        public static TKey ComposeIndexer<TKey>(this TKey parameterKey, string name, int index) where TKey : ParameterKey
        {
            ArgumentNullException.ThrowIfNull(parameterKey);
            ArgumentNullException.ThrowIfNull(name);

            var composedKey = new ParameterComposedKey(parameterKey, name, index);
            return (TKey) GetOrCreateComposedKey(in composedKey);
        }

        /// <summary>
        ///   Retrieves an existing composed parameter key or creates a new one if it does not exist.
        /// </summary>
        /// <param name="composedKey">
        ///   A composed key structure that includes the base key and additional composition details.
        /// </param>
        /// <returns>The <see cref="ParameterKey"/> associated with the specified composed key.</returns>
        /// <exception cref="ArgumentException">
        ///   Thrown if the composed key cannot be found or created.
        /// </exception>
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

        /// <summary>
        ///   Attempts to find a parameter key by its name.
        /// </summary>
        /// <param name="name">The name of the parameter key to search for.</param>
        /// <returns>
        ///   The <see cref="ParameterKey"/> associated with the specified name,
        ///   or <see langword="null"/> if no such key exists.
        /// </returns>
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

        /// <summary>
        ///   Finds and returns a parameter key based on the specified name.
        /// </summary>
        /// <param name="name">
        ///   The name of the parameter key to find.
        ///   The name must be in the format <c>"XXX.YYY{.ZZZ}"</c>, where <c>"ZZZ"</c> can be
        ///   any number of identifiers separated by dots.
        /// </param>
        /// <returns>
        ///   A <see cref="ParameterKey"/> corresponding to the specified name, or <see langword="null"/>
        ///   if the name is not valid or the key cannot be found.
        /// </returns>
        /// <remarks>
        ///   If the key is not found, it attempts to create a new key using the base key's default value metadata.
        /// </remarks>
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
