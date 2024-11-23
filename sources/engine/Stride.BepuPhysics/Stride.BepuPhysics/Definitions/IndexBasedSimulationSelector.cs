// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// This selector picks the simulation based on the simulation index
/// </summary>
[DataContract]
public record IndexBasedSimulationSelector : ISimulationSelector
{
    public int Index { get; init; }

    public BepuSimulation Pick(BepuConfiguration configuration, Entity target)
    {
        return configuration.BepuSimulations[Index];
    }
}
