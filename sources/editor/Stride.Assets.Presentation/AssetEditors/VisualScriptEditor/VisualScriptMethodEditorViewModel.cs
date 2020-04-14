// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    public class VisualScriptMethodEditorViewModel : DispatcherViewModel, IAddChildViewModel
    {
        private readonly VisualScriptEditorViewModel editor;
        private readonly VisualScriptMethodViewModel method;

        private readonly IObjectNode blocksNode;
        private readonly IObjectNode linksNode;
        private readonly IObjectNode parametersNode;

        private readonly ObservableList<BlockNodeVertex> blocks = new ObservableList<BlockNodeVertex>();
        private readonly Dictionary<Block, BlockNodeVertex> blockMapping = new Dictionary<Block, BlockNodeVertex>();

        private readonly ObservableList<LinkNodeEdge> links = new ObservableList<LinkNodeEdge>();
        private readonly Dictionary<Link, LinkNodeEdge> linkMapping = new Dictionary<Link, LinkNodeEdge>();

        private GraphViewModel parameters;

        private Int2 blockCreationPosition;

        public VisualScriptMethodEditorViewModel(VisualScriptEditorViewModel editor, VisualScriptMethodViewModel method) : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
        {
            this.editor = editor;
            this.method = method;

            var methodNode = editor.Session.AssetNodeContainer.GetNode(method.Method);
            blocksNode = methodNode[nameof(method.Method.Blocks)].Target;
            linksNode = methodNode[nameof(method.Method.Links)].Target;
            parametersNode = methodNode[nameof(method.Method.Parameters)].Target;
        }

        public async Task<bool> Initialize()
        {
            SelectedBlocks.CollectionChanged += SelectedBlocks_CollectionChanged;

            parameters = GraphViewModel.Create(ServiceProvider, new[] { new SinglePropertyProvider(parametersNode) });

            using (var transaction = editor.UndoRedoService.CreateTransaction())
            {
                // Add existing blocks and links
                blocksNode.ItemChanged += BlocksContentChanged;
                foreach (var block in (IEnumerable<Block>)blocksNode.Retrieve())
                    await AddBlockViewModel(block);

                linksNode.ItemChanged += LinksContentChanged;
                foreach (var link in (IEnumerable<Link>)linksNode.Retrieve())
                    AddLinkViewModel(link);

                editor.UndoRedoService.SetName(transaction, "Update graph asset");
            }

            // Forward global diagnostics to blocks/links
            editor.Diagnostics.CollectionChanged += Diagnostics_CollectionChanged;

            DeleteSelectionCommand = new AnonymousCommand(ServiceProvider, DeleteSelection);
            ContextMenuOpeningCommand = new AnonymousCommand<System.Windows.Point>(ServiceProvider, ContextMenuOpening);
            RunBlockTemplateCommand = new AnonymousCommand<ITemplateDescriptionViewModel>(ServiceProvider, RunBlockTemplate);

            AddNewParameterCommand = new AnonymousCommand(ServiceProvider, AddNewParameter);
            RemoveSelectedParametersCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedParameters);

            return true;
        }

        public override void Destroy()
        {
            editor.Diagnostics.CollectionChanged -= Diagnostics_CollectionChanged;

            blocksNode.ItemChanged -= BlocksContentChanged;
            linksNode.ItemChanged -= LinksContentChanged;

            SelectedBlocks.CollectionChanged -= SelectedBlocks_CollectionChanged;

            base.Destroy();
        }

        public VisualScriptEditorViewModel Editor => editor;

        public VisualScriptMethodViewModel Method => method;

        public ObservableList<BlockNodeVertex> Blocks => blocks;

        public ObservableList<LinkNodeEdge> Links => links;

        public ObservableCollection<BlockNodeVertex> SelectedBlocks { get; } = new ObservableCollection<BlockNodeVertex>();
        public ObservableCollection<LinkNodeEdge> SelectedLinks { get; } = new ObservableCollection<LinkNodeEdge>();

        public ICommandBase DeleteSelectionCommand { get; private set; }

        public ICommandBase ContextMenuOpeningCommand { get; private set; }

        public ICommandBase RunBlockTemplateCommand { get; private set; }

        public ICommandBase AddNewParameterCommand { get; private set; }

        public ICommandBase RemoveSelectedParametersCommand { get; private set; }

        public NodeViewModel Parameters => parameters.RootNode;

        public ObservableList<object> SelectedParameters { get; } = new ObservableList<object>();

        private void AddNewParameter()
        {
            var parameter = new Parameter("bool", $"variable{Parameters.Children.Count}");

            method.AddParameter(parameter);

            //SelectedParameters.Clear();
            //SelectedParameters.Add(Session.AssetNodeContainer.GetNode(parameter));
        }

        private void RemoveSelectedParameters()
        {
            using (var transaction = editor.UndoRedoService.CreateTransaction())
            {
                foreach (var parameter in SelectedParameters.Cast<NodeViewModel>().Select(x => (Parameter)x.NodeValue).ToList())
                {
                    method.RemoveParameter(parameter);
                }

                editor.UndoRedoService.SetName(transaction, "Delete variable(s)");
            }
        }

        public void FinalizeMoves()
        {
            using (var transaction = editor.UndoRedoService.CreateTransaction())
            {
                // Check every selected element to update its final position (in case it changed)
                foreach (var selectedVertex in SelectedBlocks)
                {
                    selectedVertex.FinalizeMove();
                }

                editor.UndoRedoService.SetName(transaction, "Moved graph elements");
            }
        }

        #region Drag & Drop

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            if (children.Count != 1)
            {
                message = "Only one element can be dropped at a time";
                return false;
            }

            var child = children.Single();
            var variable = ((NodeViewModel)child).NodeValue as Symbol;
            if (variable != null)
            {
                message = $"Add variable {variable}";
                return true;
            }

            // We only process variables drops for now
            // TODO: Process prefab drops
            message = "Can only drop variables";
            return false;
        }

        async void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            var child = children.Single();

            var symbol = ((NodeViewModel)child).NodeValue as Symbol;
            if (symbol != null)
            {
                // Check if user wants a getter or setter
                var block = await Editor.VisualScriptViewModelService.TransformVariableIntoBlock(symbol);
                if (block != null)
                {
                    method.AddBlock(block);
                }
            }
        }

        #endregion


        private void SelectedBlocks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Update selection
            editor.EditorProperties.UpdateTypeAndName(SelectedBlocks, x => "Block", x => x.ViewModel.Title, "blocks");
            editor.EditorProperties.GenerateSelectionPropertiesAsync(SelectedBlocks.Select(x => x.ViewModel)).Forget();
        }

        private void DeleteSelection()
        {
            // Group everything as a single transaction, since it might delete multiple blocks, and it will also remove dependent links
            using (var transaction = editor.UndoRedoService.CreateTransaction())
            {
                // Delete each selected link
                foreach (var link in SelectedLinks.ToArray())
                    method.RemoveLink(link.ViewModel.Link);

                // Delete each selected block
                foreach (var block in SelectedBlocks.ToArray())
                    method.RemoveBlock(block.ViewModel.Block);

                editor.UndoRedoService.SetName(transaction, "Deleting selected graph elements");
            }
        }

        public void ContextMenuOpening(System.Windows.Point mousePosition)
        {
            // Store where mouse was when context menu was opened
            blockCreationPosition = new Int2((int)Math.Round(mousePosition.X), (int)Math.Round(mousePosition.Y));
        }

        private async void RunBlockTemplate(ITemplateDescriptionViewModel template)
        {
            // Get block factory
            var blockTemplate = (BlockTemplateDescriptionViewModel)template;
            var blockFactory = blockTemplate.BlockFactory;

            // Create block
            var block = await blockFactory.Create();
            if (block == null)
                return;

            // Position the block where the mouse is
            block.Position = blockCreationPosition;

            // Add block
            method.AddBlock(block);
        }


        // Note: ViewModel methods are only used for internal update related to Quantum events
        // If you want to add a block or link, please use GraphicsCompositorViewModel through Asset property
        private void AddLinkViewModel(Link link)
        {
            BlockNodeVertex source, target;
            if (!blockMapping.TryGetValue(link.Source.Owner, out source)
                || !blockMapping.TryGetValue(link.Target.Owner, out target))
                throw new InvalidOperationException("Could not find source or target block when creating a link");

            VisualScriptSlotViewModel linkSource, linkTarget;
            if (!source.ViewModel.Slots.TryGetValue(link.Source, out linkSource)
                || !target.ViewModel.Slots.TryGetValue(link.Target, out linkTarget))
                throw new InvalidOperationException("Could not find source or target slot when creating a link");

            var viewModel = new VisualScriptLinkViewModel(this, link, linkSource, linkTarget);
            var linkNodeEdge = new LinkNodeEdge(viewModel, source, target);

            // Update initial diagnostics (if any)
            foreach (var diagnostic in editor.Diagnostics.Where(x => x.LinkId.HasValue && x.LinkId.Value == link.Id))
                linkNodeEdge.ViewModel.Diagnostics.Add(diagnostic);

            links.Add(linkNodeEdge);
            linkMapping.Add(link, linkNodeEdge);

            // Add ourselves to slot.Links
            linkSource.Links.Add(viewModel);
            linkTarget.Links.Add(viewModel);
        }

        private void RemoveLinkViewModel(Link link)
        {
            LinkNodeEdge linkNodeEdge;
            if (!linkMapping.TryGetValue(link, out linkNodeEdge))
            {
                throw new InvalidOperationException("Could not find link to delete");
            }

            // Remove ourselves from slot.Links
            var viewModel = linkNodeEdge.ViewModel;
            viewModel.SourceSlot.Links.Remove(viewModel);
            viewModel.TargetSlot.Links.Remove(viewModel);

            links.Remove(linkNodeEdge);
            linkMapping.Remove(link);
        }

        private async Task AddBlockViewModel(Block block)
        {
            using (var transaction = editor.UndoRedoService.CreateTransaction(TransactionFlags.KeepParentsAlive))
            {
                var viewModel = new VisualScriptBlockViewModel(this, block);
                var blockNodeVertex = new BlockNodeVertex(viewModel);

                // Update initial diagnostics (if any)
                foreach (var diagnostic in editor.Diagnostics.Where(x => x.BlockId.HasValue && x.BlockId.Value == block.Id))
                    viewModel.Diagnostics.Add(diagnostic);

                blocks.Add(blockNodeVertex);
                blockMapping.Add(block, blockNodeVertex);

                // (Re)generate slots
                await this.method.RegenerateSlots(block);

                editor.UndoRedoService.SetName(transaction, "Added block");
            }
        }

        private void RemoveBlockViewModel(Block block)
        {
            BlockNodeVertex blockNodeVertex;
            if (!blockMapping.TryGetValue(block, out blockNodeVertex))
            {
                throw new InvalidOperationException("Could not find block to delete");
            }
            blocks.Remove(blockNodeVertex);
            blockMapping.Remove(block);
        }

        private void BlocksContentChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.None:
                    break;

                case ContentChangeType.CollectionAdd:
                    {
                        var block = (Block)e.NewValue;
                        AddBlockViewModel(block).Forget();
                        break;
                    }
                case ContentChangeType.CollectionRemove:
                    {
                        var block = (Block)e.OldValue;
                        RemoveBlockViewModel(block);
                        break;
                    }
                case ContentChangeType.ValueChange:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LinksContentChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.None:
                    break;

                case ContentChangeType.CollectionAdd:
                    {
                        var link = (Link)e.NewValue;
                        AddLinkViewModel(link);
                        break;
                    }
                case ContentChangeType.CollectionRemove:
                    {
                        var link = (Link)e.OldValue;
                        RemoveLinkViewModel(link);
                        break;
                    }
                case ContentChangeType.ValueChange:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Diagnostics_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    foreach (var block in blocks)
                        block.ViewModel.Diagnostics.Clear();
                    break;
                case NotifyCollectionChangedAction.Add:
                    foreach (VisualScriptEditorViewModel.Diagnostic diagnostic in e.NewItems)
                    {
                        // Update Block/Link view model with diagnostics
                        if (diagnostic.BlockId.HasValue)
                            FindBlockFromId(diagnostic.BlockId.Value)?.ViewModel.Diagnostics.Add(diagnostic);
                        if (diagnostic.LinkId.HasValue)
                            FindLinkFromId(diagnostic.LinkId.Value)?.ViewModel.Diagnostics.Add(diagnostic);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (VisualScriptEditorViewModel.Diagnostic diagnostic in e.OldItems)
                    {
                        // Update Block/Link view model with diagnostics
                        if (diagnostic.BlockId.HasValue)
                            FindBlockFromId(diagnostic.BlockId.Value)?.ViewModel.Diagnostics.Remove(diagnostic);
                        if (diagnostic.LinkId.HasValue)
                            FindLinkFromId(diagnostic.LinkId.Value)?.ViewModel.Diagnostics.Remove(diagnostic);
                    }
                    break;
            }
        }

        private BlockNodeVertex FindBlockFromId(Guid blockId)
        {
            Block block;
            BlockNodeVertex blockNode;
            if (method.Method.Blocks.TryGetValue(blockId, out block)
                && blockMapping.TryGetValue(block, out blockNode))
            {
                return blockNode;
            }

            return null;
        }

        private LinkNodeEdge FindLinkFromId(Guid linkId)
        {
            Link link;
            LinkNodeEdge linkNode;
            if (method.Method.Links.TryGetValue(linkId, out link)
                && linkMapping.TryGetValue(link, out linkNode))
            {
                return linkNode;
            }

            return null;
        }
    }
}
