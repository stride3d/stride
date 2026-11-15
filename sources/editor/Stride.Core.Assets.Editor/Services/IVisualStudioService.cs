// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Finds the Visual Studio instance that has a solution open. The WPF editor implements it on Windows; without it, Open in IDE
/// always starts a new instance.
/// </summary>
public interface IVisualStudioService
{
    /// <summary>
    /// Finds the Visual Studio instance that has the given solution open.
    /// </summary>
    /// <param name="solutionPath">The solution.</param>
    /// <param name="makeActive">Whether to bring its window to the front (and restore it when minimized).</param>
    /// <returns>The process of the instance, or <c>null</c> when none has the solution open.</returns>
    Task<Process?> GetInstanceAsync(UFile solutionPath, bool makeActive);
}
