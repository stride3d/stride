// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Processors;

namespace Stride.BepuPhysics.Definitions;

internal static class SystemsOrderHelper
{

    //Note : transform processor's Draw() is at -200;

    /// <summary> Creation and management of constraints </summary>
    public const int ORDER_OF_CONSTRAINT_P = -900;
    /// <summary> Simulation Step(dt) & Transform Update() </summary>
    public const int ORDER_OF_GAME_SYSTEM = int.MaxValue;
    /// <summary> Drawing debug wireframe, must occur after <see cref="TransformProcessor.Draw"/> </summary>
    public const int ORDER_OF_DEBUG_P = 1000;
    /// <summary> Creation and management of collidable, rebuilds static meshes when they are moved, must occur after <see cref="TransformProcessor.Draw"/> </summary>
    public const int ORDER_OF_COLLIDABLE_P = -100;
}
