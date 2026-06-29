// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views;
using Stride.GameStudio.View;

namespace Stride.GameStudio.AutoTesting;

/// <summary>
/// Window class names used by fixtures and the runner for string-based window lookups. Defined via
/// <c>nameof</c> against the real types so a window-type rename is caught at compile time (and the
/// value tracks the new name) instead of silently breaking the lookups.
/// </summary>
public static class GameStudioWindowNames
{
    public const string GameStudio = nameof(GameStudioWindow);
    public const string ProjectSelection = nameof(ProjectSelectionWindow);
}
