// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.View
{
    /// <summary>
    /// Interaction logic for AssetPickerWindow.xaml
    /// </summary>
    public partial class AssetPickerWindow : IAssetPickerDialog
    {
        /// <summary>
        /// Identifies the <see cref="Message"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(AssetPickerWindow), new PropertyMetadata(string.Empty));

        private static readonly List<SessionObjectViewModel> ExpandedObjects = new List<SessionObjectViewModel>();
        private readonly List<AssetViewModel> selectedAssets = new List<AssetViewModel>();

        public AssetPickerWindow(SessionViewModel session)
        {
            InitializeComponent();
            Session = session;
            AcceptedTypes = new List<Type>();
            AssetDoubleClickCommand = new AnonymousCommand(session.ServiceProvider, OnAssetDoubleClick);
            DataContext = this;
            Loaded += AssetPickerWindowLoaded;
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        }

        private void AssetPickerWindowLoaded(object sender, RoutedEventArgs e)
        {
            var initialDirectory = InitialLocation ?? Session.LocalPackages.First().AssetMountPoint;
            var selectedItem = initialDirectory;
            DirectoryTreeView.SelectedItems.Add(selectedItem);
            DirectoryTreeView.BringItemToView(selectedItem, x => (x as IChildViewModel)?.GetParent());

            if (InitialAsset != null)
            {
                AssetView.SelectAssets(InitialAsset.ToEnumerable<AssetViewModel>());
            }

            DirectoryTreeView.ExpandSessionObjects(ExpandedObjects);
        }

        public string Message { get { return (string)GetValue(MessageProperty); } set { SetValue(MessageProperty, value); } } 

        public DirectoryBaseViewModel InitialLocation { get; set; }

        public AssetViewModel InitialAsset { get; set; }

        public bool AllowMultiSelection { get; set; }

        public List<Type> AcceptedTypes { get; }

        public IReadOnlyCollection<AssetViewModel> SelectedAssets => selectedAssets;

        public SessionViewModel Session { get; }

        public AssetCollectionViewModel AssetView { get; set; }

        public ICommandBase AssetDoubleClickCommand { get; private set; }

        public Func<AssetViewModel, bool> Filter { get { return AssetView.CustomFilter; } set { AssetView.CustomFilter = value; } }

        public override async Task<DialogResult> ShowModal()
        {
            if (AcceptedTypes.Count > 0)
            {
                // Gather all registered asset types that are assignable to the given accepted types
                var assetTypes = AssetRegistry.GetPublicTypes().Where(x => AcceptedTypes.Any(y => y.IsAssignableFrom(x))).ToList();
                // Retrieve the filters that then match the collected asset types
                var activeFilters = AssetView.TypeFilters.Where(f => assetTypes.Any(t => string.Equals(t.FullName, f.Filter)));
                foreach (var filter in activeFilters)
                {
                    filter.IsReadOnly = true; // prevent the user from removing or deactivating the filter
                    AssetView.AddAssetFilter(filter);
                }
            }
            selectedAssets.Clear();

            await base.ShowModal();

            if (Result == Presentation.Services.DialogResult.Ok)
            {
                selectedAssets.AddRange(AssetView.SelectedAssets);
            }

            return Result;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            lock (ExpandedObjects)
            {
                ExpandedObjects.Clear();
                var items = DirectoryTreeView.FindVisualChildrenOfType<TreeViewItem>();
                foreach (var sessionObject in items.Where(x => x.IsExpanded).Select(x => x.DataContext).OfType<SessionObjectViewModel>())
                {
                    ExpandedObjects.Add(sessionObject);
                }
            }
            base.OnClosing(e);
        }

        private void OnAssetDoubleClick()
        {
            // Folder
            var folder = AssetView.SingleSelectedContent as DirectoryViewModel;
            if (folder != null)
            {
                AssetView.SelectedLocations.Clear();
                AssetView.SelectedLocations.Add(folder);
            }
            // Asset
            var asset = AssetView.SingleSelectedAsset;
            if (asset != null)
            {
                Result = Presentation.Services.DialogResult.Ok;
                Close();
            }
        }
    }
}
