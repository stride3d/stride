// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using Xenko.Core.Annotations;

namespace Xenko.Core.Storage
{
    /// <summary>
    /// A hash to uniquely identify data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
#if !XENKO_ASSEMBLY_PROCESSOR
    [DataContract("ObjectId")]
#endif
    public unsafe partial struct ObjectId : IEquatable<ObjectId>, IComparable<ObjectId>
    {
        // ***************************************************************
        // NOTE: This file is shared with the AssemblyProcessor.
        // If this file is modified, the AssemblyProcessor has to be
        // recompiled separately. See build\Xenko-AssemblyProcessor.sln
        // ***************************************************************

        // Murmurshash3 ahsh size is 128 bits.
        public const int HashSize = 16;
        public const int HashStringLength = HashSize * 2;
        private const int HashSizeInUInt = HashSize / sizeof(uint);
        private const string HexDigits = "0123456789abcdef";

        public static readonly ObjectId Empty = new ObjectId();

        private uint hash1;
        private uint hash2;
        private uint hash3;
        private uint hash4;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> struct.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <exception cref="System.ArgumentNullException">hash</exception>
        /// <exception cref="System.InvalidOperationException">ObjectId value doesn't match expected size.</exception>
        public ObjectId([NotNull] byte[] hash)
        {
            if (hash == null) throw new ArgumentNullException(nameof(hash));

            if (hash.Length != HashSize)
                throw new InvalidOperationException("ObjectId value doesn't match expected size.");

            fixed (byte* hashSource = hash)
            {
                var hashSourceCurrent = (uint*)hashSource;
                hash1 = *hashSourceCurrent++;
                hash2 = *hashSourceCurrent++;
                hash3 = *hashSourceCurrent++;
                hash4 = *hashSourceCurrent;
            }
        }

        public ObjectId(uint hash1, uint hash2, uint hash3, uint hash4)
        {
            this.hash1 = hash1;
            this.hash2 = hash2;
            this.hash3 = hash3;
            this.hash4 = hash4;
        }

        public static unsafe explicit operator ObjectId(Guid guid)
        {
            return *(ObjectId*)&guid;
        }

        public static ObjectId Combine(ObjectId left, ObjectId right)
        {
            // Note: we don't carry (probably not worth the performance hit)
            return new ObjectId
            {
                hash1 = left.hash1 * 3 + right.hash1,
                hash2 = left.hash2 * 3 + right.hash2,
                hash3 = left.hash3 * 3 + right.hash3,
                hash4 = left.hash4 * 3 + right.hash4,
            };
        }

        public static void Combine(ref ObjectId left, ref ObjectId right, out ObjectId result)
        {
            // Note: we don't carry (probably not worth the performance hit)
            result = new ObjectId
            {
                hash1 = left.hash1 * 3 + right.hash1,
                hash2 = left.hash2 * 3 + right.hash2,
                hash3 = left.hash3 * 3 + right.hash3,
                hash4 = left.hash4 * 3 + right.hash4,
            };
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="ObjectId"/> to <see cref="byte[]"/>.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <returns>The result of the conversion.</returns>
        [NotNull]
        public static explicit operator byte[](ObjectId objectId)
        {
            var result = new byte[HashSize];
            var hashSource = &objectId.hash1;
            fixed (byte* hashDest = result)
            {
                var hashSourceCurrent = (uint*)hashSource;
                var hashDestCurrent = (uint*)hashDest;
                for (var i = 0; i < HashSizeInUInt; ++i)
                    *hashDestCurrent++ = *hashSourceCurrent++;
            }
            return result;
        }

        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Tries to parse an <see cref="ObjectId"/> from a string.
        /// </summary>
        /// <param name="input">The input hexa string.</param>
        /// <param name="result">The result ObjectId.</param>
        /// <returns><c>true</c> if parsing was successfull, <c>false</c> otherwise</returns>
        public static bool TryParse([NotNull] string input, out ObjectId result)
        {
            if (input.Length != HashStringLength)
            {
                result = Empty;
                return false;
            }

            var hash = new byte[HashSize];
            for (var i = 0; i < HashStringLength; i += 2)
            {
                var c1 = input[i];
                var c2 = input[i + 1];

                int digit1, digit2;
                if (((digit1 = HexDigits.IndexOf(c1)) == -1)
                    || ((digit2 = HexDigits.IndexOf(c2)) == -1))
                {
                    result = Empty;
                    return false;
                }

                hash[i >> 1] = (byte)((digit1 << 4) | digit2);
            }

            result = new ObjectId(hash);
            return true;
        }

        /// <inheritdoc/>
        public bool Equals(ObjectId other)
        {
            // Compare content
            fixed (uint* xPtr = &hash1)
            {
                var x1 = xPtr;
                var y1 = &other.hash1;

                for (var i = 0; i < HashSizeInUInt; ++i)
                {
                    if (*x1++ != *y1++)
                        return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ObjectId && Equals((ObjectId)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            fixed (uint* objPtr = &hash1)
            {
                var obj1 = (int*)objPtr;
                return *obj1;
            }
        }

        /// <inheritdoc/>
        public int CompareTo(ObjectId other)
        {
            // Compare content
            fixed (uint* xPtr = &hash1)
            {
                var x1 = xPtr;
                var y1 = &other.hash1;

                for (var i = 0; i < HashSizeInUInt; ++i)
                {
                    var compareResult = (*x1++).CompareTo(*y1++);
                    if (compareResult != 0)
                        return compareResult;
                }
            }

            return 0;
        }

        public override string ToString()
        {
            var c = new char[HashStringLength];

            fixed (uint* hashStart = &hash1)
            {
                var hashBytes = (byte*)hashStart;
                for (var i = 0; i < HashStringLength; ++i)
                {
                    var index0 = i >> 1;
                    var b = (byte)(hashBytes[index0] >> 4);
                    c[i++] = HexDigits[b];

                    b = (byte)(hashBytes[index0] & 0x0F);
                    c[i] = HexDigits[b];
                }
            }

            return new string(c);
        }

        /// <summary>
        /// Gets a <see cref="Guid"/> from this object identifier.
        /// </summary>
        /// <returns>Guid.</returns>
        public Guid ToGuid()
        {
            fixed (void* hashStart = &hash1)
            {
                return *(Guid*)hashStart;
            }
        }

        /// <summary>
        /// News this instance.
        /// </summary>
        /// <returns>ObjectId.</returns>
        public static ObjectId New()
        {
            return FromBytes(Guid.NewGuid().ToByteArray());
        }

        /// <summary>
        /// Computes a hash from a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <returns>The hash of the object.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        public static ObjectId FromBytes([NotNull] byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            return FromBytes(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Computes a hash from a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="count">The number of bytes to read from the buffer starting at offset position.</param>
        /// <returns>The hash of the object.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        public static ObjectId FromBytes([NotNull] byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var builder = new ObjectIdBuilder();
            builder.Write(buffer, offset, count);
            return builder.ComputeHash();
        }
    }
}
