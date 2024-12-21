// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Provides a descriptor for a <see cref="System.Collections.IDictionary"/>.
    /// </summary>
    public class DictionaryDescriptor : ObjectDescriptor
    {
        private static readonly List<string> ListOfMembersToRemove = ["Comparer", "Keys", "Values", "Capacity"];

        Action<object, object, object?> addMethod;
        Action<object, object> removeMethod;
        Action<object, object, object?> setValueMethod;
        Func<object, object, bool> containsKeyMethod;
        Func<object, ICollection> getKeysMethod;
        Func<object, ICollection> getValuesMethod;
        Func<object, object, object?> getValueMethod;
        Func<object, IEnumerable<KeyValuePair<object, object?>>> getEnumeratorMethod;

        #pragma warning disable CS8618
        // This warning is disabled because the necessary initialization will occur 
        // in the CreateDictionaryDelegates<T>() method, not in the constructor.
        public DictionaryDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            if (!IsDictionary(type))
                throw new ArgumentException(@"Expecting a type inheriting from System.Collections.Generic.IDictionary<T,K>", nameof(type));

            // extract Key, Value types from IDictionary<??, ??>
            var interfaceType = type.GetInterface(typeof(IDictionary<,>))!;
            KeyType = interfaceType.GetGenericArguments()[0];
            ValueType = interfaceType.GetGenericArguments()[1];
            IsGenericDictionary = true;

            // if the type has late bound generics, no delegates can be created as the type is invalid for calling collection operations
            if (type.ContainsGenericParameters)
                return;

            var createMethod = typeof(DictionaryDescriptor).GetMethod(nameof(CreateDictionaryDelegates), BindingFlags.NonPublic | BindingFlags.Instance);
            var genericCreateMethod = createMethod!.MakeGenericMethod([KeyType, ValueType]);
            genericCreateMethod!.Invoke(this, []);
        }
        void CreateDictionaryDelegates<TKey, TValue>()
        {
            addMethod = (dictionary, key, value) => ((IDictionary<TKey, TValue?>)dictionary).Add((TKey)key, (TValue?)value);
            removeMethod = (dictionary, key) => ((IDictionary<TKey, TValue?>)dictionary).Remove((TKey)key);
            containsKeyMethod = (dictionary, key) => ((IDictionary<TKey, TValue?>)dictionary).ContainsKey((TKey)key);
            getKeysMethod = (dictionary) => (ICollection)((IDictionary<TKey, TValue?>)dictionary).Keys;
            getValuesMethod = (dictionary) => (ICollection)((IDictionary<TKey, TValue?>)dictionary).Values;
            getValueMethod = (dictionary, key) => ((IDictionary<TKey, TValue?>)dictionary)[(TKey)key];
            setValueMethod = (dictionary, key, value) => ((IDictionary<TKey, TValue?>)dictionary)[(TKey)key] = (TValue?)value;
            getEnumeratorMethod = (dictionary) => {
                return GetGenericEnumerable((IDictionary<TKey, TValue>)dictionary);
            };
        }

        public override void Initialize(IComparer<object> keyComparer)
        {
            base.Initialize(keyComparer);

            // Only Keys and Values
            IsPureDictionary = Count == 0;
        }

        public override DescriptorCategory Category => DescriptorCategory.Dictionary;

        /// <summary>
        /// Gets a value indicating whether this instance is generic dictionary.
        /// </summary>
        /// <value><c>true</c> if this instance is generic dictionary; otherwise, <c>false</c>.</value>
        public bool IsGenericDictionary { get; }

        /// <summary>
        /// Gets the type of the key.
        /// </summary>
        /// <value>The type of the key.</value>
        public Type KeyType { get; }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        /// <value>The type of the value.</value>
        public Type ValueType { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is pure dictionary.
        /// </summary>
        /// <value><c>true</c> if this instance is pure dictionary; otherwise, <c>false</c>.</value>
        public bool IsPureDictionary { get; private set; }

        /// <summary>
        /// Determines whether the specified object is read-only.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <returns><c>true</c> if [is read only] [the specified this object]; otherwise, <c>false</c>.</returns>
        public bool IsReadOnly(object thisObject)
        {
            return ((IDictionary)thisObject).IsReadOnly;
        }

        /// <summary>
        /// Gets a generic enumerator for a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>A generic enumerator.</returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        public IEnumerable<KeyValuePair<object, object?>> GetEnumerator(object dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            return getEnumeratorMethod.Invoke(dictionary);
        }

        /// <summary>
        /// Adds a a key-value to a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].ToFormat(Type)</exception>
        public void SetValue(object dictionary, object key, object? value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            setValueMethod(dictionary, key, value);
        }

        /// <summary>
        /// Adds a a key-value to a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].DoFormat(Type)</exception>
        public void AddToDictionary(object dictionary, object key, object value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            addMethod.Invoke(dictionary, key, value);
        }

        /// <summary>
        /// Remove a key-value from a dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public void Remove(object dictionary, object key)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            removeMethod.Invoke(dictionary, key);
        }

        /// <summary>
        /// Indicate whether the dictionary contains the given key
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public bool ContainsKey(object dictionary, object key)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            return containsKeyMethod.Invoke(dictionary, key);
        }

        /// <summary>
        /// Returns an enumerable of the keys in the dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary</param>
        public ICollection GetKeys(object dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            return getKeysMethod.Invoke(dictionary);
        }

        /// <summary>
        /// Returns an enumerable of the values in the dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary</param>
        public ICollection GetValues(object dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            return getValuesMethod(dictionary);
        }

        /// <summary>
        /// Returns the value matching the given key in the dictionary, or null if the key is not found
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public object? GetValue(object dictionary, object key)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            return getValueMethod.Invoke(dictionary, key);
        }

        /// <summary>
        /// Determines whether the specified type is a .NET dictionary.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is dictionary; otherwise, <c>false</c>.</returns>
        public static bool IsDictionary(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            var typeInfo = type.GetTypeInfo();

            foreach (var iType in typeInfo.ImplementedInterfaces)
            {
                var iTypeInfo = iType.GetTypeInfo();
                if (iTypeInfo.IsGenericType && iTypeInfo.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<KeyValuePair<object, object?>> GetGenericEnumerable<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            return dictionary.Select(keyValue => new KeyValuePair<object, object?>(keyValue.Key!, keyValue.Value));
        }

        protected override bool PrepareMember(MemberDescriptorBase member, MemberInfo metadataClassMemberInfo)
        {
            // Filter members
            if (member is PropertyDescriptor && ListOfMembersToRemove.Contains(member.OriginalName))
            {
                return false;
            }

            return base.PrepareMember(member, metadataClassMemberInfo);
        }
    }
}
