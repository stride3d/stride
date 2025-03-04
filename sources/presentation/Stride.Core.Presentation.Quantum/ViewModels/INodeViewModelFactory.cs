// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Presentation.Quantum.ViewModels;

public interface INodeViewModelFactory
{
    NodeViewModel CreateGraph(GraphViewModel owner, Type rootType, IEnumerable<INodePresenter> rootNodes);

    void GenerateChildren(GraphViewModel owner, NodeViewModel parent, List<INodePresenter> nodePresenters);
}
