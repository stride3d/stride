// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Specifies flags used when serializing reference types.
    /// </summary>
    [Flags]
    public enum SerializeClassFlags
    {
        /// <summary>
        /// Default value.
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies that the object is null.
        /// </summary>
        IsNull = 1,

        /// <summary>
        /// Specifies that additional type info is necessary and is stored in the stream.
        /// </summary>
        IsTypeInfo = 2,

        /// <summary>
        /// Specifies that the object has already been serialized previously in the stream, and is only stored as an index.
        /// </summary>
        IsReference = 4,

        /// <summary>
        /// Specifies that the object is an <see cref="IIdentifiable"/> instance has not been serialized, and only its <see cref="IIdentifiable.Id"/> has been stored.
        /// </summary>
        IsExternalIdentifiable = 8,
    }
}
