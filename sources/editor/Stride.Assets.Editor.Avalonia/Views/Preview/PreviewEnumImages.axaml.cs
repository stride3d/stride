// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// The images of the enum values of the previews (the primitives of the material preview).
/// </summary>
public sealed class PreviewEnumImages : ResourceDictionary
{
    public PreviewEnumImages()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
