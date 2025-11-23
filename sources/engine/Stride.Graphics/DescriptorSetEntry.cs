// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Internal structure to store Descriptor entries in a <see cref="DescriptorSet"/>.
/// </summary>
/// <param name="value">The Descriptor, representing the Graphics Resource to bind.</param>
/// <param name="offset">
///   The offset in the Constant Buffer or the initial counter offset value for Unordered Access Views of Compute Shaders.
/// </param>
/// <param name="size">The size of the Buffer view, if applicable.</param>
internal struct DescriptorSetEntry(object value, int offset, int size)
{
    /// <summary><inheritdoc cref="DescriptorSetEntry(object, int, int)" path='/param[@name="value"]'/></summary>
    public object Value = value;

    /// <summary><inheritdoc cref="DescriptorSetEntry(object, int, int)" path='/param[@name="offset"]'/></summary>
    public int Offset = offset;

    /// <summary><inheritdoc cref="DescriptorSetEntry(object, int, int)" path='/param[@name="size"]'/></summary>
    public int Size = size;
}
