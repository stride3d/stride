// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Graph.Behaviors;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Scripts;
using Block = Stride.Assets.Scripts.Block;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    /// <summary>
    /// Interaction logic for GraphicsCompositorEditorView.xaml
    /// </summary>
    public partial class VisualScriptEditorView : IEditorView, IVisualScriptViewModelService
    {
        private readonly TaskCompletionSource<bool> editorInitializationNotifier = new TaskCompletionSource<bool>();

        private DropVariableContextMenuChoice? dropVariableContextMenuChoice;

        public VisualScriptEditorView()
        {
            InitializeComponent();

            ZoomControl.AddHandler(UIElement.MouseUpEvent, new MouseButtonEventHandler(ZoomControl_MouseUp), true);
        }

        /// <inheritdoc/>
        public Task EditorInitialization => editorInitializationNotifier.Task;

        /// <inheritdoc/>
        public async Task<IAssetEditorViewModel> InitializeEditor(AssetViewModel asset)
        {
            var visualScript = (VisualScriptViewModel)asset;

            var editor = new VisualScriptEditorViewModel(this, visualScript);

            // Don't set the actual Editor property until the editor object is fully initialized - we don't want data bindings to access uninitialized properties
            var result = await editor.Initialize();
            editorInitializationNotifier.SetResult(result);
            if (result)
                return editor;

            editor.Destroy();
            return null;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (SymbolSearchPopup.IsKeyboardFocusWithin)
            {
                return;
            }

            // We give the focus to the editor so shortcuts will work
            if (!IsKeyboardFocusWithin && !(e.OriginalSource is HwndHost))
            {
                Keyboard.Focus(this);
            }
        }

        async Task<Block> IVisualScriptViewModelService.TransformVariableIntoBlock(Symbol symbol)
        {
            var property = symbol as Property;
            if (property == null)
                return null;

            // Open context menu asking if we want a getter or setter
            var dropVariableContextMenu = (ContextMenu)Resources["DropVariableContextMenu"];

            var menuClosedTCS = new TaskCompletionSource<bool>();
            RoutedEventHandler closedEventHandler = (sender, e) =>
            {
                menuClosedTCS.TrySetResult(true);
            };

            // Capture mouse position right now to position block later
            var mousePosition = GraphContextMenuOpenedBehavior.GetMousePosition(ZoomControl, Area);

            // Setup events
            dropVariableContextMenuChoice = null;
            dropVariableContextMenu.Closed += closedEventHandler;

            // Display context menu and waits for it to close
            dropVariableContextMenu.IsOpen = true;
            await menuClosedTCS.Task;

            dropVariableContextMenu.Closed -= closedEventHandler;

            // TODO: check if view model is still part of the visual script?
            // Check user choice in context menu (dropVariableContextMenuChoice acts as a kind of local view-model)
            Block result;
            switch (dropVariableContextMenuChoice)
            {
                case DropVariableContextMenuChoice.Get:
                    result = new VariableGet { Name = property.Name };
                    break;
                case DropVariableContextMenuChoice.Set:
                    result = new VariableSet { Name = property.Name };
                    break;
                default:
                    return null;
            }

            // Adjust result with variable and position with mouse position when context menu was opened
            result.Position = new Int2((int)Math.Round(mousePosition.X), (int)Math.Round(mousePosition.Y));

            return result;
        }

        private void DropVariableContextMenuGetClicked(object sender, RoutedEventArgs e)
        {
            dropVariableContextMenuChoice = DropVariableContextMenuChoice.Get;
        }

        private void DropVariableContextMenuSetClicked(object sender, RoutedEventArgs e)
        {
            dropVariableContextMenuChoice = DropVariableContextMenuChoice.Set;
        }

        private void ZoomControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Mark event has unhandled so that context menu properly open (considered Handled when ZoomControl.Click is fired)
            e.Handled = false;
        }

        enum DropVariableContextMenuChoice
        {
            Get,
            Set,
        }
    }
}
