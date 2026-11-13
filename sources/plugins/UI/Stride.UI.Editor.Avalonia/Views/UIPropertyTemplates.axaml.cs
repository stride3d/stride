// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.UI.Editor.Avalonia.Views;

/// <summary>
/// The property templates of the UI elements: thickness, strip definition (the rows, columns and layers of a grid) and the
/// rotation of an image, in degrees.
/// </summary>
public sealed class UIPropertyTemplates : ResourceDictionary
{
    public UIPropertyTemplates()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
