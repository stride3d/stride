// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Assets.Rendering;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    [AssetEditorViewModel(typeof(GraphicsCompositorAsset), typeof(Views.GraphicsCompositorEditorView))]
    public class GraphicsCompositorEditorViewModel : AssetEditorViewModel
    {
        protected readonly GraphViewModelService ViewModelService;

        private readonly IObjectNode graphicsCompositorNode;
        private readonly IObjectNode renderStagesNode;
        private readonly IObjectNode renderFeaturesNode;
        private readonly IObjectNode cameraSlotsNode;
        private readonly IObjectNode sharedRenderersNode;
        
        public GraphicsCompositorEditorViewModel([NotNull] GraphicsCompositorViewModel graphicsCompositor) : base(graphicsCompositor)
        {
            // Create the service needed to manage observable view models
            ViewModelService = new GraphViewModelService(Session.AssetNodeContainer);

            // Update the service provider of this view model to contains the ObservableViewModelService we created.
            ServiceProvider = new ViewModelServiceProvider(ServiceProvider, ViewModelService.Yield());

            // Get some quantum nodes
            graphicsCompositorNode = Session.AssetNodeContainer.GetNode(graphicsCompositor.Asset);
            renderStagesNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.RenderStages)].Target;
            renderFeaturesNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.RenderFeatures)].Target;
            cameraSlotsNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.Cameras)].Target;
            sharedRenderersNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.SharedRenderers)].Target;

            // Setup commands
            DeleteSelectionCommand = new AnonymousCommand(ServiceProvider, DeleteSelection);

            AddNewRenderStageCommand = new AnonymousCommand(ServiceProvider, AddNewRenderStage);
            RemoveSelectedRenderStagesCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedRenderStages);

            AddNewRenderFeatureCommand = new AnonymousCommand<AbstractNodeType>(ServiceProvider, AddNewRenderFeature);
            RemoveSelectedRenderFeaturesCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedRenderFeatures);

            AddNewCameraSlotCommand = new AnonymousCommand(ServiceProvider, AddNewCameraSlot);
            RemoveSelectedCameraSlotsCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedCameraSlots);
        }

        public new GraphicsCompositorViewModel Asset => (GraphicsCompositorViewModel)base.Asset;

        public ObservableList<RenderStageViewModel> RenderStages { get; } = new ObservableList<RenderStageViewModel>();

        public ObservableList<RenderFeatureViewModel> RenderFeatures { get; } = new ObservableList<RenderFeatureViewModel>();
        public ObservableList<AbstractNodeType> RenderFeatureTypes { get; } = new ObservableList<AbstractNodeType>();

        public ObservableList<GraphicsCompositorCameraSlotsViewModel> CameraSlots { get; } = new ObservableList<GraphicsCompositorCameraSlotsViewModel>();

        public ObservableList<IGraphicsCompositorBlockViewModel> Blocks { get; } = new ObservableList<IGraphicsCompositorBlockViewModel>();

        public IObservableCollection<IGraphicsCompositorBlockViewModel> SelectedSharedRenderers { get; } = new ObservableList<IGraphicsCompositorBlockViewModel>();

        public IObservableCollection<IGraphicsCompositorLinkViewModel> SelectedRendererLinks { get; } = new ObservableList<IGraphicsCompositorLinkViewModel>();

        public ObservableList<object> SelectedRenderStages { get; } = new ObservableList<object>();

        public ObservableList<object> SelectedRenderFeatures { get; } = new ObservableList<object>();

        public ObservableList<object> SelectedCameraSlots { get; } = new ObservableList<object>();

        public ObservableList<SharedRendererFactoryViewModel> SharedRendererFactories { get; } = new ObservableList<SharedRendererFactoryViewModel>();

        public ICommandBase DeleteSelectionCommand { get; }

        public ICommandBase AddNewRenderStageCommand { get; }

        public ICommandBase RemoveSelectedRenderStagesCommand { get; }

        public ICommandBase AddNewRenderFeatureCommand { get; }

        public ICommandBase RemoveSelectedRenderFeaturesCommand { get; }

        public ICommandBase AddNewCameraSlotCommand { get; }

        public ICommandBase RemoveSelectedCameraSlotsCommand { get; }

        public sealed override Task<bool> Initialize()
        {
            // Add a block that represents entry points
            var entryPointOutputs = new[]
            {
                nameof(GraphicsCompositorAsset.Game),
                nameof(GraphicsCompositorAsset.SingleView),
                nameof(GraphicsCompositorAsset.Editor)
            };
            var entryPoint = new EntryPointBlockViewModel(this, graphicsCompositorNode, entryPointOutputs);
            entryPoint.Initialize();
            entryPoint.UpdateSlots();
            Blocks.Add(entryPoint);

            // Add blocks
            sharedRenderersNode.ItemChanged += SharedRenderersChanged;
            foreach (var sharedRendererNode in sharedRenderersNode.ItemReferences)
            {
                var sharedRenderer = (ISharedRenderer)sharedRendererNode.TargetNode.Retrieve();
                AddSharedRendererViewModel(sharedRenderer);
            }

            // Now that we have all blocks with all slots, we can update all links
            foreach (var block in Blocks)
            {
                foreach (var slot in block.OutputSlots.Cast<GraphicsCompositorSlotViewModel>())
                {
                    slot.UpdateLink();
                }
            }

            // TODO: Relayout the graph?

            // Add render stages
            renderStagesNode.ItemChanged += RenderStagesChanged;
            foreach (var renderStage in (IEnumerable<RenderStage>)renderStagesNode.Retrieve())
                RenderStages.Add(new RenderStageViewModel(this, renderStage));

            // Add render features
            renderFeaturesNode.ItemChanged += RenderFeaturesChanged;
            foreach (var renderFeature in (IEnumerable<RenderFeature>)renderFeaturesNode.Retrieve())
                RenderFeatures.Add(new RenderFeatureViewModel(this, renderFeature));

            // Add camera slots
            cameraSlotsNode.ItemChanged += CamerasSlotsChanged;
            foreach (var cameraSlot in (IEnumerable<SceneCameraSlot>)cameraSlotsNode.Retrieve())
                CameraSlots.Add(new GraphicsCompositorCameraSlotsViewModel(this, cameraSlot));

            // Update property grid on selected events
            SelectedRenderStages.CollectionChanged += SelectionChanged;
            SelectedRenderFeatures.CollectionChanged += SelectionChanged;
            SelectedCameraSlots.CollectionChanged += SelectionChanged;
            SelectedSharedRenderers.CollectionChanged += SelectionChanged;
            SelectedRendererLinks.CollectionChanged += SelectionChanged;

            // Make selection scope exclusives
            ServiceProvider.Get<SelectionService>().RegisterSelectionScope(id => RenderStages.FirstOrDefault(x => x.Id == id), obj => (obj as RenderStageViewModel)?.Id, SelectedRenderStages);
            ServiceProvider.Get<SelectionService>().RegisterSelectionScope(id => RenderFeatures.FirstOrDefault(x => x.Id == id), obj => (obj as RenderFeatureViewModel)?.Id, SelectedRenderFeatures);
            ServiceProvider.Get<SelectionService>().RegisterSelectionScope(id => CameraSlots.FirstOrDefault(x => x.Id == id), obj => (obj as GraphicsCompositorCameraSlotsViewModel)?.Id, SelectedCameraSlots);
            ServiceProvider.Get<SelectionService>().RegisterSelectionScope(id => Blocks.OfType<GraphicsCompositorBlockViewModel>().FirstOrDefault(x => x.Id == id), obj => (obj as GraphicsCompositorBlockViewModel)?.Id, SelectedSharedRenderers);

            // Update available types for factories
            UpdateAvailableTypes();
            AssemblyRegistry.AssemblyRegistered += AssembliesUpdated;
            AssemblyRegistry.AssemblyUnregistered += AssembliesUpdated;

            return Task.FromResult(true);
        }

        [NotNull]
        internal IGraphicsCompositorLinkViewModel CreateLink([NotNull] IGraphicsCompositorSlotViewModel source, [NotNull] IGraphicsCompositorSlotViewModel target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            return new GraphicsCompositorLinkViewModel(ServiceProvider, (GraphicsCompositorSlotViewModel)source, (GraphicsCompositorSlotViewModel)target);
        }

        public GraphicsCompositorBlockViewModel GetBlock(object value)
        {
            if (value == null)
                return null;

            foreach (var block in Blocks)
            {
                var sharedRenderedBlock = block as SharedRendererBlockViewModel;
                if (sharedRenderedBlock?.GetSharedRenderer() == value)
                {
                    return sharedRenderedBlock;
                }
            }

            return null;
        }

        private async void SelectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var selectedItems = ((IEnumerable<object>)sender).OfType<IPropertyProviderViewModel>().ToList();
            EditorProperties.UpdateTypeAndName(selectedItems, x => ComputeSelectionName(x, false), x => x.ToString(), ComputeSelectionName(selectedItems.FirstOrDefault(), true));
            await EditorProperties.GenerateSelectionPropertiesAsync(selectedItems);
        }

        private static string ComputeSelectionName(object selectedObject, bool plural)
        {
            if (selectedObject is SharedRendererBlockViewModel)
                return !plural ? "Shared renderer" : "shared renderers";
            if (selectedObject is EntryPointBlockViewModel)
                return !plural ? "Entry points" : "entry points";
            if (selectedObject is IGraphicsCompositorLinkViewModel)
                return !plural ? "Renderer link" : "renderer links";
            if (selectedObject is RenderStageViewModel)
                return !plural ? "Render stage" : "render stages";
            if (selectedObject is RenderFeatureViewModel)
                return !plural ? "Render feature" : "render features";
            if (selectedObject is GraphicsCompositorCameraSlotsViewModel)
                return !plural ? "Camera slot" : "camera slots";

            return null;
        }


        public override void Destroy()
        {
            ServiceProvider.Get<SelectionService>().UnregisterSelectionScope(SelectedRenderStages);
            ServiceProvider.Get<SelectionService>().UnregisterSelectionScope(SelectedRenderFeatures);
            ServiceProvider.Get<SelectionService>().UnregisterSelectionScope(SelectedCameraSlots);
            ServiceProvider.Get<SelectionService>().UnregisterSelectionScope(SelectedSharedRenderers);

            SelectedRenderStages.CollectionChanged -= SelectionChanged;
            SelectedRenderFeatures.CollectionChanged -= SelectionChanged;
            SelectedCameraSlots.CollectionChanged -= SelectionChanged;
            SelectedSharedRenderers.CollectionChanged -= SelectionChanged;
            SelectedRendererLinks.CollectionChanged -= SelectionChanged;

            sharedRenderersNode.ItemChanged -= SharedRenderersChanged;
            renderStagesNode.ItemChanged -= RenderStagesChanged;
            renderFeaturesNode.ItemChanged -= RenderFeaturesChanged;
            cameraSlotsNode.ItemChanged -= CamerasSlotsChanged;

            AssemblyRegistry.AssemblyRegistered -= AssembliesUpdated;
            AssemblyRegistry.AssemblyUnregistered -= AssembliesUpdated;

            Blocks.Cast<GraphicsCompositorBlockViewModel>().ForEach(x => x.Destroy());
            base.Destroy();
        }

        private void DeleteSelection()
        {
            // Remove anything selected (should be only one type of elements at a given moment)
            RemoveSelectedSharedRenderers();
            RemoveSelectedRenderFeatures();
            RemoveSelectedRenderStages();
            RemoveSelectedCameraSlots();
            RemoveSelectedLinks();
        }

        private void RemoveSelectedLinks()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var toRemove = SelectedRendererLinks.Cast<GraphicsCompositorLinkViewModel>().ToList();
                // Clear selection first, so we don't try to unselect items that don't exist anymore later.
                SelectedRendererLinks.Clear();

                foreach (var viewModel in toRemove)
                {
                    viewModel.SourceSlot.ClearLink();
                    viewModel.Destroy();
                    viewModel.SourceSlot.Links.Remove(viewModel);
                    viewModel.TargetSlot.Links.Remove(viewModel);
                }

                UndoRedoService.SetName(transaction, "Delete link(s)");
            }
        }

        private void RemoveSelectedSharedRenderers()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var toRemove = SelectedSharedRenderers.OfType<SharedRendererBlockViewModel>().ToList();
                // Clear selection first, so we don't try to unselect items that don't exist anymore later.
                SelectedSharedRenderers.Clear();

                foreach (var viewModel in toRemove)
                {
                    // Find index
                    var index = Asset.Asset.SharedRenderers.IndexOf(viewModel.GetSharedRenderer());
                    if (index < 0)
                        continue;

                    // Remove
                    var itemIndex = new NodeIndex(index);
                    sharedRenderersNode.Remove(viewModel.GetSharedRenderer(), itemIndex);
                }

                UndoRedoService.SetName(transaction, "Delete renderer(s)");
            }
        }

        private void AddNewRenderStage()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var renderStage = new RenderStage();
                renderStagesNode.Add(renderStage);

                UndoRedoService.SetName(transaction, "Create new render stage");
            }
        }

        private void RemoveSelectedRenderStages()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var toRemove = SelectedRenderStages.Cast<RenderStageViewModel>().Select(x => x.RenderStage).ToList();
                // Clear selection first, so we don't try to unselect items that don't exist anymore later.
                SelectedRenderStages.Clear();

                foreach (var renderStage in toRemove)
                {
                    // Find index
                    var index = Asset.Asset.RenderStages.IndexOf(renderStage);
                    if (index < 0)
                        continue;

                    // Clear references to this object
                    Asset.PropertyGraph.ClearReferencesToObjects(renderStage.Id.Yield());

                    // Remove
                    var itemIndex = new NodeIndex(index);
                    renderStagesNode.Remove(renderStage, itemIndex);
                }

                UndoRedoService.SetName(transaction, "Delete render stage(s)");
            }
        }

        private void AddNewCameraSlot()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var cameraSlot = new SceneCameraSlot();
                cameraSlotsNode.Add(cameraSlot);

                UndoRedoService.SetName(transaction, "Create new camera slot");
            }
        }

        private void RemoveSelectedCameraSlots()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var toRemove = SelectedCameraSlots.Cast<GraphicsCompositorCameraSlotsViewModel>().Select(x => x.CameraSlot).ToList();
                // Clear selection first, so we don't try to unselect items that don't exist anymore later.
                SelectedCameraSlots.Clear();

                foreach (var viewModel in toRemove)
                {
                    // Find index
                    var index = Asset.Asset.Cameras.IndexOf(viewModel);
                    if (index < 0)
                        continue;

                    // Do not clear references to this object - camera slots in the scene asset should not change automatically!

                    // Remove
                    var itemIndex = new NodeIndex(index);
                    cameraSlotsNode.Remove(viewModel, itemIndex);
                }

                UndoRedoService.SetName(transaction, "Delete camera slot(s)");
            }
        }

        private void AssembliesUpdated(object sender, AssemblyRegisteredEventArgs e)
        {
            UpdateAvailableTypes();
        }

        private void AddNewRenderFeature(AbstractNodeType abstractNodeType)
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var renderFeature = abstractNodeType.GenerateValue(null);
                renderFeaturesNode.Add(renderFeature);

                UndoRedoService.SetName(transaction, "Create new render feature");
            }
        }

        private void RemoveSelectedRenderFeatures()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var toRemove = SelectedRenderFeatures.Cast<RenderFeatureViewModel>().Select(x => x.RenderFeature).ToList();
                // Clear selection first, so we don't try to unselect items that don't exist anymore later.
                SelectedRenderFeatures.Clear();

                foreach (var renderFeature in toRemove)
                {
                    // Find index
                    var index = Asset.Asset.RenderFeatures.IndexOf(renderFeature);
                    if (index < 0)
                        continue;

                    // Remove
                    var itemIndex = new NodeIndex(index);
                    renderFeaturesNode.Remove(renderFeature, itemIndex);
                }

                UndoRedoService.SetName(transaction, "Delete render feature(s)");
            }
        }

        private void SharedRenderersChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.None:
                    break;

                case ContentChangeType.CollectionAdd:
                    {
                        var sharedRenderer = (ISharedRenderer)e.NewValue;
                        AddSharedRendererViewModel(sharedRenderer);
                        // Make sure to rebuild all outgoing links, if the new renderer is already connected to the rest of the graph
                        foreach (var block in Blocks.OfType< GraphicsCompositorBlockViewModel>())
                        {
                            block.UpdateOutgoingLinks();
                        }
                        break;
                    }
                case ContentChangeType.CollectionRemove:
                    {
                        var sharedRenderer = (ISharedRenderer)e.OldValue;
                        RemoveSharedRendererViewModel(sharedRenderer);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RenderStagesChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.None:
                    break;

                case ContentChangeType.CollectionAdd:
                    {
                        RenderStages.Insert(e.Index.Int, new RenderStageViewModel(this, (RenderStage)e.NewValue));
                        break;
                    }
                case ContentChangeType.CollectionRemove:
                    {
                        RenderStages.RemoveAt(e.Index.Int);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RenderFeaturesChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.None:
                    break;

                case ContentChangeType.CollectionAdd:
                    {
                        RenderFeatures.Insert(e.Index.Int, new RenderFeatureViewModel(this, (RenderFeature)e.NewValue));
                        break;
                    }
                case ContentChangeType.CollectionRemove:
                    {
                        RenderFeatures.RemoveAt(e.Index.Int);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CamerasSlotsChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.None:
                    break;

                case ContentChangeType.CollectionAdd:
                    {
                        CameraSlots.Insert(e.Index.Int, new GraphicsCompositorCameraSlotsViewModel(this, (SceneCameraSlot)e.NewValue));
                        break;
                    }
                case ContentChangeType.CollectionRemove:
                    {
                        CameraSlots.ElementAt(e.Index.Int).Dispose();
                        CameraSlots.RemoveAt(e.Index.Int);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddSharedRendererViewModel(ISharedRenderer sharedRenderer)
        {
            var viewModel = new SharedRendererBlockViewModel(this, sharedRenderer);
            viewModel.Initialize();
            viewModel.UpdateSlots();
            Blocks.Add(viewModel);
        }

        private void RemoveSharedRendererViewModel(ISharedRenderer sharedRenderer)
        {
            // Clear references to this object
            Asset.PropertyGraph.ClearReferencesToObjects(sharedRenderer.Id.Yield());

            var block = GetBlock(sharedRenderer);
            Blocks.Remove(block);
            block.Destroy();
        }

        private void UpdateAvailableTypes()
        {
            RenderFeatureTypes.Clear();
            RenderFeatureTypes.AddRange(AbstractNodeType.GetInheritedInstantiableTypes(typeof(RootRenderFeature)));

            var sharedRendererFactories = new List<SharedRendererFactoryViewModel>();
            foreach (var rendererType in typeof(ISharedRenderer).GetInheritedInstantiableTypes())
            {
                if (TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<NonInstantiableAttribute>(rendererType) != null)
                    continue;
                if (TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<ObsoleteAttribute>(rendererType) != null)
                    continue;

                sharedRendererFactories.Add(new SharedRendererFactoryViewModel(ServiceProvider, sharedRenderersNode, rendererType));
            }
            sharedRendererFactories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            SharedRendererFactories.Clear();
            SharedRendererFactories.AddRange(sharedRendererFactories);
        }
    }
}
