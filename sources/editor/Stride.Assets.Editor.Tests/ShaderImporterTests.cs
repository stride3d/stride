// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Build;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;

namespace Stride.Assets.Editor.Tests;

public sealed class ShaderImporterTests
{
    [Fact]
    public async Task APackageWithoutShadersYieldsOnlyTheCacheUpdateStep()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Prefab", new PrefabAsset()));
        var importer = new StrideShaderImporter();

        // Every package not imported yet is enumerated (the system packages are those of the session on the first pass): no shader to
        // import, only the step that marks them imported
        var systemSteps = importer.CreateSystemShaderBuildSteps(fixture.Session);
        Assert.NotNull(systemSteps);
        Assert.Equal("UpdateImportShaderCacheBuildStep", Assert.Single(systemSteps!.Steps).GetType().Name);

        // The user packages likewise, with the step that refreshes the import cache
        var userSteps = importer.CreateUserShaderBuildSteps(fixture.Session);
        Assert.NotNull(userSteps);
        Assert.Equal("UpdateImportShaderCacheBuildStep", Assert.Single(userSteps!.Steps).GetType().Name);
    }
}
