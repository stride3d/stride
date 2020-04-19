// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Assets.Presentation.Preview;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(SkyboxPreview))]
    public class SkyboxPreviewViewModel : AssetPreviewViewModel
    {
        private SkyboxPreview skyboxPreview;
        private float glossiness = 0.6f;
        private float metalness = 1.0f;

        public SkyboxPreviewViewModel(SessionViewModel session)
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

        public override void AttachPreview(IAssetPreview preview)
        {
            skyboxPreview = (SkyboxPreview)preview;
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
}
