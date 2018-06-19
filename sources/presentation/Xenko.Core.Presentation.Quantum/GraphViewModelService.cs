// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Quantum;

namespace Xenko.Core.Presentation.Quantum
{
    /// <summary>
    /// A class that provides various services to <see cref="GraphViewModel"/> objects
    /// </summary>
    public class GraphViewModelService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewModelService"/> class.
        /// </summary>
        public GraphViewModelService([NotNull] NodeContainer nodeContainer)
        {
            if (nodeContainer == null) throw new ArgumentNullException(nameof(nodeContainer));
            NodePresenterFactory = new NodePresenterFactory(nodeContainer.NodeBuilder, AvailableCommands, AvailableUpdaters);
            NodeViewModelFactory = new NodeViewModelFactory();
        }

        public INodePresenterFactory NodePresenterFactory { get; set; }

        public NodeViewModelFactory NodeViewModelFactory { get; set; }

        public List<INodePresenterCommand> AvailableCommands { get; } = new List<INodePresenterCommand>();

        public List<INodePresenterUpdater> AvailableUpdaters { get; } = new List<INodePresenterUpdater>();
    }
}
