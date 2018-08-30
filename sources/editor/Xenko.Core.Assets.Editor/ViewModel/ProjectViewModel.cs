// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Dirtiables;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    // TODO: For the moment we consider that a project has only a single parent profile. Sharing project in several profile is not supported.
    public class ProjectViewModel : MountPointViewModel, IComparable<ProjectViewModel>
    {
        private readonly ProjectReference project;
        private bool isCurrentProject;

        public ProjectViewModel(ProjectReference project, ProfileViewModel container)
            : base(container.SafeArgument(nameof(container)).Package)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (container == null) throw new ArgumentNullException(nameof(container));
            Profile = container;
            this.project = project;

            // Use the property in order to create an action item
            InitialUndelete(false);
        }

        public override string Name { get { return project.Location.GetFileNameWithoutExtension(); } set { if (value != Name) throw new InvalidOperationException("The name of a project cannot be set"); } }

        public override string Path => Name + Separator;

        public Guid Id => project.Id;

        public UFile ProjectPath => project.Location;

        public ProjectType Type => project.Type;

        /// <summary>
        /// Gets the generic type (either PlatformType or ProjectType)
        /// </summary>
        /// TODO: Remove this code as this is an ugly workaround
        public object GenericType => Profile.Platform != PlatformType.Shared ? (object)Profile.Platform : Type;

        public ProfileViewModel Profile { get; }

        public bool IsCurrentProject { get { return isCurrentProject; } set { SetValueUncancellable(ref isCurrentProject, value); } }

        /// <inheritdoc/>
        public override bool IsEditable => Package.IsEditable && Type != ProjectType.Executable;

        /// <inheritdoc/>
        public override bool IsEditing { get { return false; } set { } }

        /// <inheritdoc/>
        public override string TypeDisplayName => "Project";

        /// <inheritdoc/>
        public override IEnumerable<IDirtiable> Dirtiables => base.Dirtiables.Concat(Profile.Package.Dirtiables);

        /// <summary>
        /// Gets the root namespace for this project.
        /// </summary>
        public string RootNamespace => project.RootNamespace ?? Package.Package.Meta.RootNamespace ?? Name;

        /// <inheritdoc/>
        public int CompareTo(ProjectViewModel other)
        {
            if (other == null)
                return -1;

            var result = Type.CompareTo(other.Type);
            return result != 0 ? result : string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool CanDelete(out string error)
        {
            error = "Projects can't be deleted from Game Studio.";
            return false;
        }

        public override void Delete()
        {
            IsDeleted = true;
        }

        public override bool AcceptAssetType(Type assetType)
        {
            return typeof(IProjectAsset).IsAssignableFrom(assetType);
        }

        protected override void UpdateIsDeletedStatus()
        {
            if (IsDeleted)
            {
                Profile.DeleteProject(project);
            }
            else
            {
                Profile.AddProject(this, project);
            }
        }
    }
}
