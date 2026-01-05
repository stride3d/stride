// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Runtime.CompilerServices;

using Stride.Core;
using Stride.Core.UnsafeExtensions;

namespace Stride.Rendering
{
    public abstract class ParameterKeyValueMetadata : PropertyKeyMetadata
    {
        public abstract object GetDefaultValue();

        public abstract bool WriteValue(IntPtr destination, int alignment = 1);

        public abstract bool WriteValue(scoped ref byte destination, int alignment = 1);

        public abstract bool WriteValue(scoped Span<byte> destination, int alignment = 1);
    }

    /// <summary>
    /// Metadata used for <see cref="ParameterKey"/>
    /// </summary>

    public class ParameterKeyValueMetadata<T> : ParameterKeyValueMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        /// </summary>
        public T DefaultValue { get; internal set; }
        public override object GetDefaultValue() => DefaultValue;


        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        public ParameterKeyValueMetadata() { }

        /// </summary>
        /// <param name="setupDelegate">The setup delegate.</param>
        public ParameterKeyValueMetadata(T defaultValue)
        {
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the default value.
        /// </summary>

        // TODO: We only support structs (not sure how to deal with arrays yet)

        public override unsafe bool WriteValue(IntPtr destination, int alignment = 1)
        {
            if (typeof(T).IsValueType)
            {
                Unsafe.WriteUnaligned((void*) destination, DefaultValue);
                return true;
            }

            return false;
        }

        public override bool WriteValue(scoped ref byte destination, int alignment = 1)
        {
            if (typeof(T).IsValueType)
            {
                Unsafe.WriteUnaligned(ref destination, DefaultValue);
                return true;
            }

            return false;
        }

        public override bool WriteValue(scoped Span<byte> destination, int alignment = 1)
        {
            scoped ref byte destinationRef = ref destination.GetReference();
            return WriteValue(ref destinationRef, alignment);
        }
    }
}
