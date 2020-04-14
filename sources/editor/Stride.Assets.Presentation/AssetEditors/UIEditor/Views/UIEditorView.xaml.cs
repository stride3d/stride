// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Views
{
    /// <summary>
    /// Interaction logic for UIEditorView.xaml
    /// </summary>
    public abstract partial class UIEditorView : IEditorView
    {
        private readonly TaskCompletionSource<bool> editorInitializationNotifier = new TaskCompletionSource<bool>();

        protected UIEditorView()
        {
            InitializeComponent();
            // Ensure we can give the focus to the editor
            Focusable = true;
        }

        /// <inheritdoc/>
        public Task EditorInitialization => editorInitializationNotifier.Task;

        /// <inheritdoc/>
        public async Task<IAssetEditorViewModel> InitializeEditor(AssetViewModel asset)
        {
            var editor = CreateEditorViewModel(asset);

            // Don't set the actual Editor property until the editor object is fully initialized - we don't want data bindings to access uninitialized properties
            var result = await editor.Initialize();

            SceneView.Content = editor.Controller.EditorHost;
            SceneView.InvalidateVisual();

            editorInitializationNotifier.SetResult(result);
            if (result)
                return editor;

            editor.Destroy();
            return null;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            // We give the focus to the editor so shortcuts will work
            if (!IsKeyboardFocusWithin && !(e.OriginalSource is HwndHost))
            {
                Keyboard.Focus(this);
            }
        }

        [NotNull]
        protected abstract UIEditorBaseViewModel CreateEditorViewModel([NotNull] AssetViewModel asset);

        private void EditorPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent arrow keys to be involved in arrow navigation. There seems to be no other way - http://bit.ly/1BzM0AC
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                    e.Handled = true;
                    break;
            }
        }
    }
}
