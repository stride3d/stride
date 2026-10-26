// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.BuildEngine;

namespace Stride.Editor.Build;

/// <summary>
/// Imports the shaders of the packages of a session, which <see cref="GameStudioBuilderService"/> builds before anything else.
/// </summary>
/// <remarks>
/// The shaders are Stride assets, which this layer does not know: the implementation is in <c>Stride.Assets.Editor</c>.
/// </remarks>
public interface IShaderImporter
{
    /// <summary>
    /// Creates the build steps that import the shaders of the packages of the session that are not imported yet.
    /// </summary>
    /// <param name="session">The session whose packages to import the shaders of.</param>
    /// <returns>The build steps of the import, or <c>null</c> when there is nothing to import.</returns>
    ListBuildStep? CreateSystemShaderBuildSteps(SessionViewModel session);
}
