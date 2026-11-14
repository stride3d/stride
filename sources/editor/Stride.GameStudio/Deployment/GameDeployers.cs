// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Core;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Deployment;

/// <summary>
/// The deploy steps of Game Studio: an Android game goes to a device with <c>adb</c>, a Linux or macOS game to a remote host. A
/// Windows game starts on this machine; an iOS game is only built.
/// </summary>
internal static class GameDeployers
{
    /// <summary>
    /// Registers the deploy steps of Game Studio on its build and run.
    /// </summary>
    public static void Register(BuildAndRunViewModel buildAndRun, IViewModelServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(buildAndRun);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        buildAndRun.RegisterDeployer(PlatformType.Android, new AndroidGameDeployer());
        var remote = new RemoteGameDeployer(serviceProvider);
        buildAndRun.RegisterDeployer(PlatformType.Linux, remote);
        buildAndRun.RegisterDeployer(PlatformType.macOS, remote);
    }
}
