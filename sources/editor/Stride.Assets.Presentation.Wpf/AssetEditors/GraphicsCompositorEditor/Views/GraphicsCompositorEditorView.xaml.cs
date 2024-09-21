// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using System.Windows.Input;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.Views
{
    /// <summary>
    /// Interaction logic for GraphicsCompositorEditorView.xaml
    /// </summary>
    [AssetEditorView<GraphicsCompositorEditorViewModel>]
    public partial class GraphicsCompositorEditorView : IEditorView
    {
        private readonly TaskCompletionSource editorInitializationNotifier = new();

        public GraphicsCompositorEditorView()
        {
            InitializeComponent();

            ZoomControl.AddHandler(MouseUpEvent, new MouseButtonEventHandler(ZoomControl_MouseUp), true);
        }

        /// <inheritdoc/>
        public Task EditorInitialization => editorInitializationNotifier.Task;

        public GraphicsCompositorGraph Graph { get; set; }

        /// <inheritdoc/>
        public async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
        {
            var graphicsEditor = (GraphicsCompositorEditorViewModel)editor;

            Graph = new GraphicsCompositorGraph(graphicsEditor.Blocks, graphicsEditor.SelectedSharedRenderers, graphicsEditor.SelectedRendererLinks);

            if (!await graphicsEditor.Initialize())
            {
                editor.Destroy();
                editorInitializationNotifier.SetResult();
                return false;
            }

            editorInitializationNotifier.SetResult();
            return true;
        }

        private void ZoomControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Focus this element so that undo/redo and delete are properly handled after having any other window selected
            // and clicking on a vertex inside the graph (or any other non-empty space)
            Focus();

            // Mark event has unhandled so that context menu properly open (considered Handled when ZoomControl.Click is fired)
            e.Handled = false;
        }
    }
}
