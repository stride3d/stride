// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using RoslynPad.Roslyn.Diagnostics;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.Annotations;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    /// <summary>
    /// Interaction logic for ScriptEditorView.xaml
    /// </summary>
    [AssetEditorView<ScriptEditorViewModel>]
    public partial class ScriptEditorView : IEditorView
    {
        private readonly TaskCompletionSource editorInitializationNotifier = new();
        private ScriptEditorViewModel editor;

        public ScriptEditorView()
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
            this.editor = (ScriptEditorViewModel)editor;
            if (!await editor.Initialize())
            {
                editor.Destroy();
                editorInitializationNotifier.SetResult();
                return false;
            }

            // Ctrl + mouse wheel => zoom/unzoom
            CodeEditor.PreviewMouseWheel += OnEditorMouseWheel;

            this.editor.DocumentClosed += Editor_DocumentClosed;
            this.editor.ProcessDiagnostics += Editor_ProcessDiagnostics;

            // Bind SourceTextContainer to UI
            CodeEditor.BindSourceTextContainer(this.editor.Workspace, this.editor.SourceTextContainer, this.editor.DocumentId);

            editorInitializationNotifier.SetResult();
            return true;
        }

        private void OnEditorMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                editor.Code.EditorFontSize += e.Delta > 0 ? 1 : -1;
                e.Handled = true;
            }
        }

        private void Editor_ProcessDiagnostics(object sender, DiagnosticsUpdatedArgs e)
        {
            CodeEditor.ProcessDiagnostics(e);
        }

        private void Editor_DocumentClosed(object sender, EventArgs e)
        {
            // Events
            CodeEditor.PreviewMouseWheel -= OnEditorMouseWheel;
            editor.DocumentClosed -= Editor_DocumentClosed;
            editor.ProcessDiagnostics -= Editor_ProcessDiagnostics;

            CodeEditor.Unbind();
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
    }
}
