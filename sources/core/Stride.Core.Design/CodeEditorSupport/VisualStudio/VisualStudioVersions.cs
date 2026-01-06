// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Stride.Core.CodeEditorSupport.VisualStudio;

public static class VisualStudioVersions
{
    private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
    private static readonly Lazy<List<IDEInfo>> IDEInfos = new(BuildIDEInfos);

    /// <summary>
    /// Only lists VS2019+ (previous versions are not supported due to lack of buildTransitive targets).
    /// </summary>
    public static IEnumerable<IDEInfo> AvailableInstances => IDEInfos.Value.Where(x => x.InstallationVersion?.Major >= 16 && x.HasProgram);

    private static List<IDEInfo> BuildIDEInfos()
    {
        List<IDEInfo> instances = [];
        
        if (!OperatingSystem.IsWindows())
            return instances;

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
                    if (inst[0] is not ISetupInstance2 setupInstance2)
                        continue;

                    // Only examine VS2019+
                    if (!Version.TryParse(setupInstance2.GetInstallationVersion(), out var installationVersion)
                        || installationVersion.Major < 16)
                    {
                        continue;
                    }

                    var displayName = setupInstance2.GetDisplayName();
                    // Try to append nickname (if any)
                    try
                    {
                        var nickname = setupInstance2.GetProperties().GetValue("nickname") as string;
                        if (!string.IsNullOrEmpty(nickname))
                        {
                            displayName = $"{displayName} ({nickname})";
                        }
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
                        const InstanceState minimumRequiredState = InstanceState.Local | InstanceState.Registered;
                        if ((setupInstance2.GetState() & minimumRequiredState) != minimumRequiredState)
                            continue;
                    }
                    catch (COMException)
                    {
                        continue;
                    }
                    
                    var idePath = Path.Combine(setupInstance2.GetInstallationPath(), "Common7", "IDE");
                    var programPath = Path.Combine(idePath, "devenv.exe");
                    if (!File.Exists(programPath))
                    {
                        programPath = null;
                    }

                    var vsixInstallerPath = Path.Combine(idePath, "VSIXInstaller.exe");
                    if (!File.Exists(vsixInstallerPath))
                    {
                        vsixInstallerPath = null;
                    }

                    var ideInfo = new IDEInfo(displayName,
                        programPath, IDEType.VisualStudio, installationVersion) { VsixInstallerPath = vsixInstallerPath };

                    // Fill packages
                    foreach (var package in setupInstance2.GetPackages())
                    {
                        ideInfo.PackageVersions[package.GetId()] = package.GetVersion();
                    }

                    instances.Add(ideInfo);
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
        return instances;
    }
}
