// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.FixAssetReferences.Views
{
    /// <summary>
    /// Interaction logic for FixAssetReferencesWindow.xaml
    /// </summary>
    public partial class FixAssetReferencesWindow : IFixReferencesDialog
    {
        public FixAssetReferencesWindow(IViewModelServiceProvider serviceProvider)
        {
            InitializeComponent();
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        }

        /// <inheritdoc/>
        public void ApplyReferenceFixes()
        {
            var viewModel = (FixAssetReferencesViewModel)DataContext;
            viewModel.ProcessFixes();
        }
    }
}
