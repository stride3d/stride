// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

/// <summary>
///   Represents a structure that contains the information for accessing the data of
///   an object in a parameter collection.
/// </summary>
public readonly struct ObjectParameterAccessor<T>
{
    /// <summary>
    ///   The binding slot in the parameter collection's resources where the object
    ///   of the associated parameter can be found.
    /// </summary>
    internal readonly int BindingSlot;
    /// <summary>
    ///   The he number of elements the values of the parameter is composed of.
    ///   For single values, this is 1.
    /// </summary>
    internal readonly int Count;


    /// <summary>
    ///   Initializes a new instance of the <see cref="ObjectParameterAccessor{T}"/> structure.
    /// </summary>
    /// <param name="bindingSlot">The binding slot of the accessor.</param>
    /// <param name="count">The number of elements the accessor can handle.</param>
    internal ObjectParameterAccessor(int bindingSlot, int count)
    {
        BindingSlot = bindingSlot;
        Count = count;
    }
}
