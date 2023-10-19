// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Presentation.ViewModels;

public sealed class ProjectViewModel : PackageViewModel
{
    public ProjectViewModel(ISessionViewModel session, SolutionProject project)
        : base(session, project)
    {
        content.Add(Code = new ProjectCodeViewModel(this));
    }

    public ProjectCodeViewModel Code { get; }

    public SolutionProject Project => (SolutionProject)PackageContainer;

    public UFile ProjectPath => Project.FullPath;

    /// <summary>
    /// Gets asset directory view model for a given path and creates all missing parts.
    /// </summary>
    /// <param name="projectDirectory">Project directory path.</param>
    /// <returns>Given directory view model.</returns>
    public DirectoryBaseViewModel GetOrCreateProjectDirectory(string projectDirectory)
    {
        DirectoryBaseViewModel result = Code;
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            var directories = projectDirectory.Split(new[] { DirectoryBaseViewModel.Separator }, StringSplitOptions.RemoveEmptyEntries);
            result = directories.Aggregate(result, (current, next) => current.SubDirectories.FirstOrDefault(x => x.Name == next) ?? new DirectoryViewModel(next, current));
        }

        return result;
    }
}
