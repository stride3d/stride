// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// Gives the folder of the selected asset as the initial directory of a file or folder picker, or a path relative to it.
/// </summary>
internal sealed class AssetInitialDirectoryProvider : IInitialDirectoryProvider
{
    private readonly ISessionViewModel session;

    public AssetInitialDirectoryProvider(ISessionViewModel session)
    {
        this.session = session;
    }

    public UDirectory? GetInitialDirectory(UDirectory? currentPath)
    {
        if (session != null && session.ActiveAssetView.SelectedAssets.Count == 1 && currentPath != null)
        {
            var asset = session.ActiveAssetView.SelectedAssets.First();
            var projectPath = asset.Directory.Package.PackagePath;
            if (projectPath != null)
            {
                // The file of the asset, in the asset folder of its package (its URL alone misses that folder)
                var assetFullPath = asset.AssetItem.FullPath;

                if (string.IsNullOrWhiteSpace(currentPath))
                {
                    return assetFullPath.GetFullDirectory();
                }
                var defaultPath = UPath.Combine(assetFullPath.GetFullDirectory(), currentPath);
                return defaultPath.GetFullDirectory();
            }
        }
        return currentPath;
    }
}
