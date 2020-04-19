// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Specialized;
using System.Linq;

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.GameStudio
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

            RenderPreviewCommand = new AnonymousCommand<bool>(session.ServiceProvider, SetIsVisible);
        }

        public CommandBase RenderPreviewCommand { get; }

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

        private void SetIsVisible(bool isVisible)
        {
            if (isVisible)
                PreviewService.OnShowPreview();
            else
                PreviewService.OnHidePreview();
        }
    }
}
