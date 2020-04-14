// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Empty implementation of <see cref="SerializationStream"/>.
    /// </summary>
    public class NullSerializationStream : SerializationStream
    {
        /// <inheritdoc/>
        public override void Serialize(ref bool value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref float value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref double value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref short value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref int value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref long value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref ushort value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref uint value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref ulong value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref string value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref char value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref byte value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(ref sbyte value)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(byte[] values, int offset, int count)
        {
        }

        /// <inheritdoc/>
        public override void Serialize(IntPtr memory, int count)
        {
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }
    }
}
