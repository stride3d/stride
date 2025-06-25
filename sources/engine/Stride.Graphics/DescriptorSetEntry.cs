// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

internal struct DescriptorSetEntry(object value, int offset, int size)
{
    /// <summary>
    /// Used internally to store descriptor entries.
    /// </summary>
    public object Value = value;

        /// <summary>
        /// The offset, shared parameter for either cbuffer or unordered access view.
        /// Describes the cbuffer offset or the initial counter offset value for UAVs of compute shaders.
        /// </summary>
    public int Offset = offset;

    public int Size = size;
}
