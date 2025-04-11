// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Stride.Core.Presentation.Avalonia.Themes;

public class HyperlinkStyle : Styles
{
    public HyperlinkStyle(IServiceProvider? sp = null)
        : base()
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
