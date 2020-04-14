// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class SetContentReferenceCommand : ChangeValueCommandBase
    {
        /// <summary>
        /// A structure corresponding to the parameter of this command.
        /// </summary>
        public struct Parameter
        {
            /// <summary>
            /// The target asset of the content reference to create.
            /// </summary>
            public AssetViewModel Asset;
            /// <summary>
            /// The type of content reference to create.
            /// </summary>
            public Type Type;
        }

        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "SetContentReference";

        /// <inheritdoc />
        public override string Name => CommandName;

        /// <inheritdoc />
        public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

        /// <inheritdoc />
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return ContentReferenceHelper.ContainsReferenceType(nodePresenter.Descriptor);
        }

        /// <inheritdoc />
        protected override object ChangeValue(object currentValue, object parameter, object preExecuteResult)
        {
            var param = (Parameter)parameter;
            var asset = param.Asset;
            if (asset == null)
                return null;

            var newValue = CreateReference(asset, param.Type);
            return newValue;
        }

        /// <summary>
        /// Creates the content reference corresponding to the given asset.
        /// </summary>
        /// <param name="asset">The asset for which to create a content reference.</param>
        /// <param name="referenceType">The type of content reference to create.</param>
        /// <returns>A content reference corresponding to the given asset.</returns>
        protected virtual object CreateReference(AssetViewModel asset, Type referenceType)
        {
            return ContentReferenceHelper.CreateReference(asset, referenceType);
        }
    }
}
