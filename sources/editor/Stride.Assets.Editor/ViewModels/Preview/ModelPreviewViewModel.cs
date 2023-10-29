// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<ModelPreview>]
public sealed class ModelPreviewViewModel : AssetPreviewViewModel<ModelPreview>
{
    private ModelPreview? modelPreview;

    public ModelPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
        ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
    }

    public ICommandBase ResetModelCommand { get; }

    protected override void OnAttachPreview(ModelPreview preview)
    {
        modelPreview = preview;
    }

    private void ResetModel()
    {
        modelPreview?.ResetCamera();
    }
}
