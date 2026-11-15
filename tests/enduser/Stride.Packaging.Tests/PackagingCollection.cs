// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;

namespace Stride.Packaging.Tests;

// Pack/build subprocesses are heavy and the in-process template generator holds shared state;
// run these tests serially.
[CollectionDefinition("Packaging", DisableParallelization = true)]
public class PackagingCollection : ICollectionFixture<RunPackagesFolderCleanup>;

/// <summary>Deletes <see cref="TestEnvironment.RunPackagesFolder"/> once the collection is done.</summary>
public sealed class RunPackagesFolderCleanup : IDisposable
{
    public void Dispose() => TestEnvironment.DeleteCaseDirectory(TestEnvironment.RunPackagesFolder);
}
