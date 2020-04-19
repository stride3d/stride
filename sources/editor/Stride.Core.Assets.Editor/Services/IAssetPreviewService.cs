// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Services
{
    public interface IAssetPreviewService : IDisposable
    {
        void SetAssetToPreview(AssetViewModel asset);

        object GetCurrentPreviewView();

        event EventHandler<EventArgs> PreviewAssetUpdated;

        void OnShowPreview();

        void OnHidePreview();
    }
}
