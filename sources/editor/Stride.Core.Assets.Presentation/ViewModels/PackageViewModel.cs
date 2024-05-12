// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
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
        get => PackagePath.GetFileNameWithoutExtension() ?? string.Empty;
        set { } // TODO rename
    }

    public Package Package => PackageContainer.Package;

    public PackageContainer PackageContainer { get; }

    public UFile PackagePath
    {
        get => Package.FullPath;
        set => SetValue(() => Package.FullPath = value);
    }

    public UDirectory RootDirectory => Package.RootDirectory;
}
