// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class MountPointViewModel : DirectoryBaseViewModel
{
    protected MountPointViewModel(PackageViewModel package)
        : base(package.Session)
    {
        Package = package;
    }

    public override PackageViewModel Package { get; }

    public override DirectoryBaseViewModel Parent
    {
        get => null!;
        set => throw new InvalidOperationException($"Cannot change the parent of a {nameof(MountPointViewModel)}");
    }

    public override string Name
    {
        get => string.Empty;
        set => throw new InvalidOperationException($"Cannot change the name of a {nameof(MountPointViewModel)}");
    }

    public override string Path => string.Empty;

    public override MountPointViewModel Root => this;
}