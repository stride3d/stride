// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public sealed class DirectoryViewModel : DirectoryBaseViewModel
{
    private string name;
    private DirectoryBaseViewModel parent;

    public DirectoryViewModel(string name, DirectoryBaseViewModel parent)
        : base(parent.Session)
    {
        this.name = name;
        this.parent = parent;
    }

    /// <summary>
    /// Gets the package containing this directory.
    /// </summary>
    public override PackageViewModel Package => Parent.Package;

    /// <summary>
    /// Gets or sets the parent directory of this directory.
    /// </summary>
    public override DirectoryBaseViewModel Parent
    {
        get => parent;
        set => SetValue(ref parent, value);
    }

    /// <summary>
    /// Gets or sets the name of this directory.
    /// </summary>
    public override string Name
    {
        get => name;
        set => SetValue(ref name, value);
    }

    /// <summary>
    /// Gets the path of this directory in its current package.
    /// </summary>
    public override string Path => Parent.Path + Name;

    /// <inheritdoc/>
    public override MountPointViewModel Root => Parent.Root;
}
