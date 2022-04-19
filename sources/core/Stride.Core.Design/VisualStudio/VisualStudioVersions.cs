// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Stride.Core.VisualStudio
{
    public class IDEInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IDEInfo"/> class.
        /// </summary>
        /// <param name="installationVersion">The version of the VS instance.</param>
        /// <param name="displayName">The display name of the VS instance</param>
        /// <param name="installationPath">The path to the installation root of the VS instance.</param>
        /// <param name="instanceId">The unique identifier for this installation instance.</param>
        /// <param name="isComplete">Indicates whehter the VS instance is complete.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public IDEInfo(Version installationVersion, string displayName, string installationPath, string instanceId, bool isComplete = true)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            InstallationVersion = installationVersion ?? throw new ArgumentNullException(nameof(installationVersion));
            InstallationPath = installationPath ?? throw new ArgumentNullException(nameof(installationPath));
            InstanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
            IsComplete = isComplete;

            var idePath = Path.Combine(InstallationPath, "Common7", "IDE");
            DevenvPath = Path.Combine(idePath, "devenv.exe");
            if (!File.Exists(DevenvPath))
            {
                DevenvPath = null;
            }

            VsixInstallerPath = Path.Combine(idePath, "VSIXInstaller.exe");
            if (!File.Exists(VsixInstallerPath))
            {
                VsixInstallerPath = null;
            }

            VsixInstallationPath = ComputeVsixInstallationPath();
        }

        /// <summary>Gets a value indicating whether the instance is complete.</summary>
        /// <value>Whether the instance is complete.</value>
        /// <remarks>An instance is complete if it had no errors during install, resume, or repair.</remarks>
        public bool IsComplete { get; }

        /// <summary> 
        /// Gets the display name (title) of the product installed in this instance. 
        /// </summary>
        public string DisplayName { get; }

        /// <summary>Gets the version of the product installed in this instance.</summary>
        /// <value>The version of the product installed in this instance.</value>
        public Version InstallationVersion { get; }

        /// <summary>
        /// The path to the development environment executable of this IDE, or <c>null</c>.
        /// </summary>
        public string DevenvPath { get; }

        /// <summary>The root installation path of this IDE.</summary>
        /// <remarks>Can be empty but not <c>null</c>./remarks>
        public string InstallationPath { get; }

        /// <summary>
        /// The hex code for this installation instance. It is used, for example, to create a unique folder in %LocalAppData%
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// The path to the VSIX installer of this IDE, or <c>null</c>.
        /// </summary>
        public string VsixInstallerPath { get; }

        /// <summary>
        /// The package names and versions of packages installed to this instance.
        /// </summary>
        /// <value></value>
        public Dictionary<string, string> PackageVersions { get; } = new Dictionary<string, string>();

        /// <summary>
        /// <c>true</c> if this IDE has a development environment; otherwise, <c>false</c>.
        /// </summary>
        public bool HasDevenv => !string.IsNullOrEmpty(DevenvPath);

        /// <summary>
        /// <c>true</c> if this IDE has a VSIX installer; otherwise, <c>false</c>.
        /// </summary>
        public bool HasVsixInstaller => !string.IsNullOrEmpty(VsixInstallerPath);

        /// <summary>
        /// Thd path to the current user's Visual Studio extension installation directory.
        /// </summary>
        public string VsixInstallationPath { get; }

        /// <summary>
        /// Computes the installation path for Visual Studio Extensions for the current instance. 
        /// This will always be the per-user location.
        /// </summary>
        /// <returns>The full path to the VSIX installation folder for this instance.</returns>
        private string ComputeVsixInstallationPath()
        {
            // For example: C:\Users\LoggedInUserName\AppData\Local\Microsoft\VisualStudio\17.0_88b6650b\Extensions
            var vsixInstallationPath = new StringBuilder(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 90);
            vsixInstallationPath.Append(@"\Microsoft\VisualStudio\");
            vsixInstallationPath.Append(InstallationVersion.Major.ToString());
            vsixInstallationPath.Append(".0_");
            vsixInstallationPath.Append(InstanceId);
            vsixInstallationPath.Append(@"\Extensions\");

            return vsixInstallationPath.ToString();
        }

        /// <inheritdoc />
        public override string ToString() => DisplayName;
    }

    public static class VisualStudioVersions
    {
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
        private static readonly Lazy<List<IDEInfo>> IDEInfos = new Lazy<List<IDEInfo>>(BuildIDEInfos);

        public static IDEInfo DefaultIDE = new IDEInfo(new Version("0.0"), "Default IDE", string.Empty, string.Empty);

        /// <summary>
        /// Only lists VS2019+ (previous versions are not supported due to lack of buildTransitive targets).
        /// </summary>
        public static IEnumerable<IDEInfo> AvailableVisualStudioInstances => IDEInfos.Value.Where(x => x.InstallationVersion.Major >= 16 && x.HasDevenv);

        private static List<IDEInfo> BuildIDEInfos()
        {
            var ideInfos = new List<IDEInfo>();

            // Visual Studio 16.0 (2019) and later
            try
            {
                var setupInstancesEnum = new SetupConfiguration().EnumAllInstances();
                setupInstancesEnum.Reset();
                var inst = new ISetupInstance[1];

                while (true)
                {
                    setupInstancesEnum.Next(1, inst, out int numFetched);
                    if (numFetched <= 0)
                        break;

                    try
                    {
                        var setupInstance2 = inst[0] as ISetupInstance2;
                        if (setupInstance2 == null)
                            continue;

                        // Only examine VS2019+
                        if (!Version.TryParse(setupInstance2.GetInstallationVersion(), out var installationVersion)
                            || installationVersion.Major < 16)
                            continue;

                        var displayName = setupInstance2.GetDisplayName();
                        // Try to append nickname (if any)
                        try
                        {
                            var nickname = setupInstance2.GetProperties().GetValue("nickname") as string;
                            if (!string.IsNullOrEmpty(nickname))
                                displayName = $"{displayName} ({nickname})";
                            else
                            {
                                var installationName = setupInstance2.GetInstallationName();
                                // In case of Preview, we have:
                                // "installationName": "VisualStudioPreview/16.4.0-pre.6.0+29519.161"
                                // "channelId": "VisualStudio.16.Preview"
                                if (installationName.Contains("Preview"))
                                {
                                    displayName += " (Preview)";
                                }
                            }
                        }
                        catch (COMException)
                        {
                        }

                        try
                        {
                            var minimumRequiredState = InstanceState.Local | InstanceState.Registered;
                            if ((setupInstance2.GetState() & minimumRequiredState) != minimumRequiredState)
                                continue;
                        }
                        catch (COMException)
                        {
                            continue;
                        }

                        var ideInfo = new IDEInfo(installationVersion, displayName,
                            setupInstance2.GetInstallationPath(), setupInstance2.GetInstanceId(), setupInstance2.IsComplete());

                        // Fill packages
                        foreach (var package in setupInstance2.GetPackages())
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
