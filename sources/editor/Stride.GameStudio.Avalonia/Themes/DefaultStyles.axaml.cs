// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Stride.GameStudio.Avalonia.Themes;

internal sealed class DefaultStyles : Styles
{
    public DefaultStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
