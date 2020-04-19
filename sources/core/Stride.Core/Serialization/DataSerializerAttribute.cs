// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Use this attribute on a class to specify its data serializer type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class DataSerializerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSerializerAttribute"/> class.
        /// </summary>
        /// <param name="dataSerializerType">Type of the data serializer.</param>
        public DataSerializerAttribute([NotNull] Type dataSerializerType)
        {
            DataSerializerType = dataSerializerType;
        }

        /// <summary>
        /// Gets the type of the data serializer.
        /// </summary>
        /// <value>
        /// The type of the data serializer.
        /// </value>
        [NotNull]
        public Type DataSerializerType;

        public DataSerializerGenericMode Mode;
    }
}
