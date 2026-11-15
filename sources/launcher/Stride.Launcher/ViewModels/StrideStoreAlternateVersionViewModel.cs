// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Packages;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Launcher.ViewModels;

public sealed class StrideStoreAlternateVersionViewModel : DispatcherViewModel
{
    internal NugetServerPackage ServerPackage;
    internal NugetLocalPackage LocalPackage;
    private readonly StrideStoreVersionViewModel strideVersion;

    public StrideStoreAlternateVersionViewModel(StrideStoreVersionViewModel strideVersion)
        : base(strideVersion.ServiceProvider)
    {
        this.strideVersion = strideVersion;
        SetAsActiveCommand = new AnonymousCommand(ServiceProvider, () =>
        {
            strideVersion.UpdateLocalPackage(LocalPackage, null);
            if (LocalPackage is null)
            {
                // If it's a non installed version, offer same version for serverPackage so that it offers to install this specific version
                strideVersion.UpdateServerPackage(ServerPackage, null);
            }
            else
            {
                // Otherwise, offer its update: a release, or for a beta the newest build
                strideVersion.UpdateServerPackage(strideVersion.OfferedBuild(LocalPackage.Version), null);
            }

            strideVersion.Launcher.ActiveVersion = strideVersion;
        });
    }

    /// <summary>
    /// Gets the command that will set the associated version as active.
    /// </summary>
    public CommandBase SetAsActiveCommand { get; }

    /// <summary>
    /// Gets the name of this build in the alternate versions: the product and the version, without the package id, as
    /// the same version has a package for each Game Studio and the editor is chosen apart.
    /// </summary>
    public string FullName => LocalPackage is not null ? $"{strideVersion.PackageSimpleName} {Version} (installed)" : $"{strideVersion.PackageSimpleName} {Version}";

    public PackageVersion Version => LocalPackage?.Version ?? ServerPackage.Version;

    /// <summary>
    /// Gets whether this build is a beta, which has a badge in the alternate versions.
    /// </summary>
    public bool IsBeta => StrideVersionViewModel.IsBetaVersion(Version);

    internal void UpdateLocalPackage(NugetLocalPackage package)
    {
        OnPropertyChanging(nameof(FullName), nameof(Version));
        LocalPackage = package;
        OnPropertyChanged(nameof(FullName), nameof(Version));
    }

    internal void UpdateServerPackage(NugetServerPackage package)
    {
        OnPropertyChanging(nameof(FullName), nameof(Version));
        ServerPackage = package;
        OnPropertyChanged(nameof(FullName), nameof(Version));
    }
}
