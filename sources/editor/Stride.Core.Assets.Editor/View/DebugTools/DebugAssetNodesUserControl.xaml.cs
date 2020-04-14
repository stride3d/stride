// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.View.DebugTools
{
    public class DebugAssetNodeViewModel : DispatcherViewModel
    {
        public const string Null = "(NULL)";

        protected readonly IGraphNode Node;
        public DebugAssetNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode node)
            : base(serviceProvider)
        {
            Node = node;
            BreakCommand = new AnonymousCommand(ServiceProvider, Break);
        }

        public string Name => (Node as IMemberNode)?.Name ?? Node?.Type.Name ?? Null;

        public string Value => Node?.Retrieve()?.ToString() ?? Null;

        public string ContentType => GetContentType();

        public Type Type => Node?.Type;

        public ICommandBase BreakCommand { get; }

        private string GetContentType()
        {
            if (Node is IMemberNode) return "Member";
            if (Node is BoxedNode) return "Object (boxed)";
            if (Node is IObjectNode) return "Object";
            return "Unknown";
        }

        private void Break()
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }

    public class DebugAssetBaseNodeViewModel : DebugAssetNodeViewModel
    {
        public DebugAssetBaseNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode node)
            : base(serviceProvider, node)
        {
            Asset = DebugAssetNodeCollectionViewModel.FindAssetForNode(node.Guid);
        }

        public AssetViewModel Asset { get; }
    }

    public class DebugAssetChildNodeViewModel : DebugAssetNodeViewModel
    {
        public const string LinkRoot = "Root";
        public const string LinkChild = "Child";
        public const string LinkRef = "Ref";

        private readonly HashSet<IGraphNode> registeredNodes;

        public DebugAssetChildNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode node, HashSet<IGraphNode> registeredNodes)
            : this(serviceProvider, node, NodeIndex.Empty, null, LinkRoot, registeredNodes)
        {
        }

        private DebugAssetChildNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode node, NodeIndex index, ItemId? itemId, string linkFromParent, HashSet<IGraphNode> registeredNodes)
            : base(serviceProvider, node)
        {
            this.registeredNodes = registeredNodes;
            LinkFromParent = linkFromParent;
            Index = index;
            ItemId = itemId;
            Registered = node == null || registeredNodes.Contains(node);
            var assetNode = (IAssetNode)node;
            var baseNode = assetNode?.BaseNode;
            if (baseNode != null)
                Base = new DebugAssetBaseNodeViewModel(serviceProvider, baseNode);
        }

        public NodeIndex Index { get; }

        public ItemId? ItemId { get; }

        public string LinkFromParent { get; }

        public bool Registered { get; }

        public DebugAssetBaseNodeViewModel Base { get; }

        public List<DebugAssetNodeViewModel> Children => UpdateChildren();

        protected List<DebugAssetNodeViewModel> UpdateChildren()
        {
            var list = new List<DebugAssetNodeViewModel>();
            if (Node != null && Registered)
            {
                var objNode = Node as IObjectNode;
                if (objNode != null)
                {
                    foreach (var child in objNode.Members)
                    {
                        list.Add(new DebugAssetChildNodeViewModel(ServiceProvider, child, NodeIndex.Empty, null, LinkChild, registeredNodes));
                    }
                }
                if (Node.IsReference)
                {
                    var objReference = (Node as IMemberNode)?.TargetReference;
                    if (objReference != null)
                    {
                        list.Add(new DebugAssetChildNodeViewModel(ServiceProvider, objReference.TargetNode, objReference.Index, null, LinkRef, registeredNodes));
                    }
                    else
                    {
                        CollectionItemIdHelper.TryGetCollectionItemIds(Node.Retrieve(), out var itemIds);
                        foreach (var reference in ((IObjectNode)Node).ItemReferences)
                        {
                            ItemId? itemId = null;
                            if (itemIds != null && itemIds.TryGet(reference.Index.Value, out var retrievedItemId))
                                itemId = retrievedItemId;
                            list.Add(new DebugAssetChildNodeViewModel(ServiceProvider, reference.TargetNode, reference.Index, itemId, LinkRef, registeredNodes));

                        }
                    }
                }
            }
            return list;
        }
    }

    public class DebugAssetRootNodeViewModel : DebugAssetChildNodeViewModel
    {
        public DebugAssetRootNodeViewModel(IViewModelServiceProvider serviceProvider, string assetName, IGraphNode node, HashSet<IGraphNode> registeredNodes)
            : base(serviceProvider, node, registeredNodes)
        {
            AssetName = assetName;
        }

        public string AssetName { get; }

        public Type AssetType => Node.Type;
    }

    public class DebugAssetNodeCollectionViewModel : DispatcherViewModel
    {
        private static readonly FieldInfo FieldInfoListener;
        private static readonly FieldInfo FieldInfoRegisteredNodes;
        private readonly SessionViewModel session;
        //private readonly NodeContainer nodeContainer;

        private object selectedNode;
        private static readonly Dictionary<Guid, AssetViewModel> NodeToAssetMap = new Dictionary<Guid, AssetViewModel>();

        static DebugAssetNodeCollectionViewModel()
        {
            // We use reflection to access a non-public field.
            FieldInfoListener = typeof(AssetPropertyGraph).GetField("nodeListener", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfoRegisteredNodes = typeof(GraphNodeChangeListener).GetField("RegisteredNodes", BindingFlags.Instance | BindingFlags.NonPublic);
            if (FieldInfoListener == null)
            {
                throw new MissingFieldException("AssetPropertyGraph misses the nodeListener private member.");
            }
            if (FieldInfoRegisteredNodes == null)
            {
                throw new MissingFieldException("GraphNodeChangeListener misses the RegisteredNodes private member.");
            }
        }

        public DebugAssetNodeCollectionViewModel(SessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            this.session = session;

            RefreshQuantumNodesCommand = new AnonymousCommand(ServiceProvider, RefreshQuantumViewModel);
            //SelectNodeCommand = new AnonymousCommand<Guid>(ServiceProvider, SelectNodeByGuid);
        }

        public ObservableList<DebugAssetRootNodeViewModel> AssetNodes { get; } = new ObservableList<DebugAssetRootNodeViewModel>();

        public object SelectedNode { get => selectedNode; set => SetValue(ref selectedNode, value); }

        public ICommandBase RefreshQuantumNodesCommand { get; }

        //public ICommandBase SelectNodeCommand { get; private set; }

        public static AssetViewModel FindAssetForNode(Guid nodeId)
        {
            AssetViewModel asset;
            NodeToAssetMap.TryGetValue(nodeId, out asset);
            return asset;
        }

        private void RefreshQuantumViewModel()
        {
            RefreshNodeToAssetMap();
            AssetNodes.Clear();
            foreach (var asset in session.LocalPackages.SelectMany(x => x.Assets))
            {
                var nodes = GetRegisterNodes(asset.PropertyGraph);
                if (nodes == null)
                    continue;

                var rootNode = new DebugAssetRootNodeViewModel(ServiceProvider, asset.Url, asset.AssetRootNode, nodes);
                AssetNodes.Add(rootNode);
            }
        }

        private void RefreshNodeToAssetMap()
        {
            foreach (var asset in session.LocalPackages.SelectMany(x => x.Assets))
            {
                var nodes = GetRegisterNodes(asset?.PropertyGraph);
                if (nodes == null)
                    continue;

                foreach (var node in nodes)
                {
                    NodeToAssetMap[node.Guid] = asset;
                }
            }
        }

        private static HashSet<IGraphNode> GetRegisterNodes(AssetPropertyGraph propertyGraph)
        {
            if (propertyGraph == null)
                return null;

            var listener = (GraphNodeChangeListener)FieldInfoListener.GetValue(propertyGraph);
            return (HashSet<IGraphNode>)FieldInfoRegisteredNodes.GetValue(listener);
        }
    }

    /// <summary>
    /// Interaction logic for DebugQuantumUserControl.xaml
    /// </summary>
    public partial class DebugAssetNodesUserControl : IDebugPage
    {

        public DebugAssetNodesUserControl(SessionViewModel session)
        {
            InitializeComponent();
            DataContext = new DebugAssetNodeCollectionViewModel(session);
        }

        public string Title { get; set; }

        public DebugAssetNodeCollectionViewModel ViewModel => (DebugAssetNodeCollectionViewModel)DataContext;
    }
}
