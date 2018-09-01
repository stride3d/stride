// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;

using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    // TODO: Check if we can turn this into a SessionObjectViewModel
    public class ProfileViewModel : DispatcherViewModel
    {
        private readonly PackageProfile profile;
        private readonly SessionViewModel session;
        private Package package;

        public ProfileViewModel(SessionViewModel session, Package package, PackageProfile profile, PackageViewModel container)
            : base(session.ServiceProvider)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            this.session = session;
            this.package = package;
            this.profile = profile;

            Package = container;

            if (Package.Package.ProjectFullPath != null)
            {
                Projects.Add(new ProjectViewModel(this));
            }
        }

        /// <summary>
        /// Gets the platform of this profile, if defined.
        /// </summary>
        public PlatformType Platform => profile.Platform;

        public ObservableList<ProjectViewModel> Projects { get; } = new ObservableList<ProjectViewModel>();

        public PackageViewModel Package { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has executables projects.
        /// </summary>
        /// <value><c>true</c> if this instance has executables projects; otherwise, <c>false</c>.</value>
        public bool HasExecutables
        {
            get
            {
                return Projects.Any(model => model.Type == ProjectType.Executable);
            }
        }

        /// <summary>
        /// Updates the list of projects contained in this profile from the related <see cref="PackageProfile"/>.
        /// </summary>
        /// <remarks>The actions undertaken by this method are not cancellable via the action stack.</remarks>
        internal bool UpdateProjectList()
        {
            bool changed = false;

            if (Package.Package.ProjectFullPath != null && Projects.Count == 0)
            {
                Projects.Add(new ProjectViewModel(this));
                changed = true;
            }

            if (Package.Package.ProjectFullPath == null && Projects.Count != 0)
            {
                Projects[0].Delete();
                changed = true;
            }

            return changed;
        }
    }
}
