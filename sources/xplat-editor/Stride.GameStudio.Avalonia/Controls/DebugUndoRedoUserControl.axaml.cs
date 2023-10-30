// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.GameStudio.Avalonia.Controls;

public partial class DebugUndoRedoUserControl : UserControl
{
    public DebugUndoRedoUserControl()
    {
        InitializeComponent();
    }

    public void InitializeComponent(bool loadXaml = true)
    {
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
