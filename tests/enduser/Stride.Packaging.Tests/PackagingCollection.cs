// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;

namespace Stride.Packaging.Tests;

// One collection, whose tests ParallelTestFramework runs at the same time
[CollectionDefinition("Packaging")]
public class PackagingCollection : ICollectionFixture<SharedPackagesFolderCleanup>;

/// <summary>Deletes <see cref="TestEnvironment.SharedPackagesFolder"/> once the collection is done.</summary>
public sealed class SharedPackagesFolderCleanup : IDisposable
{
    public void Dispose() => TestEnvironment.DeleteCaseDirectory(TestEnvironment.SharedPackagesFolder);
}
