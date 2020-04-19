// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics
{
    /// <summary>
    /// A recycle policy to check whether the specified resource must be disposed from a <see cref="GraphicsResourceAllocator"/>.
    /// </summary>
    /// <param name="resourceLink">The resource link.</param>
    /// <returns><c>true</c> if the specified resource must be disposed and remove from the , <c>false</c> otherwise.</returns>
    public delegate bool GraphicsResourceRecyclePolicyDelegate(GraphicsResourceLink resourceLink);
}
