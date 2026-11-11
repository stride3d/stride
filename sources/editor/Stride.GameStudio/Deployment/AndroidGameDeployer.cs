// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Translation;

namespace Stride.GameStudio.Deployment;

/// <summary>
/// The deploy step of the Android games: the build installs the game on the first device <c>adb</c> lists, then <c>adb</c> starts it.
/// </summary>
internal sealed class AndroidGameDeployer : IGameDeployer
{
    /// <summary>
    /// The target of the Android build that gives the name of the game's package.
    /// </summary>
    internal const string PackageTarget = "GetAndroidPackage";

    private readonly Func<IReadOnlyList<string>> listDeviceSerials;
    private readonly Func<string?> findAdb;
    private readonly Func<string, string, (int ExitCode, IReadOnlyList<string> Errors)> runAdb;
    // The device the last build installed the game on
    private string? deviceSerial;

    public AndroidGameDeployer()
        : this(() => AndroidDeviceEnumerator.ListAndroidDevices().Select(x => x.Serial).ToList(), AndroidDeviceEnumerator.GetAdbPath, RunAdb)
    {
    }

    /// <param name="listDeviceSerials">Lists the serial numbers of the devices <c>adb</c> sees.</param>
    /// <param name="findAdb">Finds <c>adb</c>; <c>null</c> when it is nowhere.</param>
    /// <param name="runAdb">Runs <c>adb</c> (its path, its arguments) and gives its exit code and error output.</param>
    internal AndroidGameDeployer(Func<IReadOnlyList<string>> listDeviceSerials, Func<string?> findAdb, Func<string, string, (int ExitCode, IReadOnlyList<string> Errors)> runAdb)
    {
        this.listDeviceSerials = listDeviceSerials;
        this.findAdb = findAdb;
        this.runAdb = runAdb;
    }

    /// <inheritdoc/>
    public async Task<GameProjectBuildRequest?> PrepareBuildAsync(ProjectViewModel project, GameProjectBuildRequest request, bool startProject, ILogger logger, CancellationToken cancellationToken)
    {
        var devices = await Task.Run(listDeviceSerials, cancellationToken);
        if (devices.Count == 0)
        {
            logger.Error(Tr._p("Message", "No Android device found for execution."));
            return null;
        }

        // For now, use first android device
        // TODO: Android device selector (together with platform selector)
        deviceSerial = devices[0];
        var properties = new Dictionary<string, string>(request.Properties)
        {
            ["AdbTarget"] = "-s " + deviceSerial,
        };
        // The build installs the game on the device, also for Build project
        return request with { Target = PackageTarget + ";Install", Properties = properties };
    }

    /// <inheritdoc/>
    public async Task<bool> DeployAsync(ProjectViewModel project, IGameProjectBuild build, ILogger logger, CancellationToken cancellationToken)
    {
        if (!build.TargetOutputs.TryGetValue(PackageTarget, out var packages) || packages.Count == 0)
        {
            logger.Error(string.Format(Tr._p("Message", "Couldn't find Android package name for {0}."), project.Name));
            return false;
        }
        var packageName = packages[0];

        var adbPath = await Task.Run(findAdb, cancellationToken);
        if (adbPath is null)
        {
            logger.Error(Tr._p("Message", @"Android tool ""adb"" couldn't found (no running process, in registry or on the PATH). Please add it to your PATH."));
            return false;
        }

        // On the device the game was installed on
        var device = deviceSerial is not null ? $"-s {deviceSerial} " : string.Empty;
        var (exitCode, errors) = await Task.Run(() => runAdb(adbPath, $@"{device}shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1"), cancellationToken);
        if (exitCode != 0)
        {
            logger.Error(string.Format(Tr._p("Message", "Can't run Android app with adb: {0}"), string.Join(Environment.NewLine, errors)));
            return false;
        }
        return true;
    }

    private static (int ExitCode, IReadOnlyList<string> Errors) RunAdb(string adbPath, string arguments)
    {
        var outputs = ShellHelper.RunProcessAndGetOutput(adbPath, arguments);
        return (outputs.ExitCode, outputs.OutputErrors);
    }
}
