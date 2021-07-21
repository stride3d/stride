// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// Arguments of the <see cref="SessionViewModel.ActiveAssetsChanged"/> event.
    /// </summary>
    public class ActiveAssetsChangedArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveAssetsChangedArgs"/> class.
        /// </summary>
        /// <param name="assets">The collection of assets that are active.</param>
        public ActiveAssetsChangedArgs(IReadOnlyCollection<AssetViewModel> assets)
        {
            Assets = assets;
        }

        /// <summary>
        /// Gets the collection of assets that are active.
        /// </summary>
        public IReadOnlyCollection<AssetViewModel> Assets { get; private set; }
    }
}
