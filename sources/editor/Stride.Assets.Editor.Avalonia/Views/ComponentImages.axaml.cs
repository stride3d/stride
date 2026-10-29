// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The images of the entity components, keyed by component type.
/// </summary>
public sealed class ComponentImages : ResourceDictionary
{
    public ComponentImages()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
