// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Runtime.InteropServices;

using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Serializers;

namespace Xenko
{
    /// <summary>
    /// A FourCC descriptor.
    /// </summary>
    [DataSerializer(typeof(FourCC.Serializer))]
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct FourCC : IEquatable<FourCC>
    {
        private readonly uint value;

        /// <summary>
        /// Initializes a new instance of the <see cref="FourCC" /> struct.
        /// </summary>
        /// <param name="fourCC">The fourCC value as a string .</param>
        public FourCC(string fourCC)
        {
            if (fourCC.Length != 4)
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid length for FourCC(\"{0}\". Must be be 4 characters long ", fourCC), "fourCC");
            this.value = ((uint)fourCC[3]) << 24 | ((uint)fourCC[2]) << 16 | ((uint)fourCC[1]) << 8 | ((uint)fourCC[0]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FourCC" /> struct.
        /// </summary>
        /// <param name="byte1">The byte1.</param>
        /// <param name="byte2">The byte2.</param>
        /// <param name="byte3">The byte3.</param>
        /// <param name="byte4">The byte4.</param>
        public FourCC(char byte1, char byte2, char byte3, char byte4)
        {
            this.value = ((uint)byte4) << 24 | ((uint)byte3) << 16 | ((uint)byte2) << 8 | ((uint)byte1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FourCC" /> struct.
        /// </summary>
        /// <param name="fourCC">The fourCC value as an uint.</param>
        public FourCC(uint fourCC)
        {
            this.value = fourCC;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FourCC" /> struct.
        /// </summary>
        /// <param name="fourCC">The fourCC value as an int.</param>
        public FourCC(int fourCC)
        {
            this.value = unchecked((uint)fourCC);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SharpDX.Multimedia.FourCC"/> to <see cref="int"/>.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator uint(FourCC d)
        {
            return d.value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SharpDX.Multimedia.FourCC"/> to <see cref="int"/>.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator int(FourCC d)
        {
            return unchecked((int)d.value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="int"/> to <see cref="SharpDX.Multimedia.FourCC"/>.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FourCC(uint d)
        {
            return new FourCC(d);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="int"/> to <see cref="SharpDX.Multimedia.FourCC"/>.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FourCC(int d)
        {
            return new FourCC(d);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SharpDX.Multimedia.FourCC"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(FourCC d)
        {
            return d.ToString();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="SharpDX.Multimedia.FourCC"/>.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FourCC(string d)
        {
            return new FourCC(d);
        }

        public override string ToString()
        {
            return string.Format("{0}", new string(new[]
                                  {
                                      (char)(value & 0xFF),
                                      (char)((value >> 8) & 0xFF),
                                      (char)((value >> 16) & 0xFF),
                                      (char)((value >> 24) & 0xFF),
                                  }));
        }

        public bool Equals(FourCC other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FourCC && Equals((FourCC)obj);
        }

        public override int GetHashCode()
        {
            return (int)value;
        }

        public static bool operator ==(FourCC left, FourCC right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FourCC left, FourCC right)
        {
            return !left.Equals(right);
        }

        public class Serializer : DataSerializer<FourCC>
        {
            public override void Serialize(ref FourCC fourCC, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Serialize)
                    stream.Write(fourCC.value);
                else
                    fourCC = new FourCC(stream.ReadUInt32());
            }
        }
    }
}
