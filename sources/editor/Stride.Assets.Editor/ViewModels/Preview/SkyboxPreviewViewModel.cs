// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<SkyboxPreview>]
public class SkyboxPreviewViewModel : AssetPreviewViewModel<SkyboxPreview>
{
    private SkyboxPreview? skyboxPreview;
    private float glossiness = 0.6f;
    private float metalness = 1.0f;

    public SkyboxPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
        ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
    }

    public ICommandBase ResetModelCommand { get; }

    public float Glossiness
    {
        get { return glossiness; }
        set
        {
            SetValue(ref glossiness, value);
            skyboxPreview?.SetGlossiness(value);
        }
    }

    public float Metalness
    {
        get { return metalness; }
        set
        {
            SetValue(ref metalness, value);
            skyboxPreview?.SetMetalness(value);
        }
    }

    protected override void OnAttachPreview(SkyboxPreview preview)
    {
        skyboxPreview = preview;
        if (skyboxPreview != null)
        {
            SetValue(ref glossiness, skyboxPreview.Glossiness);
            SetValue(ref metalness, skyboxPreview.Metalness);
        }
    }

    private void ResetModel()
    {
        skyboxPreview?.ResetCamera();
    }
}
