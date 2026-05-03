// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

public readonly struct ParameterAccessor
{
    public readonly int Offset;
    public readonly int Count;


    /// <summary>
    ///   Represents an invalid parameter accessor with no valid offset or count.
    /// </summary>
    public static readonly ParameterAccessor Invalid = new(offset: -1, count: 0);


    internal ParameterAccessor(int offset, int count)
    {
        Offset = offset;
        Count = count;
    }
}
