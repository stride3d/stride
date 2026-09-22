// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Xunit;

namespace Stride.CrashReport.Tests;

public class ReporterStoreTests
{
    [Fact]
    public void StoreLookupTakesTheExactVersionOnly()
    {
        // A host runs the reporter its package depends on, never a newer or older one left in the store by another
        // engine: the host/reporter command line is a per-version contract.
        var store = Directory.CreateTempSubdirectory("stride-reporter-store-").FullName;
        try
        {
            foreach (var version in new[] { "4.4.0", "5.0.0", "5.0.2", "5.1.0-beta1" })
                File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(store, "stride.crashreporter", version, "tools")).FullName, "Stride.CrashReporter.dll"), "");

            var found = NativeCrashReporting.FindReporterInStore(store, "5.0.0");
            Assert.NotNull(found);
            Assert.Equal(Path.Combine(store, "stride.crashreporter", "5.0.0", "tools"), Path.GetDirectoryName(found));

            // The store path is lower-case whatever the version string's case.
            Assert.Equal(found, NativeCrashReporting.FindReporterInStore(store, "5.0.0"));
            Assert.Equal(Path.Combine(store, "stride.crashreporter", "5.1.0-beta1", "tools"),
                Path.GetDirectoryName(NativeCrashReporting.FindReporterInStore(store, "5.1.0-BETA1")));

            Assert.Null(NativeCrashReporting.FindReporterInStore(store, "5.0.1"));
            Assert.Null(NativeCrashReporting.FindReporterInStore(store, null));
            Assert.Null(NativeCrashReporting.FindReporterInStore(store, ""));
        }
        finally
        {
            Directory.Delete(store, recursive: true);
        }
    }

    [Fact]
    public void LibraryCarriesTheVersionItWasBuiltFor()
    {
        // Baked by the csproj from StrideCrashReporterVersion.props, with the worktree suffix a dev build gives the
        // reporter package itself. Without it no reporter is ever resolved from the store.
        var version = NativeCrashReporting.ReporterVersion;
        Assert.False(string.IsNullOrEmpty(version));
        Assert.Matches(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$", version);
    }
}
