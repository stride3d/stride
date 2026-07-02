// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Games;

namespace Stride.Engine.InputInteractions;

public interface IInputInteraction
{
    /// <summary>
    /// The object that requested for the interaction.
    /// </summary>
    object Owner { get; }

    /// <summary>
    /// Called after it has been instantiated.
    /// </summary>
    void Start();

    /// <summary>
    /// Returns true if this interaction should continue running.
    /// </summary>
    bool Update(GameTime gameTime);

    /// <summary>
    /// Called after <see cref="Update(GameTime)"/> returns false (ie. completed its interaction run).
    /// </summary>
    void End();

    /// <summary>
    /// Called if the interaction has been terminated externally.
    /// </summary>
    void Cancel();
}
