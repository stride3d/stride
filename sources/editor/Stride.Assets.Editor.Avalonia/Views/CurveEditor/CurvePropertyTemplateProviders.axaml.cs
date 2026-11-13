// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views.CurveEditor;

/// <summary>
/// The property templates of the curve properties (an "Edit curve" button), and the images of the curve editor.
/// </summary>
public sealed class CurvePropertyTemplateProviders : ResourceDictionary
{
    public CurvePropertyTemplateProviders()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
