// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Controls;

namespace Xenko.Editor.Preview.View
{
    [TemplatePart(Name = "PART_XenkoView", Type = typeof(ContentPresenter))]
    public class XenkoPreviewView : Control, IPreviewView
    {
        private IPreviewBuilder builder;

        private IAssetPreview previewer;

        static XenkoPreviewView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(XenkoPreviewView), new FrameworkPropertyMetadata(typeof(XenkoPreviewView)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateXenkoView();
        }

        public void InitializeView(IPreviewBuilder previewBuilder, IAssetPreview assetPreview)
        {
            previewer = assetPreview;
            builder = previewBuilder;
            var viewModel = previewer.PreviewViewModel;
            if (viewModel != null)
            {
                viewModel.AttachPreview(previewer);
                DataContext = viewModel;
            }
            UpdateXenkoView();

            Loaded += OnLoaded;
        }

        public void UpdateView(IAssetPreview assetPreview)
        {
            var viewModel = previewer.PreviewViewModel;
            if (viewModel != null)
            {
                viewModel.AttachPreview(previewer);
                DataContext = viewModel;
            }
            UpdateXenkoView();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            previewer?.OnViewAttached();
        }

        private void UpdateXenkoView()
        {
            var xenkoViewPresenter = (ContentPresenter)GetTemplateChild("PART_XenkoView");
            if (xenkoViewPresenter != null && builder != null)
            {
                var xenkoView = builder.GetXenkoView();
                xenkoViewPresenter.Content = xenkoView;
            }
        }
    }
}
