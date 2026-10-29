// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// The view of the model-like previews; the plugins whose previews render a model derive their views from it.
/// </summary>
[AssetPreviewView<ModelPreview>]
[AssetPreviewView<PrefabPreview>]
[AssetPreviewView<PrefabModelPreview>]
[AssetPreviewView<ProceduralModelPreview>]
public class ModelPreviewView : StridePreviewView
{
    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(ModelPreviewView);
}
