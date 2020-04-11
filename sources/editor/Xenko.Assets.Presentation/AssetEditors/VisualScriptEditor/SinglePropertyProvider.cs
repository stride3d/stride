// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Quantum;

namespace Xenko.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    public class SinglePropertyProvider : IPropertyProviderViewModel
    {
        private readonly IObjectNode rootNode;

        public SinglePropertyProvider(IObjectNode rootNode)
        {
            this.rootNode = rootNode;
        }


        /// <inheritdoc/>
        public bool CanProvidePropertiesViewModel => true;

        /// <inheritdoc/>
        public IObjectNode GetRootNode() => rootNode;


        /// <inheritdoc/>
        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
    }
}
