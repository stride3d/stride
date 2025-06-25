// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   A recycle policy to check whether a Graphics Resource tracked by a <see cref="GraphicsResourceAllocator"/>
///   must be disposed / recycled.
/// </summary>
/// <param name="resourceLink">The Graphics Resource link.</param>
/// <returns>
///   <see langword="true"/> if the specified Graphics Resource must be disposed and removed from its allocator;
///   <see langword="false"/> otherwise.
/// </returns>
public delegate bool GraphicsResourceRecyclePolicyDelegate(GraphicsResourceLink resourceLink);
