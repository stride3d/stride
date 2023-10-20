// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public sealed class AssetMountPointViewModel : MountPointViewModel
{
    public AssetMountPointViewModel(PackageViewModel package)
        : base(package)
    {
    }

    public override string Name
    {
        get => "Assets";
        set => throw new InvalidOperationException($"Cannot change the name of a {nameof(AssetMountPointViewModel)}");
    }
}
