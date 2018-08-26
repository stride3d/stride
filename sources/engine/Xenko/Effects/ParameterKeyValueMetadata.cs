// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Reflection;
using Xenko.Core;

namespace Xenko.Rendering
{
    public abstract class ParameterKeyValueMetadata : PropertyKeyMetadata
    {
        public abstract object GetDefaultValue();

        public abstract bool WriteBuffer(IntPtr dest, int alignment = 1);
    }

    /// <summary>
    /// Metadata used for <see cref="ParameterKey"/>
    /// </summary>
    public class ParameterKeyValueMetadata<T> : ParameterKeyValueMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        /// </summary>
        public ParameterKeyValueMetadata()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        /// </summary>
        /// <param name="setupDelegate">The setup delegate.</param>
        public ParameterKeyValueMetadata(T defaultValue)
        {
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        public readonly T DefaultValue;

        public override unsafe bool WriteBuffer(IntPtr dest, int alignment = 1)
        {
            // We only support structs (not sure how to deal with arrays yet
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                // Struct copy
                var value = DefaultValue;
                Interop.CopyInline((void*)dest, ref value);
                return true;
            }

            return false;
        }

        public override object GetDefaultValue()
        {
            return DefaultValue;
        }
    }
}
