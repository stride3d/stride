// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Engine.InputInteractions;

public enum InputInteractionType
{
    Default = 0,
    Camera = 100,
    SceneSelection = 200,
    Tools = 1000,
    Gizmo = 10000,
    Modal = 100000,
}
