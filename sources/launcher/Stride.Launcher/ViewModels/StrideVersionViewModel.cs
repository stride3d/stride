// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.LauncherApp.Services;

namespace Stride.LauncherApp.ViewModels
{
    /// <summary>
    /// An implementation of the <see cref="PackageVersionViewModel"/> that represents a major version of Stride.
    /// </summary>
    internal abstract class StrideVersionViewModel : PackageVersionViewModel, IComparable<StrideVersionViewModel>, IComparable<Tuple<int, int>>
    {
        public const string MainExecutables = @"lib\net472\Stride.GameStudio.exe,lib\net472\Xenko.GameStudio.exe,Bin\Windows\Xenko.GameStudio.exe,Bin\Windows-Direct3D11\Xenko.GameStudio.exe";

        private bool isVisible;
        private bool canStart;
        private string selectedFramework;

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

        protected void UpdateFrameworks()
        {
            Frameworks.Clear();
            if (LocalPackage != null && InstallPath != null)
            {
                var libDirectory = Path.Combine(InstallPath, "lib");
                var frameworks = Directory.EnumerateDirectories(libDirectory);
                foreach (var frameworkPath in frameworks)
                {
                    var frameworkFolder = new DirectoryInfo(frameworkPath).Name;
                    if (File.Exists(Path.Combine(frameworkPath, "Stride.GameStudio.exe"))
                        || File.Exists(Path.Combine(frameworkPath, "Xenko.GameStudio.exe")))
                    {
                        Frameworks.Add(frameworkFolder);
                    }
                }

                if (Frameworks.Count > 0)
                {
                    try
                    {
                        // If preferred framework exists in our list, select it
                        var preferredFramework = LauncherSettings.PreferredFramework;
                        if (Frameworks.Contains(preferredFramework))
                            SelectedFramework = preferredFramework;
                        else
                        {
                            // Otherwise, try to find a framework of the same kind (.NET Core or .NET Framework)
                            var nugetFramework = NuGetFramework.ParseFolder(preferredFramework);
                            SelectedFramework =
                                Frameworks.FirstOrDefault(x => NuGetFramework.ParseFolder(preferredFramework).Framework == nugetFramework.Framework)
                                ?? Frameworks.First(); // otherwise fallback to first choice
                        }
                    }
                    catch
                    {
                        SelectedFramework = Frameworks.First();
                    }
                }
            }
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

        public ObservableList<string> Frameworks { get; } = new ObservableList<string>();

        public string SelectedFramework { get { return selectedFramework; } set { SetValue(ref selectedFramework, value); } }

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

        /// <summary>
        /// Name of main executable of current store.
        /// </summary>
        /// <returns>Name of the executable.</returns>
        public string GetMainExecutables()
        {
            return MainExecutables;
        }

        /// <summary>
        /// Locate the main executable from a given package installation path. It throws exceptions if not found.
        /// </summary>
        /// <param name="packagePath">The package installation path.</param>
        /// <returns>The main executable.</returns>
        public string LocateMainExecutable()
        {
            // First, try to use the selected framework
            if (SelectedFramework != null)
            {
                var gameStudioDirectory = Path.Combine(InstallPath, "lib", SelectedFramework);
                foreach (var gameStudioExecutable in new[] { "Stride.GameStudio.exe", "Xenko.GameStudio.exe" })
                {
                    var gameStudioPath = Path.Combine(gameStudioDirectory, gameStudioExecutable);
                    if (File.Exists(gameStudioPath))
                        return gameStudioPath;
                }
            }

            // Otherwise, old-style fallback
            var mainExecutableList = GetMainExecutables();
            var fullExePath = mainExecutableList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Path.Combine(InstallPath, x)).FirstOrDefault(File.Exists);
            if (fullExePath == null)
                throw new InvalidOperationException("Unable to locate the executable for the selected version");

            return fullExePath;
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
