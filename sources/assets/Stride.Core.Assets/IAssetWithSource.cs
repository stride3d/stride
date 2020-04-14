// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.IO;

namespace Xenko.Core.Assets
{
    public interface IAssetWithSource
    {
        /// <summary>
        /// The source file of this asset.
        /// </summary>
        /// <userdoc>
        /// The source file of this asset.
        /// </userdoc>
        UFile Source { get; set; }
    }
}
