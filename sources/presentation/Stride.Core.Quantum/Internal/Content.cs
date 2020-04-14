// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A helper class containing methods to manipulate contents.
    /// </summary>
    internal static class Content
    {
        /// <summary>
        /// Retrieves the value itself or the value of one of its item, depending on the given <see cref="NodeIndex"/>.
        /// </summary>
        /// <param name="value">The value on which this method applies.</param>
        /// <param name="index">The index of the item to retrieve. If <see cref="NodeIndex.Empty"/> is passed, this method will return the value itself.</param>
        /// <param name="descriptor">The descriptor of the type of <paramref name="value"/>.</param>
        /// <returns>The value itself or the value of one of its item.</returns>
        public static object Retrieve(object value, NodeIndex index, ITypeDescriptor descriptor)
        {
            if (index.IsEmpty)
                return value;

            if (value == null) throw new ArgumentNullException(nameof(value));

            var collectionDescriptor = descriptor as CollectionDescriptor;
            if (collectionDescriptor != null)
            {
                return collectionDescriptor.GetValue(value, index.Int);
            }
            var dictionaryDescriptor = descriptor as DictionaryDescriptor;
            if (dictionaryDescriptor != null)
            {
                return dictionaryDescriptor.GetValue(value, index.Value);
            }

            // Try with the concrete type descriptor
            var objectDescriptor = TypeDescriptorFactory.Default.Find(value.GetType());
            if (objectDescriptor != descriptor)
            {
                return Retrieve(value, index, objectDescriptor);
            }

            throw new NotSupportedException("Unable to retrieve the value at the given index, this collection is unsupported");
        }
    }
}
