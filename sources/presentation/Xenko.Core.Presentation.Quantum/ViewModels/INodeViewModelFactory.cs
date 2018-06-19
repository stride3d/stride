// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum.Presenters;

namespace Xenko.Core.Presentation.Quantum.ViewModels
{
    public interface INodeViewModelFactory
    {
        [NotNull]
        NodeViewModel CreateGraph([NotNull] GraphViewModel owner, [NotNull] Type rootType, [NotNull] IEnumerable<INodePresenter> rootNodes);

        void GenerateChildren([NotNull] GraphViewModel owner, NodeViewModel parent, [NotNull] List<INodePresenter> nodePresenters);
    }
}
