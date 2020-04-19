// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        private static readonly List<string> ListOfMembersToRemove = new List<string> {"Comparer", "Keys", "Values", "Capacity" };

        private readonly MethodInfo getEnumeratorGeneric;
        private readonly PropertyInfo getKeysMethod;
        private readonly PropertyInfo getValuesMethod;
        private readonly PropertyInfo indexerProperty;
        private readonly MethodInfo indexerSetter;
        private readonly MethodInfo removeMethod;
        private readonly MethodInfo containsKeyMethod;
        private readonly MethodInfo addMethod;

        public DictionaryDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            if (!IsDictionary(type))
                throw new ArgumentException(@"Expecting a type inheriting from System.Collections.IDictionary", nameof(type));

            // extract Key, Value types from IDictionary<??, ??>
            var interfaceType = type.GetInterface(typeof(IDictionary<,>));
            if (interfaceType != null)
            {
                KeyType = interfaceType.GetGenericArguments()[0];
                ValueType = interfaceType.GetGenericArguments()[1];
                IsGenericDictionary = true;
                getEnumeratorGeneric = typeof(DictionaryDescriptor).GetMethod("GetGenericEnumerable").MakeGenericMethod(KeyType, ValueType);
                containsKeyMethod = interfaceType.GetMethod("ContainsKey", new[] { KeyType });
                // Retrieve the other properties and methods from the interface
                type = interfaceType;
            }
            else
            {
                KeyType = typeof(object);
                ValueType = typeof(object);
                containsKeyMethod = type.GetMethod("Contains", new[] { KeyType });
            }

            addMethod = type.GetMethod("Add", new[] { KeyType, ValueType });
            getKeysMethod = type.GetProperty("Keys");
            getValuesMethod = type.GetProperty("Values");
            indexerProperty = type.GetProperty("Item", ValueType, new[] { KeyType });
            indexerSetter = indexerProperty.SetMethod;
            removeMethod = type.GetMethod("Remove", new[] { KeyType });
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
        /// Determines whether the value passed is readonly.
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
        public IEnumerable<KeyValuePair<object, object>> GetEnumerator(object dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (IsGenericDictionary)
            {
                foreach (var item in (IEnumerable<KeyValuePair<object, object>>)getEnumeratorGeneric.Invoke(null, new[] {dictionary}))
                {
                    yield return item;
                }
            }
            else
            {
                var simpleDictionary = (IDictionary)dictionary;
                foreach (var keyValueObject in simpleDictionary)
                {
                    if (!(keyValueObject is DictionaryEntry))
                    {
                        throw new NotSupportedException($"Key value-pair type [{keyValueObject}] is not supported for IDictionary. Only DictionaryEntry");
                    }
                    var entry = (DictionaryEntry)keyValueObject;
                    yield return new KeyValuePair<object, object>(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Adds a a key-value to a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].ToFormat(Type)</exception>
        public void SetValue(object dictionary, object key, object value)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            var simpleDictionary = dictionary as IDictionary;
            if (simpleDictionary != null)
            {
                simpleDictionary[key] = value;
            }
            else
            {
                // Only throw an exception if the addMethod is not accessible when adding to a dictionary
                if (indexerSetter == null)
                {
                    throw new InvalidOperationException("No indexer this[key] method found on dictionary [{0}]".ToFormat(Type));
                }
                indexerSetter.Invoke(dictionary, new[] { key, value });
            }
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
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            var simpleDictionary = dictionary as IDictionary;
            if (simpleDictionary != null)
            {
                simpleDictionary.Add(key, value);
            }
            else
            {
                // Only throw an exception if the addMethod is not accessible when adding to a dictionary
                if (addMethod == null)
                {
                    throw new InvalidOperationException($"No Add() method found on dictionary [{Type}]");
                }
                addMethod.Invoke(dictionary, new[] { key, value });
            }
        }

        /// <summary>
        /// Remove a key-value from a dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public void Remove(object dictionary, object key)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            var simpleDictionary = dictionary as IDictionary;
            if (simpleDictionary != null)
            {
                simpleDictionary.Remove(key);
            }
            else
            {
                // Only throw an exception if the addMethod is not accessible when adding to a dictionary
                if (removeMethod == null)
                {
                    throw new InvalidOperationException("No Remove() method found on dictionary [{0}]".ToFormat(Type));
                }
                removeMethod.Invoke(dictionary, new[] { key });
            }

        }

        /// <summary>
        /// Indicate whether the dictionary contains the given key
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public bool ContainsKey(object dictionary, object key)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            var simpleDictionary = dictionary as IDictionary;
            if (simpleDictionary != null)
            {
                return simpleDictionary.Contains(key);
            }
            if (containsKeyMethod == null)
            {
                throw new InvalidOperationException("No ContainsKey() method found on dictionary [{0}]".ToFormat(Type));
            }
            return (bool)containsKeyMethod.Invoke(dictionary, new[] { key });
        }

        /// <summary>
        /// Returns an enumerable of the keys in the dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary</param>
        public ICollection GetKeys(object dictionary)
        {
            return (ICollection)getKeysMethod.GetValue(dictionary);
        }

        /// <summary>
        /// Returns an enumerable of the values in the dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary</param>
        public ICollection GetValues(object dictionary)
        {
            return (ICollection)getValuesMethod.GetValue(dictionary);
        }

        /// <summary>
        /// Returns the value matching the given key in the dictionary, or null if the key is not found
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public object GetValue(object dictionary, object key)
        {
            var fastDictionary = dictionary as IDictionary;
            if (fastDictionary != null)
            {
                return fastDictionary[key];
            }

            return indexerProperty.GetValue(dictionary,new [] { key });
        }

        /// <summary>
        /// Determines whether the specified type is a .NET dictionary.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is dictionary; otherwise, <c>false</c>.</returns>
        public static bool IsDictionary(Type type)
        {
            return TypeHelper.IsDictionary(type);
        }

        public static IEnumerable<KeyValuePair<object, object>> GetGenericEnumerable<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            return dictionary.Select(keyValue => new KeyValuePair<object, object>(keyValue.Key, keyValue.Value));
        }

        protected override bool PrepareMember(MemberDescriptorBase member, MemberInfo metadataClassMemberInfo)
        {
            // Filter members
            if (member is PropertyDescriptor && ListOfMembersToRemove.Contains(member.OriginalName))
            //if (member is PropertyDescriptor && (member.DeclaringType.Namespace ?? string.Empty).StartsWith(SystemCollectionsNamespace) && ListOfMembersToRemove.Contains(member.Name))
            {
                return false;
            }

            return base.PrepareMember(member, metadataClassMemberInfo);
        }
    }
}
