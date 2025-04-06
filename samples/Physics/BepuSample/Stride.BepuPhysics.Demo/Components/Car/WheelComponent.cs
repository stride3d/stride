// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

#warning This need rework/Rename and could be part of the API

namespace Stride.BepuPhysics.Demo.Components.Car
{
    [ComponentCategory("BepuDemo - Car")]
    public class WheelComponent : StartupScript
    {
        public float DamperLen { get; set; } = 0.5f;
        public float DamperRatio { get; set; } = 0.01f;
        public float DamperForce { get; set; } = 1000f;
    }
}
