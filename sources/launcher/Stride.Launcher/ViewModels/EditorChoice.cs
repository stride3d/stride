// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Launcher.ViewModels;

/// <summary>
/// An entry of the Game Studio selector under Start.
/// </summary>
/// <param name="Name">The name of the editor's executable, such as <c>Stride.GameStudio.Avalonia.Desktop</c>.</param>
/// <param name="Label">The text of the entry.</param>
/// <param name="IsAvailable">Whether the version has this editor; the entry is greyed otherwise.</param>
public sealed record EditorChoice(string Name, string Label, bool IsAvailable);
