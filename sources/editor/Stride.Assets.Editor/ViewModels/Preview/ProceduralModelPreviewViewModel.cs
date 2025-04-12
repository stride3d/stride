// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<ProceduralModelPreview>]
public class ProceduralModelPreviewViewModel : AssetPreviewViewModel<ProceduralModelPreview>
{
    private ProceduralModelPreview? modelPreview;

    public ProceduralModelPreviewViewModel(SessionViewModel session)
        : base(session)
    {
        ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
    }

    public ICommandBase ResetModelCommand { get; }

    protected override void OnAttachPreview(ProceduralModelPreview preview)
    {
        modelPreview = preview;
    }

    private void ResetModel()
    {
        modelPreview?.ResetCamera();
    }
}
