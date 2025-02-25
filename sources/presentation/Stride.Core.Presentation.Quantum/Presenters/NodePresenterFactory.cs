// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public class NodePresenterFactory : INodePresenterFactoryInternal
{
    private readonly INodeBuilder nodeBuilder;
    private readonly ThreadLocal<bool> buildingNodes = new();

    public NodePresenterFactory(INodeBuilder nodeBuilder, IReadOnlyCollection<INodePresenterCommand> availableCommands, IReadOnlyCollection<INodePresenterUpdater> availableUpdaters)
    {
        ArgumentNullException.ThrowIfNull(nodeBuilder);
        this.nodeBuilder = nodeBuilder;
        AvailableCommands = availableCommands ?? throw new ArgumentNullException(nameof(availableCommands));
        AvailableUpdaters = availableUpdaters ?? throw new ArgumentNullException(nameof(availableUpdaters));
    }

    public IReadOnlyCollection<INodePresenterCommand> AvailableCommands { get; }

    public IReadOnlyCollection<INodePresenterUpdater> AvailableUpdaters { get; }

    public bool IsPrimitiveType(Type type)
    {
        return nodeBuilder.IsPrimitiveType(type);
    }

    public INodePresenter CreateNodeHierarchy(IObjectNode rootNode, GraphNodePath rootNodePath, IPropertyProviderViewModel? propertyProvider)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        buildingNodes.Value = true;
        var rootPresenter = CreateRootPresenter(propertyProvider, rootNode);
        GenerateChildren(rootPresenter, rootNode, propertyProvider);
        RunUpdaters(rootPresenter);
        buildingNodes.Value = false;
        FinalizeTree(rootPresenter);
        return rootPresenter;
    }

    public void CreateChildren(IInitializingNodePresenter parentPresenter, IObjectNode? objectNode, IPropertyProviderViewModel? propertyProvider)
    {
        buildingNodes.Value = true;
        if (objectNode != null)
        {
            GenerateChildren(parentPresenter, objectNode, propertyProvider);
        }
        RunUpdaters(parentPresenter);
        buildingNodes.Value = false;
        FinalizeTree(parentPresenter.Root);
    }

    private void GenerateChildren(IInitializingNodePresenter parentPresenter, IObjectNode objectNode, IPropertyProviderViewModel? propertyProvider)
    {
        ArgumentNullException.ThrowIfNull(parentPresenter);
        ArgumentNullException.ThrowIfNull(objectNode);
        CreateMembers(propertyProvider, parentPresenter, objectNode);
        CreateItems(propertyProvider, parentPresenter, objectNode);
        parentPresenter.FinalizeInitialization();
    }

    protected virtual IInitializingNodePresenter CreateRootPresenter(IPropertyProviderViewModel? propertyProvider, IObjectNode rootNode)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        return new RootNodePresenter(this, propertyProvider, rootNode);
    }

    protected virtual bool ShouldCreateMemberPresenter(INodePresenter parent, IMemberNode member, IPropertyProviderViewModel? propertyProvider)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(member);
        // Ask the property provider if we have one, otherwise always construct.
        return propertyProvider?.ShouldConstructMember(member) ?? true;
    }

    protected virtual IInitializingNodePresenter CreateMember(IPropertyProviderViewModel? propertyProvider, INodePresenter parentPresenter, IMemberNode member)
    {
        ArgumentNullException.ThrowIfNull(parentPresenter);
        ArgumentNullException.ThrowIfNull(member);
        return new MemberNodePresenter(this, propertyProvider, parentPresenter, member);
    }

    protected virtual bool ShouldCreateItemPresenter(INodePresenter parent, IObjectNode collectionNode, NodeIndex index, IPropertyProviderViewModel? propertyProvider)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(collectionNode);
        // Ask the property provider if we have one, otherwise always construct.
        return propertyProvider?.ShouldConstructItem(collectionNode, index) ?? true;
    }

    protected virtual IInitializingNodePresenter CreateItem(IPropertyProviderViewModel? propertyProvider, INodePresenter containerPresenter, IObjectNode containerNode, NodeIndex index)
    {
        ArgumentNullException.ThrowIfNull(containerPresenter);
        ArgumentNullException.ThrowIfNull(containerNode);
        return new ItemNodePresenter(this, propertyProvider, containerPresenter, containerNode, index);
    }

    private void CreateMembers(IPropertyProviderViewModel? propertyProvider, IInitializingNodePresenter parentPresenter, IObjectNode objectNode)
    {
        foreach (var member in objectNode.Members)
        {
            if (ShouldCreateMemberPresenter(parentPresenter, member, propertyProvider))
            {
                var memberPresenter = CreateMember(propertyProvider, parentPresenter, member);
                if (member.Target != null)
                {
                    GenerateChildren(memberPresenter, member.Target, propertyProvider);
                }
                parentPresenter.AddChild(memberPresenter);
                RunUpdaters(memberPresenter);
            }
        }
    }

    private void CreateItems(IPropertyProviderViewModel? propertyProvider, IInitializingNodePresenter parentPresenter, IObjectNode objectNode)
    {
        if (objectNode.IsEnumerable)
        {
            if (objectNode.ItemReferences != null)
            {
                foreach (var item in objectNode.ItemReferences)
                {
                    if (ShouldCreateItemPresenter(parentPresenter, objectNode, item.Index, propertyProvider))
                    {
                        var itemPresenter = CreateItem(propertyProvider, parentPresenter, objectNode, item.Index);
                        if (item.TargetNode != null)
                        {
                            GenerateChildren(itemPresenter, item.TargetNode, propertyProvider);
                        }
                        parentPresenter.AddChild(itemPresenter);
                        RunUpdaters(itemPresenter);
                    }
                }
            }
            else
            {
                foreach (var item in objectNode.Indices)
                {
                    if (ShouldCreateItemPresenter(parentPresenter, objectNode, item, propertyProvider))
                    {
                        var itemPresenter = CreateItem(propertyProvider, parentPresenter, objectNode, item);
                        parentPresenter.AddChild(itemPresenter);
                        RunUpdaters(itemPresenter);
                    }
                }
            }
        }
    }

    protected void RunUpdaters(IInitializingNodePresenter nodePresenter)
    {
        foreach (var updater in AvailableUpdaters)
        {
            updater.UpdateNode(nodePresenter);
        }
    }

    protected void FinalizeTree(INodePresenter rootPresenter)
    {
        ArgumentNullException.ThrowIfNull(rootPresenter);

        // We might enter here while we're still constructing the hierarchy, if for example we create
        // a virtual node in one updater. In this case we skip this call because our hierarchy is still
        // incomplete. It's guaranteed that this method will be called again at the end of the creation.
        if (buildingNodes.IsValueCreated && buildingNodes.Value)
            return;

        foreach (var updater in AvailableUpdaters)
        {
            updater.FinalizeTree(rootPresenter);
        }
    }
}
