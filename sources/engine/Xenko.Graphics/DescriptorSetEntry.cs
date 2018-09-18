// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Graphics
{
    /// <summary>
    /// Used internally to store descriptor entries.
    /// </summary>
    internal struct DescriptorSetEntry
    {
        public object Value;

        /// <summary>
        /// The offset, shared parameter for either cbuffer or unordered access view.
        /// Describes the cbuffer offset or the initial counter offset value for UAVs of compute shaders.
        /// </summary>
        public int Offset;
        public int Size;

        public DescriptorSetEntry(object value, int offset, int size)
        {
            Value = value;
            Offset = offset;
            Size = size;
        }
    }
}
