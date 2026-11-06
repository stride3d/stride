// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Editor.Preview;

namespace Stride.Editor.Avalonia.Preview;

/// <summary>
/// A <see cref="IPreviewBuilder"/> that hosts the preview game in an Avalonia control.
/// </summary>
public interface IAvaloniaPreviewBuilder : IPreviewBuilder
{
    /// <summary>
    /// Gets the control the preview game renders in.
    /// </summary>
    /// <returns>The control, or <c>null</c> once the builder is disposed.</returns>
    Control? GetStrideView();
}
