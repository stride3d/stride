// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// An <see cref="IHeightfieldSampler"/> that can be combined with other <see cref="IHeightfieldSampler"/>
/// </summary>
public interface IHeightfieldFunction : IHeightfieldSampler
{
    /// <summary>
    /// Appends this <see cref="IHeightfieldFunction"/> to the <see cref="IFunctionCollector"/> provided
    /// </summary>
    void AppendTo(IFunctionCollector solution);
}
