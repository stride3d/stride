// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.BepuPhysics.Definitions;

internal static class SystemsOrderHelper
{
    //Note : transform processor's Draw() is at -200;

    /// <summary> Creation and management of constraints </summary>
    public const int ORDER_OF_CONSTRAINT_P = -900;
    /// <summary> Simulation Step(dt) & Transform Update()</summary>
    /// <remarks>
    /// Must happen before <see cref="ScriptSystem.Update"/> to make sure interpolated bodies update their positions before users read them,
    /// but after <see cref="InputSystem.Update"/> otherwise users would be reading last frame's input in <see cref="Components.ISimulationUpdate"/>
    /// </remarks>
    public const int ORDER_OF_GAME_SYSTEM = InputSystem.DefaultUpdateOrder + 1;
    /// <summary> Drawing debug wireframe, must occur after <see cref="TransformProcessor.Draw"/> </summary>
    public const int ORDER_OF_DEBUG_P = 1000;
    /// <summary> Creation and management of collidable, rebuilds static meshes when they are moved, must occur after <see cref="TransformProcessor.Draw"/> </summary>
    public const int ORDER_OF_COLLIDABLE_P = -100;
}
