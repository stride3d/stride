// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1649 // File name must match first type name

using System;
using Xenko.Core;

namespace Xenko.Updater
{
    /// <summary>
    /// Defines how to set and get values from a field of a given type for the <see cref="UpdateEngine"/>.
    /// </summary>
    public class UpdatableField<T> : UpdatableField
    {
        public UpdatableField(int offset)
        {
            Offset = offset;
            Size = Interop.SizeOf<T>();
        }

        /// <inheritdoc/>
        public override Type MemberType
        {
            get { return typeof(T); }
        }

        /// <inheritdoc/>
        public override void SetStruct(IntPtr obj, object data)
        {
#if IL
            // Note: IL is injected by UpdateEngineProcessor
            // Target
            ldarg obj

            // Load source (unboxed pointer)
            ldarg data
            unbox !T

            // *obj = *source
            cpobj !T
#endif
            throw new NotImplementedException();
        }
    }
}
