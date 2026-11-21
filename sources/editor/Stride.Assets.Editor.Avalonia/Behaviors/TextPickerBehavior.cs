// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Makes an <see cref="AutoCompleteBox"/> a filtering list of texts for a text property, such as the shader names of a
/// material.
/// </summary>
/// <seealso cref="AutoCompletePickerBehavior"/>
public sealed class TextPickerBehavior : AutoCompletePickerBehavior
{
    /// <inheritdoc/>
    protected override string? GetItemText(object? item) => item as string;
}
