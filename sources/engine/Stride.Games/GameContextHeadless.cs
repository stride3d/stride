// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Games;

/// <summary>
///   A game context for headless (windowless) operation.
///   Used for automated testing with software renderers where no display is available.
/// </summary>
public class GameContextHeadless : GameContext<object>
{
    /// <inheritdoc/>
    public GameContextHeadless(int requestedWidth = 0, int requestedHeight = 0)
        : base(null, requestedWidth, requestedHeight)
    {
        ContextType = AppContextType.Headless;
    }
}
