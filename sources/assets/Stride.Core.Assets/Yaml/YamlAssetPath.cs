// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Yaml
{
    /// <summary>
    /// A class representing the path of a member or item of an Asset as it is created/consumed by the YAML asset serializers.
    /// </summary>
    [DataContract]
    public sealed class YamlAssetPath
    {
        /// <summary>
        /// An enum representing the type of an element of the path.
        /// </summary>
        public enum ElementType
        {
            /// <summary>
            /// An element that is a member.
            /// </summary>
            Member,
            /// <summary>
            /// An element that is an index or a key.
            /// </summary>
            Index,
            /// <summary>
            /// An element that is an item identifier of a collection with ids
            /// </summary>
            /// <seealso cref="Core.Reflection.ItemId"/>
            ItemId
        }

        /// <summary>
        /// A structure representing an element of a <see cref="YamlAssetPath"/>.
        /// </summary>
        public struct Element : IEquatable<Element>
        {
            /// <summary>
            /// The type of the element.
            /// </summary>
            public readonly ElementType Type;
            /// <summary>
            /// The value of the element, corresonding to its <see cref="Type"/>.
            /// </summary>
            public readonly object Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="Element"/> structure.
            /// </summary>
            /// <param name="type">The type of element.</param>
            /// <param name="value">The value of the element.</param>
            public Element(ElementType type, object value)
            {
                Type = type;
                Value = value;
            }

            /// <summary>
            /// Fetches the name of the member, considering this element is a <see cref="ElementType.Member"/>.
            /// </summary>
            /// <returns>The name of the member.</returns>
            public string AsMember() { if (Type != ElementType.Member) throw new InvalidOperationException("This item is not a Member"); return (string)Value; }
            /// <summary>
            /// Returns the <see cref="ItemId"/> of this element, considering this element is a <see cref="ElementType.ItemId"/>.
            /// </summary>
            /// <returns>The <see cref="ItemId"/> of the item.</returns>
            public ItemId AsItemId() { if (Type != ElementType.ItemId) throw new InvalidOperationException("This item is not a item Id"); return (ItemId)Value; }

            /// <inheritdoc/>
            public bool Equals(Element other)
            {
                return Type == other.Type && Equals(Value, other.Value);
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Element && Equals((Element)obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked { return ((int)Type*397) ^ (Value?.GetHashCode() ?? 0); }
            }

            public static bool operator ==(Element left, Element right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Element left, Element right)
            {
                return !left.Equals(right);
            }
        }

        private readonly List<Element> elements = new List<Element>(16);

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlAssetPath"/> class.
        /// </summary>
        public YamlAssetPath()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlAssetPath"/> class.
        /// </summary>
        /// <param name="elements">The elements constituting this path, in proper order.</param>
        public YamlAssetPath([NotNull] IEnumerable<Element> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            this.elements.AddRange(elements);
        }

        /// <summary>
        /// The elements constituting this path.
        /// </summary>
        public IReadOnlyList<Element> Elements => elements;

        /// <summary>
        /// Indicates whether the current path represents the same path of another object.
        /// </summary>
        /// <param name="other">An object to compare with this path.</param>
        /// <returns><c>true</c> if the current path matches the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
        public bool Match(YamlAssetPath other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Elements.Count != other.Elements.Count) return false;

            return Elements.SequenceEqual(other.Elements);
        }

        /// <summary>
        /// Adds an additional element to the path representing an access to a member of an object.
        /// </summary>
        /// <param name="memberName">The name of the member.</param>
        public void PushMember(string memberName)
        {
            elements.Add(new Element(ElementType.Member, memberName));
        }

        /// <summary>
        /// Adds an additional element to the path representing an access to an item of a collection or a value of a dictionary that does not use <see cref="ItemId"/>.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <seealso cref="NonIdentifiableCollectionItemsAttribute"/>
        /// <seealso cref="ItemId"/>
        public void PushIndex(object index)
        {
            elements.Add(new Element(ElementType.Index, index));
        }

        /// <summary>
        /// Adds an additional element to the path representing an access to an item of an collection or a value of a dictionary.
        /// </summary>
        /// <param name="itemId">The <see cref="ItemId"/> of the item.</param>
        public void PushItemId(ItemId itemId)
        {
            elements.Add(new Element(ElementType.ItemId, itemId));
        }

        /// <summary>
        /// Adds an additional element.
        /// </summary>
        /// <param name="element">The <see cref="Element"/> to add.</param>
        public void Push(Element element)
        {
            elements.Add(element);
        }

        /// <summary>
        /// Appends the given <see cref="YamlAssetPath"/> to this instance.
        /// </summary>
        /// <param name="other">The <see cref="YamlAssetPath"/></param>
        /// <returns>A new instance of <see cref="YamlAssetPath"/> corresonding to the given instance appended to this instance.</returns>
        [NotNull, Pure]
        public YamlAssetPath Append([CanBeNull] YamlAssetPath other)
        {
            var result = new YamlAssetPath(elements);
            if (other != null)
            {
                result.elements.AddRange(other.elements);
            }
            return result;
        }

        /// <summary>
        /// Creates a clone of this <see cref="YamlAssetPath"/> instance.
        /// </summary>
        /// <returns>A new copy of this <see cref="YamlAssetPath"/>.</returns>
        [NotNull]
        public YamlAssetPath Clone()
        {
            var clone = new YamlAssetPath(elements);
            return clone;
        }

        /// <summary>
        /// Convert this <see cref="YamlAssetPath"/> into a <see cref="MemberPath"/>.
        /// </summary>
        /// <param name="root">The actual instance that is root of this path.</param>
        /// <returns>An instance of <see cref="MemberPath"/> corresponding to the same target than this <see cref="YamlAssetPath"/>.</returns>
        [NotNull, Pure]
        public MemberPath ToMemberPath(object root)
        {
            var currentObject = root;
            var memberPath = new MemberPath();
            foreach (var item in Elements)
            {
                if (currentObject == null)
                    throw new InvalidOperationException($"The path [{ToString()}] contains access to a member of a null object.");

                switch (item.Type)
                {
                    case ElementType.Member:
                    {
                        var typeDescriptor = TypeDescriptorFactory.Default.Find(currentObject.GetType());
                        var name = item.AsMember();
                        var memberDescriptor = typeDescriptor.Members.FirstOrDefault(x => x.Name == name);
                        if (memberDescriptor == null) throw new InvalidOperationException($"The path [{ToString()}] contains access to non-existing member [{name}].");
                        memberPath.Push(memberDescriptor);
                        currentObject = memberDescriptor.Get(currentObject);
                        break;
                    }
                    case ElementType.Index:
                    {
                        var typeDescriptor = TypeDescriptorFactory.Default.Find(currentObject.GetType());
                        if (typeDescriptor is ArrayDescriptor arrayDescriptor)
                        {
                            if (!(item.Value is int)) throw new InvalidOperationException($"The path [{ToString()}] contains non-integer index on an array.");
                            memberPath.Push(arrayDescriptor, (int)item.Value);
                            currentObject = arrayDescriptor.GetValue(currentObject, (int)item.Value);
                        }
                        if (typeDescriptor is CollectionDescriptor collectionDescriptor)
                        {
                            if (!(item.Value is int)) throw new InvalidOperationException($"The path [{ToString()}] contains non-integer index on a collection.");
                            memberPath.Push(collectionDescriptor, (int)item.Value);
                            currentObject = collectionDescriptor.GetValue(currentObject, (int)item.Value);
                        }
                        if (typeDescriptor is DictionaryDescriptor dictionaryDescriptor)
                        {
                            if (item.Value == null) throw new InvalidOperationException($"The path [{ToString()}] contains a null key on an dictionary.");
                            memberPath.Push(dictionaryDescriptor, item.Value);
                            currentObject = dictionaryDescriptor.GetValue(currentObject, item.Value);
                        }
                        break;
                    }
                    case ElementType.ItemId:
                    {
                        var ids = CollectionItemIdHelper.GetCollectionItemIds(currentObject);
                        var key = ids.GetKey(item.AsItemId());
                        var typeDescriptor = TypeDescriptorFactory.Default.Find(currentObject.GetType());
                        if (typeDescriptor is ArrayDescriptor arrayDescriptor)
                        {
                            if (!(key is int)) throw new InvalidOperationException($"The path [{ToString()}] contains a non-valid item id on an array.");
                            memberPath.Push(arrayDescriptor, (int)key);
                            currentObject = arrayDescriptor.GetValue(currentObject, (int)key);
                        }
                        if (typeDescriptor is CollectionDescriptor collectionDescriptor)
                        {
                            if (!(key is int)) throw new InvalidOperationException($"The path [{ToString()}] contains a non-valid item id on a collection.");
                            memberPath.Push(collectionDescriptor, (int)key);
                            currentObject = collectionDescriptor.GetValue(currentObject, (int)key);
                        }
                        if (typeDescriptor is DictionaryDescriptor dictionaryDescriptor)
                        {
                            if (key == null) throw new InvalidOperationException($"The path [{ToString()}] contains a non-valid item id on an dictionary.");
                            memberPath.Push(dictionaryDescriptor, key);
                            currentObject = dictionaryDescriptor.GetValue(currentObject, key);
                        }
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return memberPath;
        }

        /// <summary>
        /// Creates a <see cref="YamlAssetPath"/> out of a <see cref="MemberPath"/> instance.
        /// </summary>
        /// <param name="path">The <see cref="MemberPath"/> from which to create a <see cref="YamlAssetPath"/>.</param>
        /// <param name="root">The root object of the given <see cref="MemberPath"/>.</param>
        /// <returns>An instance of <see cref="YamlAssetPath"/> corresponding to the same target than the given <see cref="MemberPath"/>.</returns>
        [NotNull]
        public static YamlAssetPath FromMemberPath([NotNull] MemberPath path, object root)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var result = new YamlAssetPath();
            var clone = new MemberPath();
            foreach (var item in path.Decompose())
            {
                if (item.MemberDescriptor != null)
                {
                    clone.Push(item.MemberDescriptor);
                    var member = item.MemberDescriptor.Name;
                    result.PushMember(member);
                }
                else
                {
                    object index = null;
                    if (item is MemberPath.ArrayPathItem arrayItem)
                    {
                        clone.Push(arrayItem.Descriptor, arrayItem.Index);
                        index = arrayItem.Index;
                    }
                    if (item is MemberPath.CollectionPathItem collectionItem)
                    {
                        clone.Push(collectionItem.Descriptor, collectionItem.Index);
                        index = collectionItem.Index;
                    }
                    if (item is MemberPath.DictionaryPathItem dictionaryItem)
                    {
                        clone.Push(dictionaryItem.Descriptor, dictionaryItem.Key);
                        index = dictionaryItem.Key;
                    }
                    if (!CollectionItemIdHelper.TryGetCollectionItemIds(clone.GetValue(root), out CollectionItemIdentifiers ids))
                    {
                        result.PushIndex(index);
                    }
                    else
                    {
                        var id = ids[index];
                        // Create a new id if we don't have any so far
                        if (id == ItemId.Empty)
                            id = ItemId.New();
                        result.PushItemId(id);
                    }
                }
            }
            return result;
        }

        public bool StartsWith([NotNull] YamlAssetPath path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.elements.Count > elements.Count)
                return false;

            for (var i = 0; i < path.Elements.Count; ++i)
            {
                if (!Elements[i].Equals(path.Elements[i]))
                    return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(object)");
            foreach (var item in elements)
            {
                switch (item.Type)
                {
                    case ElementType.Member:
                        sb.Append('.');
                        sb.Append(item.Value);
                        break;
                    case ElementType.Index:
                        sb.Append('[');
                        sb.Append(item.Value);
                        sb.Append(']');
                        break;
                    case ElementType.ItemId:
                        sb.Append('{');
                        sb.Append(item.Value);
                        sb.Append('}');
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return sb.ToString();
        }

        internal static bool IsCollectionWithIdType([NotNull] Type type, object key, out ItemId id, out object actualKey)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(CollectionWithItemIds<>))
                {
                    id = (ItemId)key;
                    actualKey = key;
                    return true;
                }
                if (type.GetGenericTypeDefinition() == typeof(DictionaryWithItemIds<,>))
                {
                    var keyWithId = (IKeyWithId)key;
                    id = keyWithId.Id;
                    actualKey = keyWithId.Key;
                    return true;
                }
            }

            id = ItemId.Empty;
            actualKey = key;
            return false;
        }

        internal static bool IsCollectionWithIdType([NotNull] Type type, object key, out ItemId id)
        {
            return IsCollectionWithIdType(type, key, out id, out object _);
        }
    }
}
