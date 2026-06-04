// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.BraceMatching;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.QuickInfo;
using Stride.Core.Presentation.Themes;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    /// <summary>
    /// A <see cref="CodeTextEditor"/> with intellisense connected to our <see cref="RoslynWorkspace"/>.
    /// </summary>
    public class SimpleCodeTextEditor : CodeTextEditor
    {
        private RoslynWorkspace workspace;
        private DocumentId documentId;
        private AvalonEditTextContainer sourceTextContainer;

        private RoslynHighlightingColorizer syntaxHighlightingColorizer;
        private TextMarkerService textMarkerService;
        private ContextActionsRenderer contextActionsRenderer;
        private RoslynContextActionProvider contextActionProvider;
        private IQuickInfoProvider quickInfoProvider;

        private BraceMatcherHighlightRenderer braceMatcherHighlighter;
        private IBraceMatchingService braceMatchingService;
        private CancellationTokenSource braceMatchingCts;

        public static readonly StyledProperty<ImageSource> ContextActionsIconProperty = CommonProperty.Register<SimpleCodeTextEditor, ImageSource>(
            nameof(ContextActionsIcon), onChanged: OnContextActionsIconChanged);

        private static void OnContextActionsIconChanged(SimpleCodeTextEditor editor, CommonPropertyChangedArgs<ImageSource> args)
        {
            if (editor.contextActionsRenderer != null)
            {
                editor.contextActionsRenderer.IconImage = args.NewValue;
            }
        }

        public ImageSource ContextActionsIcon
        {
            get => this.GetValue<ImageSource>(ContextActionsIconProperty);
            set => this.SetValue(ContextActionsIconProperty, value);
        }

        /// <summary>
        /// Connects the text editor to a roslyn document and a <see cref="AvalonEditTextContainer"/>. This will setup syntax highlighting, intellisense, etc...
        /// </summary>
        public void BindSourceTextContainer(RoslynWorkspace workspace, AvalonEditTextContainer sourceTextContainer, DocumentId documentId)
        {
            this.workspace = workspace;
            this.sourceTextContainer = sourceTextContainer;
            this.documentId = documentId;

            Document = sourceTextContainer.Document;

            // Update Caret position on text update
            sourceTextContainer.Editor = this;

            // Setup text markers (underline diagnostics)
            textMarkerService = new TextMarkerService(this);
            TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
            TextArea.TextView.LineTransformers.Add(textMarkerService);
            TextArea.Caret.PositionChanged += CaretOnPositionChanged;

            // Syntax highlighting
            var classificationHighlightColors = ThemeController.CurrentTheme.GetThemeBase() == IconThemeSelector.ThemeBase.Dark ? new ClassificationHighlightColorsDark() : new ClassificationHighlightColors();
            syntaxHighlightingColorizer = new RoslynHighlightingColorizer(documentId, workspace.Host, classificationHighlightColors);
            TextArea.TextView.LineTransformers.Insert(0, syntaxHighlightingColorizer);

            // Context Actions
            contextActionsRenderer = new ContextActionsRenderer(this, textMarkerService) { IconImage = ContextActionsIcon };
            contextActionProvider = new RoslynContextActionProvider(documentId, workspace.Host);
            contextActionsRenderer.Providers.Add(contextActionProvider);

            // Completion provider
            CompletionProvider = new RoslynCodeEditorCompletionProvider(documentId, workspace.Host);
            // TODO: Warmup() is internal

            workspace.Host.GetWorkspaceService<IDiagnosticsUpdater>(documentId).DiagnosticsChanged += ProcessDiagnostics;

            // Quick info
            quickInfoProvider = workspace.Host.GetService<IQuickInfoProvider>();

            // Brace matching
            braceMatcherHighlighter = new BraceMatcherHighlightRenderer(TextArea.TextView, classificationHighlightColors);
            braceMatchingService = workspace.Host.GetService<IBraceMatchingService>();
            AsyncToolTipRequest = ProcessAsyncToolTipRequest;
        }

        /// <summary>
        /// Disconnects the text editor from a roslyn document.
        /// </summary>
        public void Unbind()
        {
            // Caret/brace matching
            TextArea.Caret.PositionChanged -= CaretOnPositionChanged;

            // Syntax highlighting
            TextArea.TextView.LineTransformers.Remove(syntaxHighlightingColorizer);

            // Text markers
            TextArea.TextView.BackgroundRenderers.Remove(textMarkerService);
            TextArea.TextView.LineTransformers.Remove(textMarkerService);
            textMarkerService = null;

            // Tooltips
            AsyncToolTipRequest = null;

            // Context Actions
            contextActionsRenderer.Providers.Remove(contextActionProvider);
            contextActionsRenderer = null;
            contextActionProvider = null;

            // Completion provider
            CompletionProvider = null;

            workspace.Host.GetWorkspaceService<IDiagnosticsUpdater>(documentId).DiagnosticsChanged -= ProcessDiagnostics;

            // No need to update caret position anymore
            sourceTextContainer.Editor = null;

            workspace = null;
            sourceTextContainer = null;
            documentId = null;
        }

        public void ProcessDiagnostics(DiagnosticsChangedArgs args)
        {
            if (args.DocumentId != documentId)
                return;

            if (this.Dispatcher.CheckAccess())
            {
                ProcessDiagnosticsOnUiThread(args);
                return;
            }

            this.Dispatcher.InvokeAsync(() => ProcessDiagnosticsOnUiThread(args));
        }

        private void ProcessDiagnosticsOnUiThread(DiagnosticsChangedArgs args)
        {
            var removedDiagnosticIds = new HashSet<string>(args.RemovedDiagnostics.Select(x => x.Id));
            textMarkerService.RemoveAll(d => d.Tag is DiagnosticData diagnosticData && args.RemovedDiagnostics.Contains(diagnosticData));

            foreach (var diagnosticData in args.AddedDiagnostics)
            {
                if (diagnosticData.Severity == DiagnosticSeverity.Hidden || diagnosticData.IsSuppressed)
                {
                    continue;
                }

                var span = diagnosticData.GetTextSpan(sourceTextContainer.CurrentText);
                if (span == null)
                {
                    continue;
                }

                var marker = textMarkerService.TryCreate(span.Value.Start, span.Value.Length);
                if (marker != null)
                {
                    marker.Tag = diagnosticData;
                    marker.MarkerColor = GetDiagnosticsColor(diagnosticData);
                    marker.ToolTip = diagnosticData.Message;
                }
            }
        }

        private async void CaretOnPositionChanged(object sender, EventArgs eventArgs)
        {
            braceMatchingCts?.Cancel();

            if (braceMatchingService == null) return;

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            braceMatchingCts = cts;

            var document = workspace.Host.GetDocument(documentId);
            try
            {
                var text = await document.GetTextAsync(token).ConfigureAwait(false);
                var caretOffset = CaretOffset;
                if (caretOffset <= text.Length)
                {
                    var result = await braceMatchingService.GetAllMatchingBracesAsync(document, caretOffset, token).ConfigureAwait(true);
                    braceMatcherHighlighter.SetHighlight(result.leftOfPosition, result.rightOfPosition);
                }
            }
            catch (OperationCanceledException) { }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                switch (e.Key)
                {
                    case Key.OemCloseBrackets:
                        TryJumpToBrace();
                        break;
                }
            }
        }

        private void TryJumpToBrace()
        {
            if (braceMatcherHighlighter == null) return;

            var caret = CaretOffset;

            if (TryJumpToPosition(braceMatcherHighlighter.LeftOfPosition, caret) ||
                TryJumpToPosition(braceMatcherHighlighter.RightOfPosition, caret))
            {
                ScrollToLine(TextArea.Caret.Line);
            }
        }

        private bool TryJumpToPosition(BraceMatchingResult? position, int caret)
        {
            if (position != null)
            {
                if (position.Value.LeftSpan.Contains(caret))
                {
                    CaretOffset = position.Value.RightSpan.End;
                    return true;
                }

                if (position.Value.RightSpan.Contains(caret) || position.Value.RightSpan.End == caret)
                {
                    CaretOffset = position.Value.LeftSpan.Start;
                    return true;
                }
            }

            return false;
        }

        private static Color GetDiagnosticsColor(DiagnosticData diagnosticData)
        {
            switch (diagnosticData.Severity)
            {
                case DiagnosticSeverity.Info:
                    return Colors.LimeGreen;
                case DiagnosticSeverity.Warning:
                    return Colors.DodgerBlue;
                case DiagnosticSeverity.Error:
                    return Colors.Red;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task ProcessAsyncToolTipRequest(ToolTipRequestEventArgs arg)
        {
            // TODO: consider invoking this with a delay, then showing the tool-tip without one
            var document = workspace.GetDocument(documentId);
            var info = await quickInfoProvider.GetItemAsync(document, arg.Position, CancellationToken.None).ConfigureAwait(true);
            if (info != null)
            {
                arg.SetToolTip(info.Create());
            }
        }
    }
}
