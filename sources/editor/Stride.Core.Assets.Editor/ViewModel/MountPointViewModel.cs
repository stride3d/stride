// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class MountPointViewModel : DirectoryBaseViewModel
    {
        protected MountPointViewModel(PackageViewModel package)
            : base(package.Session)
        {
            Package = package;
        }

        public override string Path => string.Empty;

        public override DirectoryBaseViewModel Parent { get { return null; } set { throw new InvalidOperationException("Cannot change the parent of an AssetMountPointViewModel"); } }

        public override MountPointViewModel Root => this;

        public override PackageViewModel Package { get; }

        /// <inheritdoc/>
        public override string TypeDisplayName => "Mount Point";

        /// <inheritdoc/>
        public override IEnumerable<IDirtiable> Dirtiables => base.Dirtiables.Concat(Package.Dirtiables);

        public IReadOnlyCollection<DirectoryBaseViewModel> GetDirectoryHierarchy()
        {
            var hierarchy = new List<DirectoryBaseViewModel>();
            GetDirectoryHierarchy(hierarchy);
            return hierarchy;
        }

        public override void Delete()
        {
            throw new NotSupportedException("Cannot delete a MountPointViewModel");
        }

        public abstract bool AcceptAssetType(Type assetType);

        protected override void UpdateIsDeletedStatus()
        {
            throw new NotImplementedException();
        }
    }
}
