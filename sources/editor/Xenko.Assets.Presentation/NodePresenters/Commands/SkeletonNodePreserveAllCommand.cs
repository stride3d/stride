// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Assets.Models;

namespace Xenko.Assets.Presentation.NodePresenters.Commands
{
    public class SkeletonNodePreserveAllCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "SkeletonNodePreserveAll";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return typeof(ICollection<NodeInformation>).IsAssignableFrom(nodePresenter.Type);
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            foreach (var item in nodePresenter.Children)
            {
                item.UpdateValue(parameter);
            }
        }
    }
}
