// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Editor.EditorGame.Game;

/// <summary>
/// Declares an <see cref="IEditorGameService"/> that editors whose controller is a <see cref="ControllerType"/> create when their game starts.
/// Its constructor can take the controller and the editor view model, matched by parameter type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[Stride.Core.Reflection.AssemblyScan]
public sealed class EditorGameServiceAttribute : Attribute
{
    public EditorGameServiceAttribute(Type controllerType)
    {
        ControllerType = controllerType;
    }

    public Type ControllerType { get; }

    /// <summary>
    /// Sorts the services of a controller (ties by type name); the editor's own services use values below <see cref="DefaultOrder"/>.
    /// </summary>
    public int Order { get; set; } = DefaultOrder;

    public const int DefaultOrder = 1000;
}
