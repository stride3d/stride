// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;

using Stride.Core.Assets.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// The preview of the editor: it builds the asset to preview in its preview game and gives the view that shows it.
    /// </summary>
    public interface IAssetPreviewService : IAssetPreviewUpdater, IDisposable
    {
        object GetCurrentPreviewView();

        event EventHandler<EventArgs> PreviewAssetUpdated;

        void OnShowPreview();

        void OnHidePreview();
    }
}
