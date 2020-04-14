// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Interop;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class CopyPropertyCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "CopyProperty";
        
        /// <inheritdoc />
        public override string Name => CommandName;

        /// <inheritdoc />
        public override CombineMode CombineMode => CombineMode.DoNotCombine;

        /// <inheritdoc />
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            var assetNodePresenter = nodePresenter as IAssetNodePresenter;
            var assetPropertyProvider = nodePresenter.PropertyProvider as IAssetPropertyProviderViewModel;
            return assetNodePresenter != null & assetPropertyProvider?.RelatedAsset.ServiceProvider.TryGet<ICopyPasteService>() != null;
        }

        /// <inheritdoc />
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            try
            {
                var assetNodePresenter = (IAssetNodePresenter)nodePresenter;
                var asset = assetNodePresenter.Asset;
                var service = asset.ServiceProvider.Get<ICopyPasteService>();
                var text = service.CopyFromAsset(asset.PropertyGraph, asset.Id, nodePresenter.Value, assetNodePresenter.IsObjectReference(nodePresenter.Value));
                if (!string.IsNullOrEmpty(text))
                    SafeClipboard.SetText(text);
            }
            catch (SystemException e)
            {
                // We don't provide feedback when copying fails.
                e.Ignore();
            }
        }
    }
}
