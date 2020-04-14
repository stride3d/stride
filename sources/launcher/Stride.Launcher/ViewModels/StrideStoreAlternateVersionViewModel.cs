using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Packages;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.LauncherApp.ViewModels
{
    internal sealed class XenkoStoreAlternateVersionViewModel : DispatcherViewModel
    {
        private XenkoStoreVersionViewModel xenkoVersion;
        internal NugetServerPackage ServerPackage;
        internal NugetLocalPackage LocalPackage;

        public XenkoStoreAlternateVersionViewModel([NotNull] XenkoStoreVersionViewModel xenkoVersion)
            : base(xenkoVersion.ServiceProvider)
        {
            this.xenkoVersion = xenkoVersion;

            SetAsActiveCommand = new AnonymousCommand(ServiceProvider, () =>
            {
                xenkoVersion.UpdateLocalPackage(LocalPackage, null);
                if (LocalPackage == null)
                {
                    // If it's a non installed version, offer same version for serverPackage so that it offers to install this specific version
                    xenkoVersion.UpdateServerPackage(ServerPackage, null);
                }
                else
                {
                    // Otherwise, offer latest version for update
                    xenkoVersion.UpdateServerPackage(xenkoVersion.LatestServerPackage, null);
                }

                xenkoVersion.Launcher.ActiveVersion = xenkoVersion;
            });
        }

        /// <summary>
        /// Gets the command that will set the associated version as active.
        /// </summary>
        public CommandBase SetAsActiveCommand { get; }

        public string FullName
        {
            get
            {
                if (LocalPackage != null)
                    return $"{LocalPackage.Id} {LocalPackage.Version} (installed)";
                return $"{ServerPackage.Id} {ServerPackage.Version}";
            }
        }

        public PackageVersion Version => LocalPackage?.Version ?? ServerPackage.Version;

        internal void UpdateLocalPackage(NugetLocalPackage package)
        {
            OnPropertyChanging(nameof(FullName), nameof(Version));
            LocalPackage = package;
            OnPropertyChanged(nameof(FullName), nameof(Version));
        }

        internal void UpdateServerPackage(NugetServerPackage package)
        {
            OnPropertyChanging(nameof(FullName), nameof(Version));
            ServerPackage = package;
            OnPropertyChanged(nameof(FullName), nameof(Version));
        }
    }
}
