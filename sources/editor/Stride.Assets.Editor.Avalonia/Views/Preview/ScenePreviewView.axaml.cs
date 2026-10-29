// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// The view of the scene previews: scenes are not previewed, so it shows a message rather than the game view.
/// </summary>
[AssetPreviewView<ScenePreview>]
public partial class ScenePreviewView : StridePreviewView
{
    public ScenePreviewView()
    {
        InitializeComponent();
    }
}
