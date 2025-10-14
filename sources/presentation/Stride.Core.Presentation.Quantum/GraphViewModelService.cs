// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum;

/// <summary>
/// A class that provides various services to <see cref="GraphViewModel"/> objects
/// </summary>
public class GraphViewModelService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphViewModelService"/> class.
    /// </summary>
    public GraphViewModelService(NodeContainer nodeContainer)
    {
        ArgumentNullException.ThrowIfNull(nodeContainer);
        NodePresenterFactory = new NodePresenterFactory(nodeContainer.NodeBuilder, AvailableCommands, AvailableUpdaters);
        NodeViewModelFactory = new NodeViewModelFactory();
    }

    public INodePresenterFactory NodePresenterFactory { get; set; }

    public NodeViewModelFactory NodeViewModelFactory { get; set; }

    public List<INodePresenterCommand> AvailableCommands { get; } = [];

    public List<INodePresenterUpdater> AvailableUpdaters { get; } = [];
}
