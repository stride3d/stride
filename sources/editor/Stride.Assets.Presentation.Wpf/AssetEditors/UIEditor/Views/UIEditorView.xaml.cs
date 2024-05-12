// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Views
{
    /// <summary>
    /// Interaction logic for UIEditorView.xaml
    /// </summary>
    public abstract partial class UIEditorView : IEditorView
    {
        private readonly TaskCompletionSource editorInitializationNotifier = new();

        protected UIEditorView()
        {
            InitializeComponent();
            // Ensure we can give the focus to the editor
            Focusable = true;
        }

        /// <inheritdoc/>
        public Task EditorInitialization => editorInitializationNotifier.Task;

        /// <inheritdoc/>
        public async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
        {
            var uiEditor = (UIEditorBaseViewModel)editor;

            // Don't set the actual Editor property until the editor object is fully initialized - we don't want data bindings to access uninitialized properties
            if (!await editor.Initialize())
            {
                editor.Destroy();
                editorInitializationNotifier.SetResult();
                return false;
            }

            SceneView.Content = uiEditor.Controller.EditorHost;
            SceneView.InvalidateVisual();

            editorInitializationNotifier.SetResult();
            return true;
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
