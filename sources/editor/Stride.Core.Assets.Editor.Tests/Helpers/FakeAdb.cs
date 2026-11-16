// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// An <c>adb</c> that lists the devices it is given and records the commands it runs, all of which succeed; the Android deploy step
/// of the editor runs on it (<see cref="CreateDeployer"/>).
/// </summary>
/// <param name="devices">The serial numbers of the devices it lists, in order.</param>
public sealed class FakeAdb(params string[] devices)
{
    private readonly Lock gate = new();
    private readonly List<(string Path, string Arguments)> runs = [];

    /// <summary>
    /// The path the fake gives as the one of <c>adb</c>.
    /// </summary>
    public const string Path = "/android-sdk/platform-tools/adb";

    /// <summary>
    /// The commands run so far: the path of <c>adb</c> and its arguments.
    /// </summary>
    public IReadOnlyList<(string Path, string Arguments)> Runs
    {
        get
        {
            lock (gate)
                return [.. runs];
        }
    }

    /// <summary>
    /// Creates the Android deploy step of the editor, on this <c>adb</c>.
    /// </summary>
    public IGameDeployer CreateDeployer() => new AndroidGameDeployer(() => devices, () => Path, Run);

    private (int ExitCode, IReadOnlyList<string> Errors) Run(string path, string arguments)
    {
        lock (gate)
            runs.Add((path, arguments));
        return (0, []);
    }
}
