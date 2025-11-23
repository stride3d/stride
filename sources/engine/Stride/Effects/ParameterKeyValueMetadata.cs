// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Runtime.CompilerServices;

using Stride.Core;
using Stride.Core.UnsafeExtensions;

namespace Stride.Rendering
{
    /// <summary>
    ///   Specifies metadata for a <see cref="PropertyKey"/>.
    /// </summary>
    /// <remarks>
    ///   This class is used to provide additional information about a property key.
    ///   Derived classes can implement specific metadata types, such as a default value (<see cref="DefaultValueMetadata"/>),
    ///   description, or other attributes.
    /// </remarks>
    public abstract class ParameterKeyValueMetadata : PropertyKeyMetadata
    {
        /// <summary>
        ///   Retrieves the default value for associated parameter key.
        /// </summary>
        /// <returns>An <see cref="object"/> representing the default value of the type.</returns>
        public abstract object GetDefaultValue();

        /// <summary>
        ///   Writes the default value of the parameter identified by the parameter key
        ///   to the specified data pointer.
        /// </summary>
        /// <param name="destination">A pointer to the data location where the value should be written.</param>
        /// <param name="alignment">The memory alignment of the data.</param>
        /// <returns>
        ///   A value indicating whether the write operation was successful.
        /// </returns>
        public abstract bool WriteValue(IntPtr destination, int alignment = 1);

        /// <summary>
        ///   Writes the default value of the parameter identified by the parameter key
        ///   to the specified data reference.
        /// </summary>
        /// <param name="destination">A reference to the data location where the value should be written.</param>
        /// <param name="alignment">The memory alignment of the data.</param>
        /// <returns>
        ///   A value indicating whether the write operation was successful.
        /// </returns>
        public abstract bool WriteValue(scoped ref byte destination, int alignment = 1);

        /// <summary>
        ///   Writes the default value of the parameter identified by the parameter key
        ///   to the specified span.
        /// </summary>
        /// <param name="destination">A writtable span where the value should be written.</param>
        /// <param name="alignment">The memory alignment of the data.</param>
        /// <returns>
        ///   A value indicating whether the write operation was successful.
        /// </returns>
        public abstract bool WriteValue(scoped Span<byte> destination, int alignment = 1);
    }


    /// <inheritdoc cref="ParameterKeyValueMetadata"/>
    public class ParameterKeyValueMetadata<T> : ParameterKeyValueMetadata
    {
        /// <summary>
        ///   Gets the default value for the parameter key.
        /// </summary>
        public T DefaultValue { get; internal set; }
        /// <inheritdoc/>
        public override object GetDefaultValue() => DefaultValue;


        /// <summary>
        ///   Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        /// </summary>
        public ParameterKeyValueMetadata() { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value of the parameter key.</param>
        public ParameterKeyValueMetadata(T defaultValue)
        {
            DefaultValue = defaultValue;
        }


        // TODO: We only support structs (not sure how to deal with arrays yet)

        /// <inheritdoc/>
        public override unsafe bool WriteValue(IntPtr destination, int alignment = 1)
        {
            if (typeof(T).IsValueType)
            {
                Unsafe.WriteUnaligned((void*) destination, DefaultValue);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool WriteValue(scoped ref byte destination, int alignment = 1)
        {
            if (typeof(T).IsValueType)
            {
                Unsafe.WriteUnaligned(ref destination, DefaultValue);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool WriteValue(scoped Span<byte> destination, int alignment = 1)
        {
            scoped ref byte destinationRef = ref destination.GetReference();
            return WriteValue(ref destinationRef, alignment);
        }
    }
}
