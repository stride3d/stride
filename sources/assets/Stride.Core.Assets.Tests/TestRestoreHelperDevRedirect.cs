// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Assemblies listed from a restore for a dev-redirect stub of build/redirect-packages.cs, as the router and the effect compiler service load them.
/// </summary>
public class TestRestoreHelperDevRedirect : IDisposable
{
    private const string Name = "Stride.ConnectionRouter";
    private const string LibAssembly = "lib/net10.0-windows7.0/Stride.ConnectionRouter.dll";

    private readonly string root = Path.Combine(Path.GetTempPath(), "StrideRestoreHelperTests", Guid.NewGuid().ToString("N"));

    private string CheckoutAssembly => Path.Combine(root, "checkout", "sources", "tools", Name, "bin", "Debug", "net10.0-windows", Name + ".dll");

    [Fact]
    public void TheCheckoutBuildReplacesTheLibOfTheStub()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CheckoutAssembly)!);
        File.WriteAllText(CheckoutAssembly, "");

        var assemblies = RestoreHelper.ListAssemblies(CreateLockFile());

        // The HintPath of a stub has '/' separators
        Assert.Equal(CheckoutAssembly, Path.GetFullPath(Assert.Single(assemblies)));
    }

    [Fact]
    public void TheLibOfTheStubLoadsWhenTheCheckoutBuildIsMissing()
    {
        var assemblies = RestoreHelper.ListAssemblies(CreateLockFile());

        var assembly = Assert.Single(assemblies);
        Assert.EndsWith(Path.Combine("lib", "net10.0-windows7.0", Name + ".dll"), assembly);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    private LockFile CreateLockFile()
    {
        var packages = Path.Combine(root, "packages");
        const string version = "4.5.0-dev";
        var path = $"{Name.ToLowerInvariant()}/{version}";
        var package = Path.Combine(packages, path);
        Directory.CreateDirectory(Path.Combine(package, "build"));
        File.WriteAllText(Path.Combine(package, "build", Name + ".props"), $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup>
                <StrideDevRedirect>true</StrideDevRedirect>
                <StrideDevRoot Condition="'$(StrideDevRoot)' == ''">{Path.Combine(root, "checkout")}</StrideDevRoot>
                <StrideDevConfiguration Condition="'$(StrideDevConfiguration)' == ''">Debug</StrideDevConfiguration>
              </PropertyGroup>
              <ItemGroup Condition="false">
                <Reference Include="{Name}">
                  <HintPath>$(StrideDevRoot)/sources/tools/{Name}/bin/$(StrideDevConfiguration)/net10.0-windows/{Name}.dll</HintPath>
                </Reference>
              </ItemGroup>
            </Project>
            """);

        var lockFile = new LockFile();
        lockFile.PackageFolders.Add(new LockFileItem(packages));
        lockFile.Libraries.Add(new LockFileLibrary { Name = Name, Version = NuGetVersion.Parse(version), Path = path });
        var library = new LockFileTargetLibrary { Name = Name, Version = NuGetVersion.Parse(version) };
        library.RuntimeAssemblies.Add(new LockFileItem(LibAssembly));
        var target = new LockFileTarget();
        target.Libraries.Add(library);
        lockFile.Targets.Add(target);
        return lockFile;
    }
}
