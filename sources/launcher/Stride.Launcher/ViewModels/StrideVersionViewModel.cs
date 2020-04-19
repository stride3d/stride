// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Packages;
using Stride.Core.Presentation.Commands;

namespace Stride.LauncherApp.ViewModels
{
    /// <summary>
    /// An implementation of the <see cref="PackageVersionViewModel"/> that represents a major version of Stride.
    /// </summary>
    internal abstract class StrideVersionViewModel : PackageVersionViewModel, IComparable<StrideVersionViewModel>, IComparable<Tuple<int, int>>
    {
        private bool isVisible;
        private bool canStart;

        internal StrideVersionViewModel(LauncherViewModel launcher, NugetStore store, NugetLocalPackage localPackage, string packageId, int major, int minor)
            : base(launcher, store, localPackage)
        {
            PackageSimpleName = packageId.Replace(".GameStudio", string.Empty);
            Major = major;
            Minor = minor;
            SetAsActiveCommand = new AnonymousCommand(ServiceProvider, () => launcher.ActiveVersion = this);
            // Update status if the user changes whether to display beta versions.
            launcher.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LauncherViewModel.ShowBetaVersions)) UpdateStatus(); };
        }

        public string PackageSimpleName { get; }

        /// <summary>
        /// Gets the major number of this version.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor number of this version.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Gets the name of this version.
        /// </summary>
        public override string Name => GetName(PackageSimpleName, Major, Minor);

        /// <summary>
        /// Gets the display name of this version.
        /// </summary>
        public virtual string DisplayName => GetName(PackageSimpleName, Major, Minor, true);

        /// <summary>
        /// Gets the command that will set the associated package as active.
        /// </summary>
        public CommandBase SetAsActiveCommand { get; }

        /// <summary>
        /// Gets whether this version is a beta version.
        /// </summary>
        public bool IsBeta => IsBetaVersion(Major, Minor);

        /// <summary>
        /// Gets whether this version should be displayed.
        /// </summary>
        public bool IsVisible { get { return isVisible; } private set { SetValue(ref isVisible, value); } }

        /// <summary>
        /// Gets whether this version can be started.
        /// </summary>
        public bool CanStart { get { return canStart; } private set { SetValue(ref canStart, value); } }

        /// <summary>
        /// Builds a string that represents the given version numbers.
        /// </summary>
        /// <param name="majorVersion">The major version number.</param>
        /// <param name="minorVersion">The minor version number.</param>
        /// <param name="isDisplayName">Indicates whether the name to compute is a display name, or a string token used to build urls.</param>
        /// <returns>A string representing the given version numbers.</returns>
        public static string GetName(string packageSimpleName, int majorVersion, int minorVersion, bool isDisplayName = false)
        {
            if (isDisplayName && IsBetaVersion(majorVersion, minorVersion))
                return $"{packageSimpleName} {majorVersion}.{minorVersion}-beta";

            return $"{packageSimpleName} {majorVersion}.{minorVersion}";
        }
        
        /// <summary>
        /// Indicates if the given version corresponds to a beta version.
        /// </summary>
        /// <param name="majorVersion">The major number of the version.</param>
        /// <param name="minorVersion">The minor nimber of the version.</param>
        /// <returns>True if the given version is a beta, false otherwise.</returns>
        public static bool IsBetaVersion(int majorVersion, int minorVersion)
        {
            return majorVersion < 3;
        }

        /// <inheritdoc/>
        protected override void UpdateStatus()
        {
            base.UpdateStatus();
            // It is visible if it's installed, or if it's not a beta, or if user want to see be available betas
            IsVisible = Launcher.ShowBetaVersions || !IsBeta || CanDelete;
            SetAsActiveCommand.IsEnabled = CanDelete;
            DeleteCommand.IsEnabled = CanDelete;
            CanStart = CanDelete;
            if (Launcher.ActiveVersion == this)
                Launcher.StartStudioCommand.IsEnabled = CanStart;
        }

        public int CompareTo(StrideVersionViewModel other)
        {
            var r = Major.CompareTo(other.Major);
            return r != 0 ? -r : -Minor.CompareTo(other.Minor);
        }

        public int CompareTo(Tuple<int, int> other)
        {
            var r = Major.CompareTo(other.Item1);
            return r != 0 ? -r : -Minor.CompareTo(other.Item2);
        }
    }
}
