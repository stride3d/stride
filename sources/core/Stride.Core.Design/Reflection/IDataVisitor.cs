// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using Stride.Core.Annotations;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Interface for visiting serializable data (binary, yaml and editor).
    /// </summary>
    public interface IDataVisitor
    {
        /// <summary>
        /// Visits a null.
        /// </summary>
        void VisitNull();

        /// <summary>
        /// Visits a primitive (int, float, string...etc.)
        /// </summary>
        /// <param name="primitive">The primitive.</param>
        /// <param name="descriptor">The descriptor.</param>
        void VisitPrimitive([NotNull] object primitive, [NotNull] PrimitiveDescriptor descriptor);

        /// <summary>
        /// Visits an object (either a class or a struct)
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="visitMembers"></param>
        void VisitObject([NotNull] object obj, [NotNull] ObjectDescriptor descriptor, bool visitMembers);

        /// <summary>
        /// Visits an object member.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="containerDescriptor">The container descriptor.</param>
        /// <param name="member">The member.</param>
        /// <param name="value">The value.</param>
        void VisitObjectMember([NotNull] object container, [NotNull] ObjectDescriptor containerDescriptor, [NotNull] IMemberDescriptor member, object value);

        /// <summary>
        /// Visits an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="descriptor">The descriptor.</param>
        void VisitArray([NotNull] Array array, [NotNull] ArrayDescriptor descriptor);

        /// <summary>
        /// Visits an array item.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="itemDescriptor">The item descriptor.</param>
        void VisitArrayItem(Array array, [NotNull] ArrayDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor);

        /// <summary>
        /// Visits a collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="descriptor">The descriptor.</param>
        void VisitCollection([NotNull] IEnumerable collection, [NotNull] CollectionDescriptor descriptor);

        /// <summary>
        /// Visits a collection item.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="itemDescriptor">The item descriptor.</param>
        void VisitCollectionItem([NotNull] IEnumerable collection, [NotNull] CollectionDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor);

        /// <summary>
        /// Visits a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="descriptor">The descriptor.</param>
        void VisitDictionary([NotNull] object dictionary, [NotNull] DictionaryDescriptor descriptor);

        /// <summary>
        /// Visits a dictionary key-value.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="key">The key.</param>
        /// <param name="keyDescriptor">The key descriptor.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueDescriptor">The value descriptor.</param>
        void VisitDictionaryKeyValue([NotNull] object dictionary, [NotNull] DictionaryDescriptor descriptor, object key, ITypeDescriptor keyDescriptor, object value, ITypeDescriptor valueDescriptor);
    }
}
