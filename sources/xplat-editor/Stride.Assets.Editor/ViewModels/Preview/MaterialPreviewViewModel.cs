// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Editor.Annotations;
using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<MaterialPreview>]
public sealed class MaterialPreviewViewModel : AssetPreviewViewModel<MaterialPreview>
{
    private MaterialPreview materialPreview;
    private MaterialPreviewPrimitive selectedPrimitive;

    public MaterialPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
    }

    public Type PrimitiveTypes => typeof(MaterialPreviewPrimitive);

    public MaterialPreviewPrimitive SelectedPrimitive { get { return selectedPrimitive; } set { SetValue(ref selectedPrimitive, value); SetPrimitive(value); } }

    protected override void OnAttachPreview(MaterialPreview preview)
    {
        materialPreview = preview;
        SetPrimitive(SelectedPrimitive);
    }

    private void SetPrimitive(MaterialPreviewPrimitive primitive)
    {
        materialPreview.SetPrimitive(primitive);
    }
}
