// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Describes a descriptor for a nullable type <see cref="Nullable{T}"/>.
    /// </summary>
    public class NullableDescriptor : ObjectDescriptor
    {
        private static readonly List<IMemberDescriptor> EmptyMembers = new List<IMemberDescriptor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDescriptor" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException">Type [{0}] is not a primitive</exception>
        public NullableDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            if (!IsNullable(type))
                throw new ArgumentException("Type [{0}] is not a primitive");

            UnderlyingType = Nullable.GetUnderlyingType(type);
        }

        public override DescriptorCategory Category => DescriptorCategory.Nullable;

        /// <summary>
        /// Gets the type underlying type T of the nullable <see cref="Nullable{T}"/>
        /// </summary>
        /// <value>The type of the element.</value>
        public Type UnderlyingType { get; private set; }

        /// <summary>
        /// Determines whether the specified type is nullable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is nullable; otherwise, <c>false</c>.</returns>
        public static bool IsNullable(Type type)
        {
            return type.IsNullable();
        }

        protected override List<IMemberDescriptor> PrepareMembers()
        {
            return EmptyMembers;
        }
    }
}
