// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Rendering
{
    /// <summary>
    ///   A parameter key that identifies a <em>blittable</em> or <em><see langword="unmanaged"/></em> value,
    ///   usually for use by Shaders (<c>.sdsl</c>).
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <remarks>
    ///   An <see cref="ValueParameterKey{T}"/> can be used to represent values like, for example,
    ///   vectors, matrices, <see langword="int"/>s, <see langword="float"/>s, etc.
    /// </remarks>
    [DataSerializer(typeof(ValueParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class ValueParameterKey<T> : ParameterKey<T> where T : struct
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="ValueParameterKey{T}"/> class.
        /// </summary>
        /// <param name="name">The name with which to identify the parameter key.</param>
        /// <param name="length">The number of elements the parameter is composed of.</param>
        /// <param name="metadata">
        ///   Optional metadata object providing additional information about the parameter or its type.
        /// </param>
        public ValueParameterKey(string name, int length = 1, PropertyKeyMetadata? metadata = null)
            : base(ParameterKeyType.Value, name, length, metadata)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValueParameterKey{T}"/> class.
        /// </summary>
        /// <param name="name">The name with which to identify the parameter key.</param>
        /// <param name="length">The number of elements the parameter is composed of.</param>
        /// <param name="metadatas">
        ///   Optional metadata objects providing additional information about the parameter or its type.
        /// </param>
        public ValueParameterKey(string name, int length = 1, params PropertyKeyMetadata[]? metadatas)
            : base(ParameterKeyType.Value, name, length, metadatas)
        {
        }


        /// <inheritdoc/>
        internal override unsafe object ReadValue(nint data)
            => Unsafe.ReadUnaligned<T>((void*) data);

        /// <inheritdoc/>
        internal override object ReadValue(scoped ref readonly byte data)
            => Unsafe.ReadUnaligned<T>(in data);

        /// <inheritdoc/>
        internal override object ReadValue(scoped ReadOnlySpan<byte> data)
            => Unsafe.ReadUnaligned<T>(in data[0]);
    }
}
