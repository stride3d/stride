// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

[AssetEditorView<ScriptSourceFileEditorViewModel>]
public partial class ScriptSourceFileEditorView : UserControl, IAssetEditorView
{
    public ScriptSourceFileEditorView()
    {
        InitializeComponent();
    }
}
