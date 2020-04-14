// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Dirtiables;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    public class ProjectCodeViewModel : MountPointViewModel
    {
        public ProjectCodeViewModel(ProjectViewModel project)
            : base(project)
        {
        }

        public override string Name { get => "Code"; set => throw new NotImplementedException(); }

        public override bool IsEditable => false;

        public override string TypeDisplayName => "Project Code";

        public ProjectViewModel Project => (ProjectViewModel)Package;

        public override bool CanDelete(out string error)
        {
            error = "Projects can't be deleted from Game Studio.";
            return false;
        }

        public override bool AcceptAssetType(Type assetType)
        {
            return typeof(IProjectAsset).IsAssignableFrom(assetType);
        }

        protected override void UpdateIsDeletedStatus()
        {
            throw new NotImplementedException();
        }
    }

    // TODO: For the moment we consider that a project has only a single parent profile. Sharing project in several profile is not supported.
    public class ProjectViewModel : PackageViewModel
    {
        private bool isCurrentProject;

        public ProjectViewModel(SessionViewModel session, SolutionProject project, bool packageAlreadyInSession)
            : base(session, project, packageAlreadyInSession)
        {
            content.Add(Code = new ProjectCodeViewModel(this));
        }

        public SolutionProject Project => (SolutionProject)PackageContainer;

        public ProjectCodeViewModel Code { get; }

        public override string Name { get { return Project.Name; } set { if (value != Name) throw new InvalidOperationException("The name of a project cannot be set"); } }

        public UFile ProjectPath => Project.FullPath;

        // TODO CSPROJ=XKPKG
        public ProjectType Type => Project.Type;

        public PlatformType Platform => Project.Platform;

        /// <summary>
        /// Gets the generic type (either PlatformType or ProjectType)
        /// </summary>
        /// TODO: Remove this code as this is an ugly workaround
        public object GenericType => Platform != PlatformType.Shared ? (object)Platform : Type;

        public bool IsCurrentProject
        {
            get { return isCurrentProject; }
            internal set
            {
                SetValueUncancellable(ref isCurrentProject, value);

                // TODO: Check with Ben if this is the property place to put this?
                Package.Session.CurrentProject = isCurrentProject ? Project : null;
            }
        }

        /// <inheritdoc/>
        public override string TypeDisplayName => "Project";

        /// <summary>
        /// Gets the root namespace for this project.
        /// </summary>
        public string RootNamespace => Package.RootNamespace ?? Name;

        /// <summary>
        /// Gets asset directory view model for a given path and creates all missing parts.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="projectDirectory">Project directory path.</param>
        /// <param name="canUndoRedoCreation">True if register UndoRedo operation for missing path parts.</param>
        /// <returns>Given directory view model.</returns>
        [NotNull]
        public DirectoryBaseViewModel GetOrCreateProjectDirectory(string projectDirectory, bool canUndoRedoCreation)
        {
            DirectoryBaseViewModel result = Code;
            if (!string.IsNullOrEmpty(projectDirectory))
            {
                var directories = projectDirectory.Split(new[] { DirectoryBaseViewModel.Separator }, StringSplitOptions.RemoveEmptyEntries).Skip(1);
                result = directories.Aggregate(result, (current, next) => current.SubDirectories.FirstOrDefault(x => x.Name == next) ?? new DirectoryViewModel(next, current, canUndoRedoCreation));
            }

            return result;
        }
    }
}
