// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// A scheme to collect <see cref="IHeightfieldFunction"/> and combine them
/// </summary>
/// <remarks>
/// Physics would rather not perform virtual calls into every <see cref="IHeightfieldFunction"/> for evaluation of heights,
/// this interface provides the means to process <see cref="IHeightfieldFunction"/> as concrete types to build operation stacks
/// out of them and have the JIT take care of the rest
/// </remarks>
public interface IFunctionCollector
{
    /// <summary>
    /// Stack this heightfield function on top of the collector to contribute to the final height
    /// </summary>
    void Append<T>(T newT) where T : IHeightfieldFunction;
    
    /// <summary>
    /// Returns the current operation stack
    /// </summary>
    IHeightfieldFunction GetFunction();
}
