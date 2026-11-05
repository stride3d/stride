// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Common.Tests.Helpers;

/// <summary>
/// Builds node presenters for an object, without any updater or command, so tests can apply them one at a time.
/// </summary>
public sealed class PresenterTestContext
{
    private readonly NodeContainer nodeContainer = new();

    public INodePresenter CreatePresenter(object instance)
    {
        var service = new GraphViewModelService(nodeContainer);
        var rootNode = nodeContainer.GetOrCreateNode(instance);
        return service.NodePresenterFactory.CreateNodeHierarchy(rootNode, new GraphNodePath(rootNode));
    }

    public static INodePresenter Child(INodePresenter parent, string name)
        => parent.Children.Single(x => x.Name == name);
}
