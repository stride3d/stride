// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Models;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands
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
                // The flag of a node is its inlined property: the item itself is a node information, which takes no flag
                var preserve = item.TryGetChild(InlineData.InlinedProperty) ?? item.TryGetChild(nameof(NodeInformation.Preserve));
                preserve?.UpdateValue(parameter);
            }
        }
    }
}
