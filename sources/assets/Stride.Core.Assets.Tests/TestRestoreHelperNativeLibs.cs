// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Native libraries listed from a restore, as the asset compiler and Game Studio load them.
/// </summary>
public class TestRestoreHelperNativeLibs : IDisposable
{
    private const string NativeLibrary = "runtimes/win-x64/native/Assimp64.dll";

    private readonly string packageFolder = Path.Combine(Path.GetTempPath(), "StrideRestoreHelperTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void OurAssimpReplacesTheOneOfSilk()
    {
        // Both packages ship Assimp64.dll, and the last one registered would win
        var lockFile = CreateLockFile(("Ultz.Native.Assimp", "6.0.2"), ("Stride.Dependencies.Assimp", "6.0.5.2"));

        var libs = RestoreHelper.ListNativeLibs(lockFile);

        var lib = Assert.Single(libs);
        Assert.Contains("stride.dependencies.assimp", lib);
    }

    [Fact]
    public void SilkAssimpIsListedWithoutOurs()
    {
        var lockFile = CreateLockFile(("Ultz.Native.Assimp", "6.0.2"));

        var libs = RestoreHelper.ListNativeLibs(lockFile);

        var lib = Assert.Single(libs);
        Assert.Contains("ultz.native.assimp", lib);
    }

    public void Dispose()
    {
        if (Directory.Exists(packageFolder))
            Directory.Delete(packageFolder, true);
    }

    private LockFile CreateLockFile(params (string Name, string Version)[] packages)
    {
        var lockFile = new LockFile();
        lockFile.PackageFolders.Add(new LockFileItem(packageFolder));
        var target = new LockFileTarget();
        foreach (var (name, version) in packages)
        {
            // The package's folder must exist for its libraries to be listed
            var path = $"{name.ToLowerInvariant()}/{version}";
            Directory.CreateDirectory(Path.Combine(packageFolder, path));
            lockFile.Libraries.Add(new LockFileLibrary { Name = name, Version = NuGetVersion.Parse(version), Path = path });
            var library = new LockFileTargetLibrary { Name = name, Version = NuGetVersion.Parse(version) };
            library.NativeLibraries.Add(new LockFileItem(NativeLibrary));
            target.Libraries.Add(library);
        }
        lockFile.Targets.Add(target);
        return lockFile;
    }
}
