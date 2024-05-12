// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class DirectoryBaseViewModel : SessionObjectViewModel
{
    protected DirectoryBaseViewModel(ISessionViewModel session)
        : base(session)
    {
    }

    public abstract DirectoryBaseViewModel Parent { get; set; }

    public abstract string Path { get; }

    public abstract MountPointViewModel Root { get; }
}
