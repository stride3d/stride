// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Packaging.Tests;

// Pack/build subprocesses are heavy and the in-process template generator holds shared state;
// run these tests serially.
[CollectionDefinition("Packaging", DisableParallelization = true)]
public class PackagingCollection;
