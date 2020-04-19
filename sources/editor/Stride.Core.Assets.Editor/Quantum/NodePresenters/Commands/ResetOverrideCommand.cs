// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class ResetOverrideCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "ResetOverride";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return nodePresenter is IAssetNodePresenter;
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var assetNodePresenter = (IAssetNodePresenter)nodePresenter;
            assetNodePresenter.ResetOverride();
        }
    }
}
