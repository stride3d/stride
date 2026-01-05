// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

/// <summary>
///   Represents a structure that contains the information for accessing the data of
///   a parameter in a parameter collection.
/// </summary>
public readonly struct ParameterAccessor
{
    /// <summary>
    ///   The offset in the parameter collection's data where the data of the associated parameter can be found.
    /// </summary>
    public readonly int Offset;
    /// <summary>
    ///   The he number of elements the values of the parameter is composed of.
    ///   For single values, this is 1.
    /// </summary>
    public readonly int Count;


    /// <summary>
    ///   Represents an invalid parameter accessor with no valid offset or count.
    /// </summary>
    /// <remarks>
    ///   This static readonly field is used to indicate an invalid state for a parameter accessor,
    ///   where the offset is set to <c>-1</c> and the count is set to <c>0</c>.
    /// </remarks>
    public static readonly ParameterAccessor Invalid = new(offset: -1, count: 0);


    /// <summary>
    ///   Initializes a new instance of the <see cref="ParameterAccessor"/> structure.
    /// </summary>
    /// <param name="offset">The starting position of the accessor.</param>
    /// <param name="count">The number of elements the accessor can handle.</param>
    internal ParameterAccessor(int offset, int count)
    {
        Offset = offset;
        Count = count;
    }
}
