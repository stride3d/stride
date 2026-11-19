// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The property templates of asset types: material shaders, sprite fonts, skeletons, animation clips and camera slots.
/// </summary>
public sealed class AssetPropertyTemplateProviders : ResourceDictionary
{
    public AssetPropertyTemplateProviders()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
