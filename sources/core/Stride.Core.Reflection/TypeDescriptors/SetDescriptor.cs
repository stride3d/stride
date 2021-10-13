// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    public class SetDescriptor : ObjectDescriptor
    {
        private static readonly List<string> ListOfMembersToRemove = new List<string> { "Comparer", "Capacity" };

        private readonly MethodInfo addMethod;
        private readonly MethodInfo removeMethod;
        private readonly MethodInfo containsMethod;
        private readonly MethodInfo countMethod;

        public SetDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            if (!IsSet(type))
                throw new ArgumentException(@"Expecting a type inheriting from System.Collections.ISet", nameof(type));

            // extract Key, Value types from ISet<??>
            var interfaceType = type.GetInterface(typeof(ISet<>));

            // Gets the element type
            ElementType = interfaceType.GetGenericArguments()[0] ?? typeof(object);

            Type[] ArgTypes = { ElementType };
            containsMethod = type.GetMethod("Contains", ArgTypes);
            addMethod = interfaceType.GetMethod("Add", ArgTypes);
            countMethod = type.GetProperty("Count").GetGetMethod();
            removeMethod = type.GetMethod("Remove", ArgTypes);
        }

        public override void Initialize(IComparer<object> keyComparer)
        {
            base.Initialize(keyComparer);

            // Only Keys and Values
            IsPureSet = Count == 0;
        }

        public override DescriptorCategory Category => DescriptorCategory.Set;

        /// <summary>
        /// Gets a value indicating whether this instance is generic dictionary.
        /// </summary>
        /// <value><c>true</c> if this instance is generic dictionary; otherwise, <c>false</c>.</value>
        public bool IsGenericSet { get; }

        /// <summary>
        /// Gets the type of the element.
        /// </summary>
        /// <value>The type of the value.</value>
        public Type ElementType { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is pure dictionary.
        /// </summary>
        /// <value><c>true</c> if this instance is pure dictionary; otherwise, <c>false</c>.</value>
        public bool IsPureSet { get; private set; }

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
        /// Adds a value to a set.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on set [{0}].ToFormat(Type)</exception>
        public void Add(object set, object item)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            var simpleSet = set as ISet<object>;
            if (simpleSet != null)
            {
                simpleSet.Add(item);
            }
            else
            {
                // Only throw an exception if the addMethod is not accessible when adding to a dictionary
                if (addMethod == null)
                {
                    throw new InvalidOperationException("No indexer this[key] method found on dictionary [{0}]".ToFormat(Type));
                }
                addMethod.Invoke(set, new[] { item });
            }
        }

        /// <summary>
        /// Remove a value from a set
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="key">The key.</param>
        public void Remove(object set, object key)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            var simpleSet = set as ISet<object>;
            if (simpleSet != null)
            {
                simpleSet.Remove(key);
            }
            else
            {
                // Only throw an exception if the addMethod is not accessible when adding to a set
                if (removeMethod == null)
                {
                    throw new InvalidOperationException("No Remove() method found on set [{0}]".ToFormat(Type));
                }
                removeMethod.Invoke(set, new[] { key });
            }

        }

        /// <summary>
        /// Indicate whether the set contains the given value
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="value">The value.</param>
        public bool Contains(object set, object value)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            var simpleSet = set as ISet<object>;
            if (simpleSet != null)
            {
                return simpleSet.Contains(value);
            }
            if (containsMethod == null)
            {
                throw new InvalidOperationException("No Contains() method found on set [{0}]".ToFormat(Type));
            }
            return (bool)containsMethod.Invoke(set, new[] { value });
        }

        /// <summary>
        /// Determines the number of elements of a count, -1 if it cannot determine the number of elements.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <returns>The number of elements of a list, -1 if it cannot determine the number of elements.</returns>
        public int GetSetCount(object set)
        {
            return set == null || countMethod == null ? -1 : (int)countMethod.Invoke(set, null);
        }

        /// <summary>
        /// Get set value by index
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="index">Index of value.</param>
        /// <returns></returns>
        public object GetValue(object set, object index)
        {
            return Contains(set, index) ? index : null;
        }

        /// <summary>
        /// Set the set value by index
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="index">Index of value.</param>
        /// <returns></returns>
        public void SetValue(object set, object index, object value)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            var simpleSet = set as ISet<object>;
            if (simpleSet != null)
            {
                if (simpleSet.Contains(index))
                {
                    simpleSet.Remove(index);
                }
                if (!simpleSet.Contains(value))
                {
                    simpleSet.Add(value);
                }
            }
            if (containsMethod != null && addMethod != null && removeMethod != null)
            {
                object[] indexParam = new[] { index };
                object[] valueParam = new[] { value };
                if ((bool)containsMethod.Invoke(set, indexParam))
                {
                    removeMethod.Invoke(set, indexParam);
                }
                if (!(bool)containsMethod.Invoke(set, valueParam))
                {
                    addMethod.Invoke(set, valueParam);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified type is a .NET set.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is set; otherwise, <c>false</c>.</returns>
        public static bool IsSet(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var typeInfo = type.GetTypeInfo();

            foreach (var iType in typeInfo.ImplementedInterfaces)
            {
                var iTypeInfo = iType.GetTypeInfo();
                if (iTypeInfo.IsGenericType && iTypeInfo.GetGenericTypeDefinition() == typeof(ISet<>))
                {
                    return true;
                }
            }

            return false;
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
