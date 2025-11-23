// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;

namespace Stride.Rendering;

/// <summary>
///   Represents the layout of a collection of parameters, including information about
///   how parameters are organized and accessed, the size of the buffer used to store
///   the values of these parameters, and the number of resources used.
/// </summary>
/// <remarks>
///   This layout is typically used to optimize the storage and access patterns for parameters,
///   generating Constant Buffers, updating them, setting resources, and binding them to the graphics pipeline.
/// </remarks>
public class ParameterCollectionLayout
{
    /// <summary>
    ///   A collection of <see cref="ParameterKeyInfo"/> structures describing the offset and size
    ///   of each value parameter, and the binding slot of each Graphics Resource.
    /// </summary>
    public FastListStruct<ParameterKeyInfo> LayoutParameterKeyInfos = [];

    /// <summary>
    ///   The number of Graphics Resources in the associated parameter collection
    ///   (<c>Texture</c>s, <c>Buffer</c>s, <c>SamplerState</c>s, etc.)
    /// </summary>
    public int ResourceCount;

    /// <summary>
    ///   The size of the buffer used to store the values of the parameters in the collection
    ///   (e.g., <see langword="float"/>s, <see langword="int"/>s, etc., for Constant Buffers).
    /// </summary>
    public int BufferSize;
}
