// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Engine.InputInteractions;

public interface IInputInteractionService
{
    /// <summary>
    /// Returns true if there is an in-progress interaction.
    /// </summary>
    bool HasActiveInteraction { get; }

    /// <summary>
    /// Request for interaction capture.
    /// </summary>
    void Request(InputInteractionRequest request);

    /// <summary>
    /// Returns true if <paramref name="interaction"/> is the in-progress interaction.
    /// </summary>
    bool IsActiveInteraction(IInputInteraction interaction);

    /// <summary>
    /// Returns true if the in-progress interaction's <see cref="IInputInteraction.Owner"/> is the same as <paramref name="owner"/>.
    /// </summary>
    bool IsActiveInteractionOwner(object owner);

    /// <summary>
    /// Force the in-progress interaction to cancel.
    /// </summary>
    void ForceTerminateActiveInteraction();
}
