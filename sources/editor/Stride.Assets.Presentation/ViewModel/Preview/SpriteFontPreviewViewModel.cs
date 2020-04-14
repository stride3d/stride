// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Assets.Presentation.Preview;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(SpriteFontPreview))]
    public class SpriteFontPreviewViewModel : AssetPreviewViewModel
    {
        private SpriteFontPreview spriteFontPreview;
        private string previewString;

        public SpriteFontPreviewViewModel(SessionViewModel session)
            : base(session)
        {
            ClearTextCommand = new AnonymousCommand(ServiceProvider, () => PreviewString = string.Empty);
        }

        public string PreviewString { get { return previewString; } set { SetValue(ref previewString, value, () => spriteFontPreview.SetPreviewString(value)); } }

        public ICommandBase ClearTextCommand { get; }

        public override void AttachPreview(IAssetPreview preview)
        {
            spriteFontPreview = (SpriteFontPreview)preview;
            spriteFontPreview.SetPreviewString(PreviewString);
        }
    }
}
