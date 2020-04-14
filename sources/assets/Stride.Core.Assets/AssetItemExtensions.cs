// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core.Assets
{
    public static class AssetItemExtensions
    {
        /// <summary>
        /// Gets the asset filename relative to its .csproj file for <see cref="IProjectAsset"/>.
        /// </summary>
        /// <param name="assetItem">The asset item.</param>
        /// <returns></returns>
        public static string GetProjectInclude([NotNull] this AssetItem assetItem)
        {
            var assetFullPath = assetItem.FullPath;
            var projectFullPath = (assetItem.Package.Container as SolutionProject)?.FullPath;
            return assetFullPath.MakeRelative(projectFullPath.GetFullDirectory()).ToWindowsPath();
        }

        /// <summary>
        /// If the asset is a <see cref="IProjectFileGeneratorAsset"/>, gets the generated file full path.
        /// </summary>
        /// <param name="assetItem">The asset item.</param>
        /// <returns></returns>
        [NotNull]
        public static UFile GetGeneratedAbsolutePath([NotNull] this AssetItem assetItem)
        {
            return new UFile(assetItem.FullPath + ".cs");
        }

        /// <summary>
        /// If the asset is a <see cref="IProjectFileGeneratorAsset"/>, gets the generated file path relative to its containing .csproj.
        /// </summary>
        /// <param name="assetItem">The asset item.</param>
        /// <returns></returns>
        public static string GetGeneratedInclude([NotNull] this AssetItem assetItem)
        {
            return GetProjectInclude(assetItem) + ".cs";
        }
    }
}
