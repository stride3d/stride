// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The view of a visual script in this Game Studio: visual scripting is not ported, so it says to open the asset in the WPF Game
/// Studio, which keeps the visual script editor.
/// </summary>
[AssetEditorView<VisualScriptEditorViewModel>]
public sealed partial class VisualScriptEditorView : UserControl, IAssetEditorView
{
    public VisualScriptEditorView()
    {
        InitializeComponent();
    }
}
