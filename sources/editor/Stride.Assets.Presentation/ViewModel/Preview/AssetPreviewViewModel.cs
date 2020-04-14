// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModel;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    /// <summary>
    /// Base implementation of <see cref="IAssetPreview"/>.
    /// </summary>
    public abstract class AssetPreviewViewModel : DispatcherViewModel, IAssetPreviewViewModel
    {
        public SessionViewModel Session { get; }

        protected AssetPreviewViewModel(SessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            this.Session = session;
        }

        /// <inheritdoc/>
        public abstract void AttachPreview(IAssetPreview preview);
    }
}
