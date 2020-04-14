// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Commands;
using Xenko.Assets.Presentation.Preview;
using Xenko.Editor.Preview;
using Xenko.Editor.Preview.ViewModel;

namespace Xenko.Assets.Presentation.ViewModel.Preview
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
