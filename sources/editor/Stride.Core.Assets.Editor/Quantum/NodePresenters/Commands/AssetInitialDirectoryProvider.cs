// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

class AssetInitialDirectoryProvider : IInitialDirectoryProvider
{
    private readonly SessionViewModel session;

    public AssetInitialDirectoryProvider(SessionViewModel session)
    {
        this.session = session;
    }

    public UDirectory? GetInitialDirectory(UDirectory? currentPath)
    {
        if (session is { AssetCollection.SelectedAssets.Count: 1 } && currentPath != null)
        {
            var asset = session.AssetCollection.SelectedAssets[0];
            var projectPath = asset.Directory.Package.PackagePath;
            if (projectPath != null)
            {
                var assetFullPath = UPath.Combine(projectPath.GetFullDirectory(), new UFile(asset.Url));

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
