// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core.Annotations;

namespace Stride.Core.Serialization
{
    public class HashSerializationWriter : BinarySerializationWriter
    {
        public HashSerializationWriter([NotNull] Stream outputStream) : base(outputStream)
        {
        }

        /// <inheritdoc/>
        public override unsafe void Serialize(ref string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            ref var @ref = ref Unsafe.AsRef(in value.GetPinnableReference());
            var bytes = MemoryMarshal.CreateSpan(ref Unsafe.As<char, byte>(ref @ref), value.Length << 1);
            Serialize(bytes);
        }
    }
}
