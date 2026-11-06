// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Editor.Engine;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// Creates the offscreen viewports of the editor games of a session (<see cref="OffscreenGameViewport"/>).
/// </summary>
public sealed class OffscreenGameViewportService : IEditorGameViewportService
{
    private readonly IDispatcherService dispatcher;

    /// <summary>
    /// Creates the service of a session.
    /// </summary>
    /// <param name="dispatcher">The dispatcher of the UI thread.</param>
    public OffscreenGameViewportService(IDispatcherService dispatcher)
    {
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public IEditorGameViewport CreateViewport(Logger logger)
    {
        return new OffscreenGameViewport(dispatcher, logger);
    }
}
