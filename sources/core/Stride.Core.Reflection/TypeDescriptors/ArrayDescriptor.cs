// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// A descriptor for an array.
    /// </summary>
    public class ArrayDescriptor : ObjectDescriptor
    {
        public ArrayDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            if (!type.IsArray) throw new ArgumentException(@"Expecting array type", nameof(type));

            if (type.GetArrayRank() != 1)
            {
                throw new ArgumentException("Cannot support dimension [{0}] for type [{1}]. Only supporting dimension of 1".ToFormat(type.GetArrayRank(), type.FullName));
            }

            ElementType = type.GetElementType();
        }

        public override DescriptorCategory Category => DescriptorCategory.Array;

        /// <summary>
        /// Gets the type of the array element.
        /// </summary>
        /// <value>The type of the element.</value>
        public Type ElementType { get; }

        /// <summary>
        /// Creates the equivalent of list type for this array.
        /// </summary>
        /// <returns>A list type with same element type than this array.</returns>
        public Array CreateArray(int dimension)
        {
            return Array.CreateInstance(ElementType, dimension);
        }

        /// <summary>
        /// Retrieves the item corresponding to the given index in the array.
        /// </summary>
        /// <param name="array">The array in which to read the item.</param>
        /// <param name="index">The index of the item to read.</param>
        /// <returns>The item corresponding to the given index in the array.</returns>
        public object GetValue(object array, int index)
        {
            return ((Array)array).GetValue(index);
        }
    }
}
