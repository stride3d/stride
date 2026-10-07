// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Commands;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// A package the selected assets can be rooted in, as listed in the root-asset menu.
    /// </summary>
    public sealed class RootAssetTargetViewModel
    {
        public RootAssetTargetViewModel(SessionViewModel session, PackageViewModel package, bool isDefault, bool isChecked, bool isPartiallyChecked)
        {
            Package = package;
            IsDefault = isDefault;
            IsChecked = isChecked;
            IsPartiallyChecked = isPartiallyChecked;
            ToggleCommand = new AnonymousCommand(session.ServiceProvider, () => session.ToggleRootAsset(session.ActiveAssetView.SelectedAssets, package));
        }

        public PackageViewModel Package { get; }

        public string Name => Package.Name;

        /// <summary>
        /// Gets whether this is the package the plain root-asset toggle targets.
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// Gets whether all the selected assets are root assets of this package.
        /// </summary>
        public bool IsChecked { get; }

        /// <summary>
        /// Gets whether some, but not all, of the selected assets are root assets of this package.
        /// </summary>
        public bool IsPartiallyChecked { get; }

        public ICommandBase ToggleCommand { get; }
    }
}
