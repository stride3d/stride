// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// Enumerate assets that <see cref="PackageCompiler"/> will process.
    /// </summary>
    public interface IPackageCompilerSource
    {
        /// <summary>
        /// Enumerates assets.
        /// </summary>
        IEnumerable<AssetItem> GetAssets(AssetCompilerResult assetCompilerResult);
    }
}
