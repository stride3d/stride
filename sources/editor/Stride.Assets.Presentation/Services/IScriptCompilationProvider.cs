// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using Microsoft.CodeAnalysis;

namespace Stride.Assets.Presentation.Services;

/// <summary>
/// Gives the latest compilation of the scripts of the session's game project, which the visual script view models need to
/// generate the slots of their blocks (the methods and types that a block calls or reads).
/// </summary>
/// <remarks>
/// The editor's script services implement it: in the WPF editor, the source code resolver of the scripts. Without it, the
/// blocks keep the slots they were saved with.
/// </remarks>
public interface IScriptCompilationProvider
{
    /// <summary>
    /// Gets the latest compilation of the game project's scripts, or <c>null</c> before the first compilation ends.
    /// </summary>
    Compilation? LatestCompilation { get; }

    /// <summary>
    /// Raised when <see cref="LatestCompilation"/> changes.
    /// </summary>
    event EventHandler? LatestCompilationChanged;
}
