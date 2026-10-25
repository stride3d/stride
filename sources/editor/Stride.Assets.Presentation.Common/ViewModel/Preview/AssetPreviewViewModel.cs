// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    /// <summary>
    /// Base implementation of <see cref="IAssetPreview"/>.
    /// </summary>
    public abstract class AssetPreviewViewModel : DispatcherViewModel, IAssetPreviewViewModel
    {
        public ISessionViewModel Session { get; }

        protected AssetPreviewViewModel(ISessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            this.Session = session;
        }

        /// <inheritdoc/>
        public abstract void AttachPreview(IAssetPreview preview);
    }

    /// <summary>
    /// Base implementation of <see cref="IAssetPreviewViewModel"/> for a view model attached to one type of preview.
    /// </summary>
    /// <typeparam name="TPreview">The type of the preview this view model is attached to.</typeparam>
    public abstract class AssetPreviewViewModel<TPreview> : AssetPreviewViewModel
        where TPreview : IAssetPreview
    {
        protected AssetPreviewViewModel(ISessionViewModel session)
            : base(session)
        {
        }

        /// <inheritdoc/>
        public sealed override void AttachPreview(IAssetPreview preview)
        {
            OnAttachPreview((TPreview)preview);
        }

        /// <summary>
        /// Attaches the given preview to this view model.
        /// </summary>
        /// <param name="preview">The preview to attach.</param>
        protected abstract void OnAttachPreview(TPreview preview);
    }
}
