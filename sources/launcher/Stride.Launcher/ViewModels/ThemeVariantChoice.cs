// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Launcher.ViewModels;

/// <summary>
/// A theme variant (dark, light...) the user can choose.
/// </summary>
/// <param name="Value">The value saved in <see cref="Services.LauncherSettings.ThemeVariant"/>.</param>
/// <param name="Name">The name shown to the user.</param>
public sealed record ThemeVariantChoice(string Value, string Name);
