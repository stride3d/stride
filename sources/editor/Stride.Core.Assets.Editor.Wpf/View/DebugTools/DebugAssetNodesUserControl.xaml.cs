// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.View.DebugTools
{
    /// <summary>
    /// Interaction logic for DebugQuantumUserControl.xaml
    /// </summary>
    public partial class DebugAssetNodesUserControl : IDebugPage
    {

        public DebugAssetNodesUserControl(SessionViewModel session)
        {
            InitializeComponent();
            DataContext = new DebugAssetNodeCollectionViewModel(session);
        }

        public string Title { get; set; }

        public DebugAssetNodeCollectionViewModel ViewModel => (DebugAssetNodeCollectionViewModel)DataContext;
    }
}
