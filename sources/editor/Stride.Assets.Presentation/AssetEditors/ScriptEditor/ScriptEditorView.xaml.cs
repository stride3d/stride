// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using RoslynPad.Roslyn.Diagnostics;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    /// <summary>
    /// Interaction logic for ScriptEditorView.xaml
    /// </summary>
    public partial class ScriptEditorView : IEditorView
    {
        private readonly TaskCompletionSource<bool> editorInitializationNotifier = new TaskCompletionSource<bool>();
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
        public async Task<IAssetEditorViewModel> InitializeEditor(AssetViewModel asset)
        {
            var script = (ScriptSourceFileAssetViewModel)asset;
            
            editor = new ScriptEditorViewModel(script, script.TextContainer);

            // Ctrl + mouse wheel => zoom/unzoom
            CodeEditor.PreviewMouseWheel += OnEditorMouseWheel;

            editor.DocumentClosed += Editor_DocumentClosed;
            editor.ProcessDiagnostics += Editor_ProcessDiagnostics;

            // Don't set the actual Editor property until the editor object is fully initialized - we don't want data bindings to access uninitialized properties
            var result = await editor.Initialize();

            // Bind SourceTextContainer to UI
            CodeEditor.BindSourceTextContainer(editor.Workspace, script.TextContainer, editor.DocumentId);

            editorInitializationNotifier.SetResult(result);
            if (result)
                return editor;

            editor.Destroy();
            return null;
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
