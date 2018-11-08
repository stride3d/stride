// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Xenko.Core.Assets.Editor.View.Controls;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Extensions;

using Xceed.Wpf.DataGrid;

namespace Xenko.Core.Assets.Editor.View
{
    /// <summary>
    /// Interaction logic for AssetViewUserControl.xaml
    /// </summary>
    public partial class AssetViewUserControl
    {
        /// <summary>
        /// Identifies the <see cref="AssetCollection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AssetCollectionProperty = DependencyProperty.Register(nameof(AssetCollection), typeof(AssetCollectionViewModel), typeof(AssetViewUserControl), new PropertyMetadata(null, AssetCollectionChanged));

        /// <summary>
        /// Identifies the <see cref="AssetContextMenu"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AssetContextMenuProperty = DependencyProperty.Register(nameof(AssetContextMenu), typeof(Control), typeof(AssetViewUserControl), new PropertyMetadata(null, OnAssetContextMenuChanged));

        /// <summary>
        /// Identifies the <see cref="CanEditAssets"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanEditAssetsProperty = DependencyProperty.Register(nameof(CanEditAssets), typeof(bool), typeof(AssetViewUserControl), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CanAddAssets"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanAddAssetsProperty = DependencyProperty.Register(nameof(CanAddAssets), typeof(bool), typeof(AssetViewUserControl), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CanDeleteAssets"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanDeleteAssetsProperty = DependencyProperty.Register(nameof(CanDeleteAssets), typeof(bool), typeof(AssetViewUserControl), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CanRecursivelyDisplayAssets"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanRecursivelyDisplayAssetsProperty = DependencyProperty.Register(nameof(CanRecursivelyDisplayAssets), typeof(bool), typeof(AssetViewUserControl), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CanRecursivelyDisplayAssets"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GiveFocusOnSelectionChangeProperty = DependencyProperty.Register(nameof(GiveFocusOnSelectionChange), typeof(bool), typeof(AssetViewUserControl), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="ThumbnailZoomIncrement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbnailZoomIncrementProperty = DependencyProperty.Register(nameof(ThumbnailZoomIncrement), typeof(double), typeof(AssetViewUserControl), new PropertyMetadata(16.0));

        /// <summary>
        /// Identifies the <see cref="ThumbnailMinimumSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbnailMinimumSizeProperty = DependencyProperty.Register(nameof(ThumbnailMinimumSize), typeof(double), typeof(AssetViewUserControl), new PropertyMetadata(16.0));

        /// <summary>
        /// Identifies the <see cref="ThumbnailMaximumSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbnailMaximumSizeProperty = DependencyProperty.Register(nameof(ThumbnailMaximumSize), typeof(double), typeof(AssetViewUserControl), new PropertyMetadata(128.0));

        /// <summary>
        /// Identifies the <see cref="TileThumbnailSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TileThumbnailSizeProperty = DependencyProperty.Register(nameof(TileThumbnailSize), typeof(double), typeof(AssetViewUserControl), new PropertyMetadata(96.0));

        /// <summary>
        /// Identifies the <see cref="GridThumbnailSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridThumbnailSizeProperty = DependencyProperty.Register(nameof(GridThumbnailSize), typeof(double), typeof(AssetViewUserControl), new PropertyMetadata(16.0));

        /// <summary>
        /// Identifies the <see cref="AssetDoubleClick"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AssetDoubleClickProperty = DependencyProperty.Register(nameof(AssetDoubleClick), typeof(ICommand), typeof(AssetViewUserControl));

        /// <summary>
        /// Gets the command that initiate the edition of the currently selected item.
        /// </summary>
        public static RoutedCommand BeginEditCommand { get; }

        /// <summary>
        /// Gets the command that will increase the size of thumbnails.
        /// </summary>
        public static RoutedCommand ZoomInCommand { get; }

        /// <summary>
        /// Gets the command that will decrease the size of thumbnails.
        /// </summary>
        public static RoutedCommand ZoomOutCommand { get; }

        static AssetViewUserControl()
        {
            BeginEditCommand = new RoutedCommand(nameof(BeginEditCommand), typeof(AssetViewUserControl));
            CommandManager.RegisterClassCommandBinding(typeof(AssetViewUserControl), new CommandBinding(BeginEditCommand, BeginEdit, CanBeginEditCommand));
            CommandManager.RegisterClassInputBinding(typeof(AssetViewUserControl), new InputBinding(BeginEditCommand, new KeyGesture(Key.F2)));

            ZoomInCommand = new RoutedCommand(nameof(ZoomInCommand), typeof(AssetViewUserControl));
            var zoomInCommandBinding = new CommandBinding(ZoomInCommand, ZoomIn);
            zoomInCommandBinding.PreviewCanExecute += (s, e) => e.CanExecute = true;
            zoomInCommandBinding.PreviewExecuted += ZoomIn;
            CommandManager.RegisterClassCommandBinding(typeof(AssetViewUserControl), zoomInCommandBinding);

            ZoomOutCommand = new RoutedCommand(nameof(ZoomOutCommand), typeof(AssetViewUserControl));
            var zoomOutCommandBinding = new CommandBinding(ZoomOutCommand, ZoomOut);
            zoomOutCommandBinding.PreviewCanExecute += (s, e) => e.CanExecute = true;
            zoomOutCommandBinding.PreviewExecuted += ZoomOut;
            CommandManager.RegisterClassCommandBinding(typeof(AssetViewUserControl), zoomOutCommandBinding);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetViewUserControl"/> class.
        /// </summary>
        public AssetViewUserControl()
        {

            InitializeComponent();
            Loaded += (s, e) => AddHandler(Row.EditBeginningEvent, (CancelRoutedEventHandler)CanBeginEditEvent);
            Unloaded += (s, e) => RemoveHandler(Row.EditBeginningEvent, (CancelRoutedEventHandler)CanBeginEditEvent);
        }

        /// <summary>
        /// Gets or sets the <see cref="AssetCollectionViewModel"/> to display in this control.
        /// </summary>
        public AssetCollectionViewModel AssetCollection { get => (AssetCollectionViewModel)GetValue(AssetCollectionProperty); set => SetValue(AssetCollectionProperty, value); }

        /// <summary>
        /// Gets or sets the control to use as context menu for assets.
        /// </summary>
        public Control AssetContextMenu { get => (Control)GetValue(AssetContextMenuProperty); set => SetValue(AssetContextMenuProperty, value); }

        /// <summary>
        /// Gets the list of items to display in the primary tool bar. The primary tool bar won't be displayed if this list is empty.
        /// </summary>
        public IList PrimaryToolBarItems { get; } = new NonGenericObservableListWrapper<object>(new ObservableList<object>());

        /// <summary>
        /// Gets or sets whether it is possible to edit assets with this <see cref="AssetViewUserControl"/>.
        /// </summary>
        public bool CanEditAssets { get => (bool)GetValue(CanEditAssetsProperty); set => SetValue(CanEditAssetsProperty, value); }

        /// <summary>
        /// Gets or sets whether it is possible to add assets with this <see cref="AssetViewUserControl"/>.
        /// </summary>
        public bool CanAddAssets { get => (bool)GetValue(CanAddAssetsProperty); set => SetValue(CanAddAssetsProperty, value); }

        /// <summary>
        /// Gets or sets whether it is possible to delete assets with this <see cref="AssetViewUserControl"/>.
        /// </summary>
        public bool CanDeleteAssets { get => (bool)GetValue(CanDeleteAssetsProperty); set => SetValue(CanDeleteAssetsProperty, value); }

        /// <summary>
        /// Gets or sets whether it is possible to select to display asset recursively from selected locations.
        /// </summary>
        public bool CanRecursivelyDisplayAssets { get => (bool)GetValue(CanRecursivelyDisplayAssetsProperty); set => SetValue(CanRecursivelyDisplayAssetsProperty, value); }

        /// <summary>
        /// Gets or sets whether the control should get the focus when its selection changes. The focus is not given if the selection is cleared.
        /// </summary>
        public bool GiveFocusOnSelectionChange { get => (bool)GetValue(GiveFocusOnSelectionChangeProperty); set => SetValue(GiveFocusOnSelectionChangeProperty, value); }

        /// <summary>
        /// Gets or sets the zoom increment value.
        /// </summary>
        public double ThumbnailZoomIncrement { get => (double)GetValue(ThumbnailZoomIncrementProperty); set => SetValue(ThumbnailZoomIncrementProperty, value); }

        /// <summary>
        /// Gets or sets the minimum size of thumbnails.
        /// </summary>
        public double ThumbnailMinimumSize { get => (double)GetValue(ThumbnailMinimumSizeProperty); set => SetValue(ThumbnailMinimumSizeProperty, value); }

        /// <summary>
        /// Gets or sets the maximum size of thumbnails.
        /// </summary>
        public double ThumbnailMaximumSize { get => (double)GetValue(ThumbnailMaximumSizeProperty); set => SetValue(ThumbnailMaximumSizeProperty, value); }

        /// <summary>
        /// Gets or sets the size of thumbnails in tile view.
        /// </summary>
        public double TileThumbnailSize { get => (double)GetValue(TileThumbnailSizeProperty); set => SetValue(TileThumbnailSizeProperty, value); }

        /// <summary>
        /// Gets or sets the size of thumbnails in grid view.
        /// </summary>
        public double GridThumbnailSize { get => (double)GetValue(GridThumbnailSizeProperty); set => SetValue(GridThumbnailSizeProperty, value); }

        /// <summary>
        /// Gets or sets the command to execute when user double-clicks an asset.
        /// </summary>
        public ICommand AssetDoubleClick { get => (ICommand)GetValue(AssetDoubleClickProperty); set => SetValue(AssetDoubleClickProperty, value); }

        /// <summary>
        /// Begins edition of the currently selected content.
        /// </summary>
        public void BeginEdit()
        {
            if (!CanEditAssets || AssetCollection == null)
                return;

            var selectedAsset = AssetCollection.SelectedContent.LastOrDefault();
            if (selectedAsset == null)
                return;

            var listBox = AssetViewPresenter.FindVisualChildOfType<EditableContentListBox>();
            listBox?.BeginEdit();

            var gridView = AssetViewPresenter.FindVisualChildOfType<DataGridControl>();
            gridView?.BeginEdit();
        }

        private void ZoomIn()
        {
            var listBox = AssetViewPresenter.FindVisualChildOfType<EditableContentListBox>();
            if (listBox != null)
            {
                TileThumbnailSize += ThumbnailZoomIncrement;
                if (TileThumbnailSize >= ThumbnailMaximumSize)
                {
                    TileThumbnailSize = ThumbnailMaximumSize;
                }
            }

            var gridView = AssetViewPresenter.FindVisualChildOfType<DataGridControl>();
            if (gridView != null)
            {
                GridThumbnailSize += ThumbnailZoomIncrement;
                if (GridThumbnailSize >= ThumbnailMaximumSize)
                {
                    GridThumbnailSize = ThumbnailMaximumSize;
                }
            }
        }

        private void ZoomOut()
        {
            var listBox = AssetViewPresenter.FindVisualChildOfType<EditableContentListBox>();
            if (listBox != null)
            {
                TileThumbnailSize -= ThumbnailZoomIncrement;
                if (TileThumbnailSize <= ThumbnailMinimumSize)
                {
                    TileThumbnailSize = ThumbnailMinimumSize;
                }
            }

            var gridView = AssetViewPresenter.FindVisualChildOfType<DataGridControl>();
            if (gridView != null)
            {
                GridThumbnailSize -= ThumbnailZoomIncrement;
                if (GridThumbnailSize <= ThumbnailMinimumSize)
                {
                    GridThumbnailSize = ThumbnailMinimumSize;
                }
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    ZoomIn();
                    e.Handled = true;
                }
                if (e.Delta < 0)
                {
                    ZoomOut();
                    e.Handled = true;
                }
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!IsFocused && !IsKeyboardFocusWithin)
                Focus();
        }

        private bool CanBeginEdit()
        {
            if (!CanEditAssets || AssetCollection == null)
                return false;

            // Special case to under edition state restoration in the DataGrid
            if (AssetCollection.SelectedContent.Count == 0)
                return true;

            if (AssetCollection.SelectedContent.Count != 1)
                return false;

            // HACK: might be a better way to check that
            var asset = AssetCollection.SelectedContent.Last() as AssetViewModel;
            return !asset?.IsLocked ?? true;
        }

        private static void CanBeginEditCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            var control = (AssetViewUserControl)sender;
            e.CanExecute = control.CanBeginEdit();
        }

        private static void CanBeginEditEvent(object sender, CancelRoutedEventArgs e)
        {
            var control = (AssetViewUserControl)sender;
            e.Cancel = !control.CanBeginEdit();
        }

        private static void BeginEdit(object sender, ExecutedRoutedEventArgs e)
        {
            var assetView = (AssetViewUserControl)sender;
            assetView.BeginEdit();
        }

        private static void ZoomIn(object sender, ExecutedRoutedEventArgs e)
        {
            var assetView = (AssetViewUserControl)sender;
            assetView.ZoomIn();
        }

        private static void ZoomOut(object sender, ExecutedRoutedEventArgs e)
        {
            var assetView = (AssetViewUserControl)sender;
            assetView.ZoomOut();
        }

        /// <summary>
        /// Raised when the <see cref="AssetCollection"/> dependency property changes.
        /// </summary>
        /// <param name="d">The event sender.</param>
        /// <param name="e">The event argument.</param>
        private static void AssetCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var assetViewControl = (AssetViewUserControl)d;
            assetViewControl.RootContainer.DataContext = e.NewValue;
        }

        /// <summary>
        /// Raised when the <see cref="AssetContextMenu"/> dependency property changes.
        /// </summary>
        /// <param name="d">The event sender.</param>
        /// <param name="e">The event argument.</param>
        private static void OnAssetContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                NameScope.SetNameScope((DependencyObject)e.NewValue, NameScope.GetNameScope(d));
            }
        }
    }
}
