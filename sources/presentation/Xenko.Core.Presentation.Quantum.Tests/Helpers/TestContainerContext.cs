// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Threading;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;

namespace Xenko.Core.Presentation.Quantum.Tests.Helpers
{
    public class TestContainerContext
    {
        public TestContainerContext()
        {
            NodeContainer = new NodeContainer();
            GraphViewModelService = new GraphViewModelService(NodeContainer);
            ServiceProvider = new ViewModelServiceProvider();
            ServiceProvider.RegisterService(new DispatcherService(Dispatcher.CurrentDispatcher));
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
}
