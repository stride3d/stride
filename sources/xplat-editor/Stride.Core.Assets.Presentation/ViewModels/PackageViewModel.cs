// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Presentation.ViewModels;

public class PackageViewModel : SessionObjectViewModel
{
    public PackageViewModel(ISessionViewModel session, PackageContainer packageContainer)
        : base(session)
    {
        PackageContainer = packageContainer;        
    }

    public override string Name
    {
        get => PackagePath.GetFileNameWithoutExtension();
        set { } // TODO rename
    }

    public Package Package => PackageContainer.Package;

    public PackageContainer PackageContainer { get; }

    public UFile PackagePath
    {
        get => Package.FullPath;
        set => SetProperty(Package.FullPath, value, x => Package.FullPath = x);
    }

    public UDirectory RootDirectory => Package.RootDirectory;
}
