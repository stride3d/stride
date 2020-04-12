// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Describes a descriptor for an unsupported object type.
    /// This will be treated as an <see cref="ObjectDescriptor"/>
    /// </summary>
    public class NotSupportedObjectDescriptor : ObjectDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotSupportedObjectDescriptor" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException">Type [{0}] is not a primitive</exception>
        public NotSupportedObjectDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
        }

        public override DescriptorCategory Category => DescriptorCategory.NotSupportedObject;
    }
}
