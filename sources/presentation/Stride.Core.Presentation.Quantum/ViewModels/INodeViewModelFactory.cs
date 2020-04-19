// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Presentation.Quantum.ViewModels
{
    public interface INodeViewModelFactory
    {
        [NotNull]
        NodeViewModel CreateGraph([NotNull] GraphViewModel owner, [NotNull] Type rootType, [NotNull] IEnumerable<INodePresenter> rootNodes);

        void GenerateChildren([NotNull] GraphViewModel owner, NodeViewModel parent, [NotNull] List<INodePresenter> nodePresenters);
    }
}
