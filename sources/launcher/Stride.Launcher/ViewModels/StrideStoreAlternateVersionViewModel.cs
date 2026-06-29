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

    public StrideStoreAlternateVersionViewModel(StrideStoreVersionViewModel strideVersion)
        : base(strideVersion.ServiceProvider)
    {
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
                // Otherwise, offer latest version for update
                strideVersion.UpdateServerPackage(strideVersion.LatestServerPackage, null);
            }

            strideVersion.Launcher.ActiveVersion = strideVersion;
        });
    }

    /// <summary>
    /// Gets the command that will set the associated version as active.
    /// </summary>
    public CommandBase SetAsActiveCommand { get; }

    public string FullName
    {
        get
        {
            return LocalPackage is not null ? $"{LocalPackage.Id} {LocalPackage.Version} (installed)" : $"{ServerPackage.Id} {ServerPackage.Version}";
        }
    }

    public PackageVersion Version => LocalPackage?.Version ?? ServerPackage.Version;

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
