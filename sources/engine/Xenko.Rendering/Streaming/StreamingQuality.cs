// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Streaming
{
    /// <summary>
    /// Resources streaming quality level value type.
    /// </summary>
    public struct StreamingQuality
    {
        /// <summary>
        /// The mininum quality value.
        /// </summary>
        public static readonly StreamingQuality Mininum = new StreamingQuality(0.0f);

        /// <summary>
        /// The maximum quality value.
        /// </summary>
        public static readonly StreamingQuality Maximum = new StreamingQuality(1.0f);

        /// <summary>
        /// The quality value in range: [0; 1].
        /// </summary>
        public float Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingQuality"/> struct.
        /// </summary>
        /// <param name="value">The quality value (range [0;1]).</param>
        public StreamingQuality(float value)
        {
            Value = value;
        }

        /// <summary>
        /// Normalizes this quality value to range [0;1].
        /// </summary>
        public void Normalize()
        {
            Value = MathUtil.Clamp(Value, 0, 1);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="StreamingQuality"/> to <see cref="System.Single"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator float(StreamingQuality value)
        {
            return value.Value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Single"/> to <see cref="StreamingQuality"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator StreamingQuality(float value)
        {
            return new StreamingQuality(value);
        }
    }
}
