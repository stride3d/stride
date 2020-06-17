// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Stride.Core.VisualStudio
{
    public class IDEInfo
    {
        public IDEInfo(Version version, string displayName, string installationPath, bool complete = true)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            Complete = complete;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Version = version;
            InstallationPath = installationPath ?? throw new ArgumentNullException(nameof(installationPath));
        }

        public bool Complete { get; }

        public string DisplayName { get; }

        public Version Version { get; }

        /// <summary>
        /// The path to the build tools of this IDE, or <c>null</c>.
        /// </summary>
        public string BuildToolsPath { get; internal set; }

        /// <summary>
        /// The path to the development environment executable of this IDE, or <c>null</c>.
        /// </summary>
        public string DevenvPath { get; internal set; }

        /// <summary>
        /// The root installation path of this IDE.
        /// </summary>
        /// <remarks>
        /// Can be empty but not <c>null</c>.
        /// </remarks>
        public string InstallationPath { get; }

        /// <summary>
        /// The path to the VSIX installer of this IDE, or <c>null</c>.
        /// </summary>
        public string VsixInstallerPath { get; internal set; }

        public VSIXInstallerVersion VsixInstallerVersion { get; internal set; }

        public Dictionary<string, string> PackageVersions { get; } = new Dictionary<string, string>();

        /// <summary>
        /// <c>true</c> if this IDE has integrated build tools; otherwise, <c>false</c>.
        /// </summary>
        public bool HasBuildTools => !string.IsNullOrEmpty(BuildToolsPath);

        /// <summary>
        /// <c>true</c> if this IDE has a development environment; otherwise, <c>false</c>.
        /// </summary>
        public bool HasDevenv => !string.IsNullOrEmpty(DevenvPath);

        /// <summary>
        /// <c>true</c> if this IDE has a VSIX installer; otherwise, <c>false</c>.
        /// </summary>
        public bool HasVsixInstaller => !string.IsNullOrEmpty(VsixInstallerPath) && VsixInstallerVersion != VSIXInstallerVersion.None;

        /// <inheritdoc />
        public override string ToString() => DisplayName;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum VSIXInstallerVersion
    {
        None,
        VS2019AndFutureVersions,
    }

    public static class VisualStudioVersions
    {
        // ReSharper disable once InconsistentNaming
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
        private static Lazy<List<IDEInfo>> IDEInfos = new Lazy<List<IDEInfo>>(BuildIDEInfos);

        public static IDEInfo DefaultIDE = new IDEInfo(new Version("0.0"), "Default IDE", string.Empty);

        /// <summary>
        /// Only lists VS2019+ (previous versions are not supported due to lack of buildTransitive targets).
        /// </summary>
        public static IEnumerable<IDEInfo> AvailableVisualStudioInstances => IDEInfos.Value.Where(x => x.Version.Major >= 16 && x.HasDevenv);

        /// <summary>
        /// List all versions of VS2017+ (might be needed by installer process).
        /// </summary>
        public static IEnumerable<IDEInfo> AllAvailableVisualStudioInstances => IDEInfos.Value.Where(x => x.Version.Major >= 16 && x.HasDevenv);

        public static IEnumerable<IDEInfo> AvailableBuildTools => IDEInfos.Value.Where(x => x.HasBuildTools);

        public static void Refresh()
        {
            IDEInfos = new Lazy<List<IDEInfo>>(BuildIDEInfos);
        }

        private static List<IDEInfo> BuildIDEInfos()
        {
            var ideInfos = new List<IDEInfo>();

            // Visual Studio 15.0 (2017) and later
            try
            {
                var configuration = new SetupConfiguration();

                var instances = configuration.EnumAllInstances();
                instances.Reset();
                var inst = new ISetupInstance[1];

                while (true)
                {
                    instances.Next(1, inst, out int pceltFetched);
                    if (pceltFetched <= 0)
                        break;

                    try
                    {
                        var inst2 = inst[0] as ISetupInstance2;
                        if (inst2 == null)
                            continue;

                        // Only deal with VS2019+
                        if (!Version.TryParse(inst2.GetInstallationVersion(), out var version)
                            || version.Major < 15)
                            continue;

                        var installationPath = inst2.GetInstallationPath();
                        var buildToolsPath = Path.Combine(installationPath, "MSBuild", "Current", "Bin");
                        if (!Directory.Exists(buildToolsPath))
                            buildToolsPath = null;
                        var idePath = Path.Combine(installationPath, "Common7", "IDE");
                        var devenvPath = Path.Combine(idePath, "devenv.exe");
                        if (!File.Exists(devenvPath))
                            devenvPath = null;
                        var vsixInstallerPath = Path.Combine(idePath, "VSIXInstaller.exe");
                        if (!File.Exists(vsixInstallerPath))
                            vsixInstallerPath = null;

                        var displayName = inst2.GetDisplayName();
                        // Try to append nickname (if any)
                        try
                        {
                            var nickname = inst2.GetProperties().GetValue("nickname") as string;
                            if (!string.IsNullOrEmpty(nickname))
                                displayName = $"{displayName} ({nickname})";
                            else
                            {
                                var installationName = inst2.GetInstallationName();
                                // In case of Preview, we have:
                                // "installationName": "VisualStudioPreview/16.4.0-pre.6.0+29519.161"
                                // "channelId": "VisualStudio.16.Preview"
                                if (installationName.Contains("Preview"))
                                {
                                    displayName = displayName + " (Preview)";
                                }
                            }
                        }
                        catch (COMException)
                        {
                        }

                        try
                        {
                            var minimumRequiredState = InstanceState.Local | InstanceState.Registered;
                            if ((inst2.GetState() & minimumRequiredState) != minimumRequiredState)
                                continue;
                        }
                        catch (COMException)
                        {
                            continue;
                        }

                        var ideInfo = new IDEInfo(version, displayName, installationPath, inst2.IsComplete())
                        {
                            BuildToolsPath = buildToolsPath,
                            DevenvPath = devenvPath,
                            VsixInstallerVersion = VSIXInstallerVersion.VS2019AndFutureVersions,
                            VsixInstallerPath = vsixInstallerPath,
                        };

                        // Fill packages
                        foreach (var package in inst2.GetPackages())
                        {
                            ideInfo.PackageVersions[package.GetId()] = package.GetVersion();
                        }

                        ideInfos.Add(ideInfo);
                    }
                    catch (Exception)
                    {
                        // Something might have happened inside Visual Studio Setup code (had FileNotFoundException in GetInstallationPath() for example)
                        // Let's ignore this instance
                    }
                }
            }
            catch (COMException comException) when (comException.HResult == REGDB_E_CLASSNOTREG)
            {
                // COM is not registered. Assuming no instances are installed.
            }
            return ideInfos;
        }
    }
}
