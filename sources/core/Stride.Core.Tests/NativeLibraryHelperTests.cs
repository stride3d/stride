// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Stride.Core.Tests;

public class NativeLibraryHelperTests
{
    private static readonly string LibraryExtension = OperatingSystem.IsWindows() ? ".dll" : OperatingSystem.IsMacOS() ? ".dylib" : ".so";

    [Fact]
    public void NativeLibrariesFlattenedNextToTheAssembliesAreRegistered()
    {
        // A build for a runtime identifier puts the native libraries next to the assemblies, not under runtimes/<rid>/native
        var directory = Path.Combine(Path.GetTempPath(), "StrideNativeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var nativeName = "stride_test_native_" + Guid.NewGuid().ToString("N");
            var nativePath = Path.Combine(directory, nativeName + LibraryExtension);
            File.WriteAllBytes(nativePath, [0]);
            var managedName = "Stride.Test.Managed." + Guid.NewGuid().ToString("N");
            File.Copy(typeof(NativeLibraryHelperTests).Assembly.Location, Path.Combine(directory, managedName + ".dll"));

            NativeLibraryHelper.RegisterDependenciesFrom(directory);

            Assert.Equal(nativePath, NativeLibraryHelper.LocateLibrary(nativeName, typeof(NativeLibraryHelperTests)));
            // A managed assembly shares the native extension on Windows, but is not a native library
            Assert.Throws<FileNotFoundException>(() => NativeLibraryHelper.LocateLibrary(managedName, typeof(NativeLibraryHelperTests)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void NativeLibrariesNextToTheAssembliesAreLookedUpWhenAskedFor()
    {
        // The directory is not listed at registration (that opened each of its DLLs): a library is searched by name
        var directory = Path.Combine(Path.GetTempPath(), "StrideNativeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            NativeLibraryHelper.RegisterDependenciesFrom(directory);

            var nativeName = "stride_test_native_" + Guid.NewGuid().ToString("N");
            var nativePath = Path.Combine(directory, nativeName + LibraryExtension);
            File.WriteAllBytes(nativePath, [0]);

            Assert.Equal(nativePath, NativeLibraryHelper.LocateLibrary(nativeName, typeof(NativeLibraryHelperTests)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
