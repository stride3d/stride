// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Assets.Presentation.Templates
{
    /// <summary>
    /// A view model class used to create or modify asset packs of a package.
    /// </summary>
    public class AssetPackageViewModel : ViewModelBase
    {
        private bool isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPackageViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="packageLocation">The directory containing this package.</param>
        /// <param name="initiallySelected">Indicates whether this plaform should be initially selected.</param>
        public AssetPackageViewModel(IViewModelServiceProvider serviceProvider, string name, UDirectory packageLocation, bool initiallySelected)
            : base(serviceProvider)
        {
            PackageName = name;
            PackageLocation = packageLocation;
            IsSelected = initiallySelected;
        }

        /// <summary>
        /// Gets the name of this platform.
        /// </summary>
        public string Name => string.IsNullOrEmpty(PackageName) ? (PackageLocation?.GetDirectoryName() ?? "Asset") : PackageName;

        /// <summary>
        /// Gets whether this platform can be unselected.
        /// </summary>
        public bool CanBeUnselected => true;

        /// <summary>
        /// Gets whether this platform is currently selected.
        /// </summary>
        public bool IsSelected { get { return isSelected; } set { SetValue(ref isSelected, value); } }

        /// <summary>
        /// Gets the package location 
        /// </summary>
        public UDirectory PackageLocation { get; }

        /// <summary>
        /// Gets the package name 
        /// </summary>
        public string PackageName { get; }

    }
}
