// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Provides a descriptor for a <see cref="System.Collections.IDictionary"/> and <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>.
    /// </summary>
    public abstract class DictionaryDescriptor : ObjectDescriptor
    {
        private static readonly List<string> ListOfMembersToRemove = ["Comparer", "Keys", "Values", "Capacity"];

        protected DictionaryDescriptor(ITypeDescriptorFactory factory, [NotNull] Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention) : base(factory, type, emitDefaultValues, namingConvention)
        {

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
        public abstract bool IsGenericDictionary { get; }

        /// <summary>
        /// Gets the type of the key.
        /// </summary>
        /// <value>The type of the key.</value>
        public abstract Type KeyType { get; protected init; }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        /// <value>The type of the value.</value>
        public abstract Type ValueType { get; protected init; }

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
        public abstract bool IsReadOnly(object thisObject);

        /// <summary>
        /// Gets a generic enumerator for a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>A generic enumerator.</returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        public abstract IEnumerable<KeyValuePair<object, object?>> GetEnumerator(object dictionary);

        /// <summary>
        /// Adds a a key-value to a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].ToFormat(Type)</exception>
        public abstract void SetValue(object dictionary, object key, object value);

        /// <summary>
        /// Adds a a key-value to a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].DoFormat(Type)</exception>
        public abstract void AddToDictionary(object dictionary, object key, object value);

        /// <summary>
        /// Remove a key-value from a dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public abstract void Remove(object dictionary, object key);

        /// <summary>
        /// Indicate whether the dictionary contains the given key
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public abstract bool ContainsKey(object dictionary, object key);

        /// <summary>
        /// Returns an enumerable of the keys in the dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary</param>
        public abstract ICollection GetKeys(object dictionary);

        /// <summary>
        /// Returns an enumerable of the values in the dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary</param>
        public abstract ICollection GetValues(object dictionary);

        /// <summary>
        /// Returns the value matching the given key in the dictionary, or null if the key is not found
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        public abstract object? GetValue(object dictionary, object key);

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

        public static IEnumerable<KeyValuePair<object, object?>> GetGenericEnumerable<TKey, TValue>(IDictionary<TKey, TValue?> dictionary)
        {
            return dictionary.Select(keyValue => new KeyValuePair<object, object?>(keyValue.Key, keyValue.Value));
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
