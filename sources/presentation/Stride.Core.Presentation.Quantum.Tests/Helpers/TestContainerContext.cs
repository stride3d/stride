// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Tests.Helpers;

public class TestContainerContext
{
    public TestContainerContext()
    {
        NodeContainer = new NodeContainer();
        GraphViewModelService = new GraphViewModelService(NodeContainer);
        ServiceProvider = new ViewModelServiceProvider();
        ServiceProvider.RegisterService(new NullDispatcherService());
        ServiceProvider.RegisterService(GraphViewModelService);
    }

    public ViewModelServiceProvider ServiceProvider { get; }

    public GraphViewModelService GraphViewModelService { get; }

    public NodeContainer NodeContainer { get; }

    public TestInstanceContext CreateInstanceContext(object instance)
    {
        var rootNode = NodeContainer.GetOrCreateNode(instance);
        var context = new TestInstanceContext(this, rootNode);
        return context;
    }
}
