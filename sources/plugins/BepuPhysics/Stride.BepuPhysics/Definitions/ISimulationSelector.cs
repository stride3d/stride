// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// Implement this to control which <see cref="BepuSimulation"/> a given component is assigned to
/// </summary>
public interface ISimulationSelector
{
    /// <summary>
    /// Go through available simulations and pick the most appropriate one for <paramref name="target"/>
    /// </summary>
    BepuSimulation Pick(BepuConfiguration configuration, Entity target);
}
