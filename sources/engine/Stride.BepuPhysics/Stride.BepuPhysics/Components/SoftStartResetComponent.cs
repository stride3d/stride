// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace Stride.BepuPhysics.Components;

public sealed class SoftStartResetComponent : StartupScript
{
    public int SimulationIndex { get; set; }

    public override void Start()
    {
        Services.GetService<BepuConfiguration>().BepuSimulations[SimulationIndex].ResetSoftStart();
    }
}