// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;

[assembly: AssemblyFixture(typeof(Stride.Packaging.Tests.SharedPackagesFolderCleanup))]
// The generator and the sessions report on the console: it goes to the output of the test they run for
[assembly: CaptureConsole]

namespace Stride.Packaging.Tests;

/// <summary>Deletes <see cref="TestEnvironment.SharedPackagesFolder"/> once the tests are done.</summary>
public sealed class SharedPackagesFolderCleanup : IDisposable
{
    public void Dispose() => TestEnvironment.DeleteCaseDirectory(TestEnvironment.SharedPackagesFolder);
}
