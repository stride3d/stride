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
    public class SetDescriptor : CollectionDescriptor
    {
        private static readonly List<string> ListOfMembersToRemove = new List<string> { "Comparer", "Capacity" };

        private readonly MethodInfo addMethod;
        private readonly MethodInfo removeMethod;
        private readonly MethodInfo clearMethod;
        private readonly MethodInfo containsMethod;
        private readonly MethodInfo countMethod;
        private readonly MethodInfo isReadOnlyMethod;

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
            addMethod = interfaceType.GetMethod("Add", ArgTypes);
            removeMethod = type.GetMethod("Remove", ArgTypes);
            clearMethod = interfaceType.GetMethod("Clear");
            containsMethod = type.GetMethod("Contains", ArgTypes);
            countMethod = type.GetProperty("Count").GetGetMethod();
            isReadOnlyMethod = type.GetInterface(typeof(ICollection<>)).GetProperty("IsReadOnly").GetGetMethod();

            HasAdd = true;
            HasRemove = true;
            HasIndexerAccessors = true;
            HasInsert = false;
            HasRemoveAt = false;
        }

        public override void Initialize(IComparer<object> keyComparer)
        {
            base.Initialize(keyComparer);

            // Only Keys and Values
            IsPureCollection = Count == 0;
        }

        public override DescriptorCategory Category => DescriptorCategory.Set;

        /// <summary>
        /// Gets a value indicating whether this instance is generic set.
        /// </summary>
        /// <value><c>true</c> if this instance is generic set; otherwise, <c>false</c>.</value>
        public bool IsGenericSet { get; }

        /// <summary>
        /// Determines whether the value passed is readonly.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <returns><c>true</c> if [is read only] [the specified this object]; otherwise, <c>false</c>.</returns>
        public override bool IsReadOnly(object thisObject)
        {
            return thisObject == null || isReadOnlyMethod == null || (bool)isReadOnlyMethod.Invoke(thisObject, null);
        }

        /// <summary>
        /// Adds a value to a set.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on set [{0}].ToFormat(Type)</exception>
        public override void Add(object set, object item)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            var simpleSet = set as ISet<object>;
            if (simpleSet != null)
            {
                simpleSet.Add(item);
            }
            else
            {
                // Only throw an exception if the addMethod is not accessible when adding to a set
                if (addMethod == null)
                {
                    throw new InvalidOperationException("No Add() method found on set [{0}]".ToFormat(Type));
                }
                addMethod.Invoke(set, new[] { item });
            }
        }

        public override void Insert(object set, int index, object value)
        {
            throw new InvalidOperationException("SetDescriptor should not call function 'Insert'.");
        }

        /// <summary>
        /// Remove a value from a set
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="key">The key.</param>
        public override void Remove(object set, object key)
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

        public override void RemoveAt(object set, int index)
        {
            throw new InvalidOperationException("SetDescriptor should not call function 'RemoveAt'.");
        }

        /// <summary>
        /// Clears the specified set.
        /// </summary>
        /// <param name="set">The set.</param>
        public override void Clear(object set)
        {
            clearMethod.Invoke(set, null);
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
        /// <returns>The number of elements of a set, -1 if it cannot determine the number of elements.</returns>
        public override int GetCollectionCount(object set)
        {
            return set == null || countMethod == null ? -1 : (int)countMethod.Invoke(set, null);
        }

        /// <summary>
        /// Get set value by index
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="index">Index of value.</param>
        /// <returns></returns>
        public override object GetValue(object set, object index)
        {
            return Contains(set, index) ? index : null;
        }

        /// <summary>
        /// Returns the value matching the given index in the set.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="index">The index.</param>
        public override object GetValue(object set, int index)
        {
            throw new InvalidOperationException("SetDescriptor should not call function 'GetValue' with int index parameter.");
        }

        /// <summary>
        /// Set the set value by index
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="index">Index of value.</param>
        /// <returns></returns>
        public override void SetValue(object set, object index, object value)
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
            else if (containsMethod != null && addMethod != null && removeMethod != null)
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
            {
                return false;
            }

            return base.PrepareMember(member, metadataClassMemberInfo);
        }
    }
}
