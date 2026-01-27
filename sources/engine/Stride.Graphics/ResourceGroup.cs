// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Groups bound Graphics Resources and a Constant Buffer, that usually change at a given frequency.
/// </summary>
public class ResourceGroup
{
    /// <summary>
    ///   The Descriptor Set containing the bound Graphics Resources (i.e.
    ///   <see cref="Texture"/>s, <see cref="Buffer"/>s, <see cref="SamplerState"/>s, etc.)
    /// </summary>
    public DescriptorSet DescriptorSet;

    /// <summary>
    ///   The Constant Buffer containing data and values associated with the group.
    /// </summary>
    public BufferPoolAllocationResult ConstantBuffer;
}
