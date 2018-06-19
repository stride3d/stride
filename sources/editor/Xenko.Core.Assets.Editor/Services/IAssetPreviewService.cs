// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Assets.Editor.ViewModel;

namespace Xenko.Core.Assets.Editor.Services
{
    public interface IAssetPreviewService : IDisposable
    {
        void SetAssetToPreview(AssetViewModel asset);

        object GetCurrentPreviewView();

        event EventHandler<EventArgs> PreviewAssetUpdated;
    }
}
