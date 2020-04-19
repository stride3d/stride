// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Analysis;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// Main interface for compiling an <see cref="Asset"/>.
    /// </summary>
    public interface IAssetCompiler
    {
        /// <summary>
        /// Compiles a list of assets from the specified package.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="assetItem">The asset reference.</param>
        /// <returns>The result of the compilation.</returns>
        AssetCompilerResult Prepare(AssetCompilerContext context, AssetItem assetItem);

        /// <summary>
        /// Enumerates all the dependencies required to compile this asset
        /// </summary>
        /// <param name="assetItem">The asset for which dependencies are enumerated</param>
        /// <returns>The dependencies</returns>
        IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem);

        /// <summary>
        /// Enumerates all the asset types required to compile this asset
        /// </summary>
        /// <param name="assetItem">The asset for which types are enumerated</param>
        /// <returns>The dependencies</returns>
        IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem);

        /// <summary>
        /// Enumerates all the asset types to exclude when compiling this asset
        /// </summary>
        /// <param name="assetItem">The asset for which types are enumerated</param>
        /// <returns>The types to exclude</returns>
        /// <remarks>This method takes higher priority, it will exclude assets included with inclusion methods even in the same compiler</remarks>
        IEnumerable<Type> GetInputTypesToExclude(AssetItem assetItem);

        bool AlwaysCheckRuntimeTypes { get; }

        IEnumerable<Type> GetRuntimeTypes(AssetItem assetItem);
    }
}
