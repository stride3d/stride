// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Engine.InputInteractions;

public class InputInteractionRequest
{
    public string Name { get; init; }

    public InputInteractionType InteractionType { get; init; } = InputInteractionType.Tools;
    /// <summary>
    /// Higher value is given priority to tie-break multiple requests from the same <see cref="InteractionType"/>.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Function that instantiates the interaction if the request is accepted.
    /// </summary>
    public required Func<IInputInteraction> Factory { get; init; }
}
