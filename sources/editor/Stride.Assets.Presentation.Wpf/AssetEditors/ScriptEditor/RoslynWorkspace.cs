// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Copyright 2016 Eli Arbel
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    /// <summary>
    /// A workspace specific to Stride script editor.
    /// </summary>
    public class RoslynWorkspace : RoslynPad.Roslyn.RoslynWorkspace // workaround: need to inherit from this due to RoslynPad DocumentTrackingServiceFactory casting workspace to this type... we'll need to review/reconsider RoslynPad seriously
    {
        private readonly RoslynHost host;
        private readonly ConcurrentDictionary<DocumentId, Action<DiagnosticsChangedArgs>> diagnosticsChangedNotifiers = new ConcurrentDictionary<DocumentId, Action<DiagnosticsChangedArgs>>();
        private readonly ConcurrentDictionary<DocumentId, TrackDocumentCallback> trackDocumentCallbacks = new ConcurrentDictionary<DocumentId, TrackDocumentCallback>();

        public RoslynWorkspace(RoslynHost host) : base(host.HostServices, WorkspaceKind.Host)
        {
            this.host = host;
            Services.GetRequiredService<IDiagnosticsUpdater>().DiagnosticsChanged += OnDiagnosticsChanged;
        }

        /// <summary>
        /// The roslyn host services.
        /// </summary>
        public RoslynHost Host => host;

        /// <inheritdoc/>
        public override bool CanOpenDocuments => true;

        /// <summary>
        /// Event fired when a document is closed.
        /// </summary>
        public event Action<DocumentId> HostDocumentClosed;

        /// <inheritdoc/>
        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            return feature switch
            {
                ApplyChangesKind.ChangeDocument
                    or ApplyChangesKind.ChangeDocumentInfo
                    or ApplyChangesKind.AddMetadataReference
                    or ApplyChangesKind.RemoveMetadataReference
                    or ApplyChangesKind.AddAnalyzerReference
                    or ApplyChangesKind.RemoveAnalyzerReference => true,
                _ => false,
            };
        }

        /// <summary>
        /// Adds a new project or updates it if already added.
        /// </summary>
        public void AddOrUpdateProject(Project project)
        {
            // Preserve project id if it project with same path already loaded
            var oldProject = this.CurrentSolution.Projects.FirstOrDefault(x => x.FilePath == project.FilePath);
            var projectInfo = CreateProjectInfo(oldProject, project);

            if (!this.CurrentSolution.ContainsProject(projectInfo.Id))
                this.OnProjectAdded(projectInfo);
            else
                this.OnProjectReloaded(projectInfo);
        }

        /// <inheritdoc/>
        protected override Project AdjustReloadedProject(Project oldProject, Project reloadedProject)
        {
            var result = base.AdjustReloadedProject(oldProject, reloadedProject);
            var reloadedSolution = result.Solution;

            // Keep each Script document open
            foreach (var docId in this.GetOpenDocumentIds(oldProject.Id))
            {
                if (!reloadedProject.ContainsDocument(docId))
                {
                    var oldDocument = oldProject.GetDocument(docId);
                    if (oldDocument.SourceCodeKind == SourceCodeKind.Script)
                    {
                        SourceText text;
                        oldDocument.TryGetText(out text);
                        reloadedSolution = reloadedSolution.AddDocument(oldDocument.Id, oldDocument.Name, text)
                            .WithDocumentSourceCodeKind(docId, SourceCodeKind.Script);
                    }
                }
            }

            return reloadedSolution.GetProject(oldProject.Id);
        }

        /// <summary>
        /// Removes a project. No-op when absent (the caller's project reference can be newer than this
        /// workspace, which syncs through batched change events).
        /// </summary>
        public void RemoveProject(ProjectId projectId)
        {
            if (CurrentSolution.ContainsProject(projectId))
                this.OnProjectRemoved(projectId);
        }

        /// <summary>
        /// Called when a document is reloaded.
        /// </summary>
        protected internal async Task HostDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader)
        {
            var document = GetDocument(documentId);
            if (document == null)
                return;

            // No longer async void: the awaiting caller observes and logs any failure (e.g. the file
            // vanishing between the change notification and the reload), so it no longer crashes the process.
            object reloadState = null;
            TrackDocumentCallback trackDocumentCallback;
            trackDocumentCallbacks.TryGetValue(documentId, out trackDocumentCallback);

            trackDocumentCallback.ExternalChangesDetected?.Invoke();
            if (trackDocumentCallback.AllowReload?.Invoke(document, ref reloadState) ?? true)
            {
                OnDocumentTextLoaderChanged(documentId, loader);

                // Get updated document
                document = GetDocument(documentId);
                if (document != null)
                    RaiseTextUpdated(documentId, await document.GetTextAsync(), true);
            }
        }

        /// <summary>
        /// Tracks document reloads and changes.
        /// </summary>
        public void TrackDocument(DocumentId documentId, AllowReloadDelegate allowReload, Action<SourceText, bool> textUpdated, Action externalChangesDetected)
        {
            trackDocumentCallbacks.TryAdd(documentId, new TrackDocumentCallback
            {
                AllowReload = allowReload,
                TextUpdated = textUpdated,
                ExternalChangesDetected = externalChangesDetected,
            });
        }

        /// <summary>
        /// Untracks document reloads and changes.
        /// </summary>
        /// <param name="documentId"></param>
        public void UntrackDocument(DocumentId documentId)
        {
            TrackDocumentCallback trackDocumentCallback;
            trackDocumentCallbacks.TryRemove(documentId, out trackDocumentCallback);
        }

        /// <summary>
        /// Opens an existing document.
        /// </summary>
        public DocumentId OpenDocument(SourceTextContainer sourceTextContainer, DocumentId documentId, Action<DiagnosticsChangedArgs> onDiagnosticsChanged)
        {
            if (documentId != null && CurrentSolution.ContainsDocument(documentId) && !IsDocumentOpen(documentId))
            {
                OnDocumentOpened(documentId, sourceTextContainer);
                OnDocumentContextUpdated(documentId);

                if (onDiagnosticsChanged != null)
                    diagnosticsChangedNotifiers.TryAdd(documentId, onDiagnosticsChanged);

                return documentId;
            }

            return null;
        }

        /// <summary>
        /// Gets an existing document.
        /// </summary>
        public Document GetDocument(DocumentId documentId)
        {
            return CurrentSolution.GetDocument(documentId);
        }

        /// <summary>
        /// Removes a document.
        /// </summary>
        public void RemoveDocument(DocumentId documentId)
        {
            // Close first so an open editor is notified (HostDocumentClosed) and Roslyn drops its open state.
            if (IsDocumentOpen(documentId))
                CloseDocument(documentId);
            OnDocumentRemoved(documentId);
        }

        /// <inheritdoc/>
        protected override void OnProjectReloaded(ProjectInfo reloadedProjectInfo)
        {
            // Close any document that don't exist anymore
            var documentIds = new HashSet<DocumentId>(reloadedProjectInfo.Documents.Select(x => x.Id));
            var documentsToRemove = new List<DocumentId>();
            foreach (var docId in this.GetOpenDocumentIds(reloadedProjectInfo.Id))
            {
                if (!documentIds.Contains(docId))
                {
                    documentsToRemove.Add(docId);
                }
            }

            foreach (var docId in documentsToRemove)
            {
                CloseDocument(docId);
            }

            base.OnProjectReloaded(reloadedProjectInfo);
        }

        /// <inheritdoc/>
        protected override void OnDocumentClosing(DocumentId documentId)
        {
            base.OnDocumentClosing(documentId);
            HostDocumentClosed?.Invoke(documentId);
        }

        /// <inheritdoc/>
        public override void CloseDocument(DocumentId documentId)
        {
            if (IsDocumentOpen(documentId))
            {
                // Unregister callbacks
                diagnosticsChangedNotifiers.TryRemove(documentId, out var diagnosticChangedNotifier);

                var currentDoc = this.CurrentSolution.GetDocument(documentId);
                if (currentDoc == null)
                    return;

                // Open documents already have materialized text, so this doesn't block the UI thread.
                TextLoader loader;
                if (currentDoc.TryGetText(out var text) && currentDoc.TryGetTextVersion(out var version))
                    loader = TextLoader.From(TextAndVersion.Create(text, version, currentDoc.FilePath));
                else if (!string.IsNullOrEmpty(currentDoc.FilePath) && File.Exists(currentDoc.FilePath))
                    loader = new FileTextLoader(currentDoc.FilePath, Encoding.UTF8);
                else
                    loader = TextLoader.From(TextAndVersion.Create(SourceText.From(string.Empty), VersionStamp.Create(), currentDoc.FilePath));

                OnDocumentClosed(documentId, loader);
            }
        }

        /// <inheritdoc/>
        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
        {
            RaiseTextUpdated(documentId, newText, false);
        }

        private void RaiseTextUpdated(DocumentId documentId, SourceText newText, bool external)
        {
            TrackDocumentCallback trackDocumentCallback;
            if (trackDocumentCallbacks.TryGetValue(documentId, out trackDocumentCallback))
            {
                trackDocumentCallback.TextUpdated?.Invoke(newText, external);
            }
        }

        /// <summary>
        /// Adds a new document.
        /// </summary>
        public DocumentId AddDocument(ProjectId projectId, string filePath, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular, TextLoader loader = null)
        {
            CheckProjectIsInCurrentSolution(projectId);

            var id = DocumentId.CreateNewId(projectId, filePath);
            this.OnDocumentAdded(DocumentInfo.Create(id, Path.GetFileName(filePath), sourceCodeKind: sourceCodeKind, loader: loader, filePath: filePath));
            return id;
        }

        /// <summary>
        /// Updates document text.
        /// </summary>
        public void UpdateDocument(DocumentId documentId, SourceText sourceText)
        {
            OnDocumentTextChanged(documentId, sourceText, PreservationMode.PreserveIdentity);
        }

        public void UpdateDocument(DocumentId documentId, SyntaxNode syntaxNode)
        {
            // TODO: This is not protected by the _serializationLock
            this.SetCurrentSolution(CurrentSolution.WithDocumentSyntaxRoot(documentId, syntaxNode));
            OnDocumentTextChanged(GetDocument(documentId));
        }

        public void UpdateDocument(DocumentId documentId, TextLoader textLoader)
        {
            OnDocumentTextLoaderChanged(documentId, textLoader);
        }

        /// <summary>
        /// Updates a document file path, when a file is moved on the disk.
        /// </summary>
        public void UpdateFilePath(DocumentId documentId, string filePath)
        {
            var document = GetDocument(documentId);
            if (document == null)
                return;

            // Nothing to do
            if (document.FilePath == filePath)
                return;

            var newDocument = CreateDocumentInfoWithText(documentId.ProjectId, document, null).WithName(Path.GetFileName(filePath)).WithFilePath(filePath);
            OnDocumentReloaded(newDocument);
        }

        /// <summary>
        /// Notify listener that diagnostics changed.
        /// </summary>
        private void OnDiagnosticsChanged(DiagnosticsChangedArgs diagnosticsChangedArgs)
        {
            var documentId = diagnosticsChangedArgs?.DocumentId;
            if (documentId == null) return;

            if (diagnosticsChangedNotifiers.TryGetValue(documentId, out var notifier))
            {
                notifier(diagnosticsChangedArgs);
            }
        }

        /// <summary>
        /// Creates <see cref="ProjectInfo"/> from a specific <see cref="Project"/>, while trying to preserve project and document ids.
        /// </summary>
        private ProjectInfo CreateProjectInfo(Project oldProject, Project project)
        {
            // Build file => docId mapping (try to preserve document id)
            Dictionary<string, ImmutableArray<DocumentId>> fileMapping = null;
            if (oldProject != null)
            {
                fileMapping = new Dictionary<string, ImmutableArray<DocumentId>>();
                foreach (var document in oldProject.Documents.Concat(oldProject.AdditionalDocuments))
                {
                    var filePath = document.FilePath;

                    if (string.IsNullOrEmpty(filePath))
                    {
                        continue;
                    }

                    ImmutableArray<DocumentId> documentIdsWithPath;
                    fileMapping[filePath] = fileMapping.TryGetValue(filePath, out documentIdsWithPath)
                        ? documentIdsWithPath.Add(document.Id)
                        : ImmutableArray.Create(document.Id);
                }
            }

            var projectId = oldProject?.Id ?? project.Id;

            return ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                project.Name,
                project.AssemblyName,
                project.Language,
                project.FilePath,
                project.OutputFilePath,
                project.CompilationOptions,
                project.ParseOptions,
                project.Documents.Select(d => CreateDocumentInfoWithText(projectId, d, fileMapping)),
                project.ProjectReferences,
                project.MetadataReferences,
                project.AnalyzerReferences,
                project.AdditionalDocuments.Select(d => CreateDocumentInfoWithText(projectId, d, fileMapping)));
        }

        /// <summary>
        /// Creates a lazy text loader for a document, avoiding a synchronous disk read on the UI thread.
        /// </summary>
        private static TextLoader CreateTextLoader(TextDocument doc)
        {
            // On-disk documents: load lazily/asynchronously from file when text is actually needed.
            if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))
                return new FileTextLoader(doc.FilePath, Encoding.UTF8);

            // In-memory documents (e.g. open scratch scripts) already have materialized text.
            if (doc.TryGetText(out var text))
                return TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create(), doc.FilePath));

            return TextLoader.From(TextAndVersion.Create(SourceText.From(string.Empty), VersionStamp.Create(), doc.FilePath));
        }

        private DocumentInfo CreateDocumentInfoWithText(ProjectId projectId, TextDocument doc, Dictionary<string, ImmutableArray<DocumentId>> fileMapping = null)
        {
            return CreateDocumentInfoWithoutText(projectId, doc, fileMapping).WithTextLoader(CreateTextLoader(doc));
        }

        private DocumentInfo CreateDocumentInfoWithoutText(ProjectId projectId, TextDocument doc, Dictionary<string, ImmutableArray<DocumentId>> fileMapping = null)
        {
            DocumentId docId = doc.Id;

            if (fileMapping != null)
            {
                // Try to find a matching document id
                ImmutableArray<DocumentId> matchingDocuments;
                if (doc.FilePath != null && fileMapping.TryGetValue(doc.FilePath, out matchingDocuments) && matchingDocuments.Length == 1)
                {
                    docId = matchingDocuments[0];
                }
                else
                {
                    // Otherwise, create a new document id with the proper projectId (the one from source project probably had a different project id)
                    docId = DocumentId.CreateNewId(projectId, doc.Name);
                }
            }

            var sourceDoc = doc as Document;
            return DocumentInfo.Create(
                docId,
                doc.Name,
                doc.Folders,
                sourceDoc != null ? sourceDoc.SourceCodeKind : SourceCodeKind.Regular,
                filePath: doc.FilePath);
        }

        public delegate bool AllowReloadDelegate(Document newDocument, ref object state);
        
        struct TrackDocumentCallback
        {
            public AllowReloadDelegate AllowReload;
            public Action<SourceText, bool> TextUpdated;
            public Action ExternalChangesDetected;
        }
    }
}
