// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Models;

namespace Stride.Assets.Presentation.NodePresenters.Commands
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
