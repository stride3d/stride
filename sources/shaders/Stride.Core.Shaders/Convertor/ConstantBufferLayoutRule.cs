// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Convertor
{
    /// <summary>
    /// A single map rule.
    /// </summary>
    public class ConstantBufferLayoutRule
    {
        /// <summary>
        /// Gets or sets from name.
        /// </summary>
        /// <value>
        /// From name.
        /// </value>
        public string Register { get; set; }

        /// <summary>
        /// Gets or sets the binding.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public string Binding { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("Register: {0}, Binding: {1}", this.Register, this.Binding);
        }
    }
}
