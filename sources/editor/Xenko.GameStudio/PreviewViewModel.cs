// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Specialized;
using System.Linq;

using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.GameStudio
{
    public class PreviewViewModel : DispatcherViewModel, IDisposable
    {
        private readonly SessionViewModel session;

        private IAssetPreviewService previewService;
        private object previewObject;
        
        public PreviewViewModel(SessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            this.session = session;
            session.ActiveAssetView.SelectedAssets.CollectionChanged += SelectedAssetsCollectionChanged;
            session.ActiveAssetsChanged += ActiveAssetsChanged;
        }

        public object PreviewObject { get { return previewObject; } private set { SetValue(ref previewObject, value); } }

        private IAssetPreviewService PreviewService
        {
            get
            {
                if (previewService != null)
                    return previewService;

                previewService = ServiceProvider.TryGet<IAssetPreviewService>();
                if (previewService == null)
                    return null;

                previewService.PreviewAssetUpdated += PreviewAssetUpdated;
                return previewService;
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(PreviewViewModel));
            Cleanup();
            base.Destroy();
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (previewService != null)
            {
                previewService.PreviewAssetUpdated -= PreviewAssetUpdated;
                previewService.Dispose();
            }

            session.ActiveAssetView.SelectedAssets.CollectionChanged -= SelectedAssetsCollectionChanged;
            session.ActiveAssetsChanged -= ActiveAssetsChanged;
        }

        private void ActiveAssetsChanged(object sender, ActiveAssetsChangedArgs e)
        {
            EnsureNotDestroyed(nameof(PreviewViewModel));
            PreviewService?.SetAssetToPreview(e.Assets.Count == 1 ? e.Assets.First() : null);
        }

        private void PreviewAssetUpdated(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() => PreviewObject = previewService.GetCurrentPreviewView());
        }

        private void SelectedAssetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EnsureNotDestroyed(nameof(PreviewViewModel));
            if (PreviewService == null)
                return;

            if (session.ActiveAssetView.SelectedAssets.Count == 1)
            {
                var selectedItem = session.ActiveAssetView.SelectedAssets.First();
                previewService.SetAssetToPreview(selectedItem);
            }
            else
            {
                previewService.SetAssetToPreview(null);
            }
        }
    }
}
