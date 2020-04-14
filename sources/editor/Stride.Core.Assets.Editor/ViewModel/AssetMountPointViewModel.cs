// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class AssetMountPointViewModel : MountPointViewModel
    {
        public AssetMountPointViewModel(PackageViewModel package)
            : base(package)
        {
        }

        public override string Name { get { return "Assets"; } set { throw new InvalidOperationException("The asset mount point cannot be renamed"); } }

        public override bool IsEditable => false;

        public override bool CanDelete(out string error)
        {
            error = "Unable to delete the asset root folder.";
            return false;
        }

        public override bool AcceptAssetType(Type assetType)
        {
            return !typeof(IProjectAsset).IsAssignableFrom(assetType);
        }
    }
}
