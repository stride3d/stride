// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Xenko.Core.Presentation.Quantum.Presenters
{
    public abstract class NodePresenterUpdaterBase : INodePresenterUpdater
    {
        public virtual void UpdateNode(INodePresenter node)
        {
            // Do nothing by default
        }

        public virtual void FinalizeTree(INodePresenter root)
        {
            // Do nothing by default
        }
    }
}
