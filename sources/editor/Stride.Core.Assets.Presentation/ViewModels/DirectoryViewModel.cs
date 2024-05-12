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

    public override DirectoryBaseViewModel Parent
    {
        get => parent;
        set => SetValue(ref parent, value);
    }

    public override string Name
    {
        get => name;
        set => SetValue(ref name, value);
    }

    public override string Path => Parent.Path + Name;

    public override MountPointViewModel Root => Parent.Root;
}
