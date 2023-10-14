// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Presentation.ViewModels;

public sealed class ProjectViewModel : PackageViewModel
{
    public ProjectViewModel(ISessionViewModel session, SolutionProject project)
        : base(session, project)
    {
    }

    public SolutionProject Project => (SolutionProject)PackageContainer;

    public UFile ProjectPath => Project.FullPath;
}
