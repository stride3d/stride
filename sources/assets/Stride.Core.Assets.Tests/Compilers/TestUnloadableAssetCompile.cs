// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Compiler;
using Stride.Core.Diagnostics;
using Stride.Core.Yaml;
using static Stride.Core.Assets.Tests.Compilers.TestBuildDependencyManager;

namespace Stride.Core.Assets.Tests.Compilers;

/// <summary>
/// An asset whose type is not loaded (the package providing it is missing) fails a game build only: the editor's
/// previews, thumbnails and editor game skip it and still prepare the other assets.
/// </summary>
public class TestUnloadableAssetCompile : CompilerTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnUnloadableAssetIsAnErrorOnlyInAGameBuild(bool gameBuild)
    {
        var package = new Package();
        // ReSharper disable once UnusedVariable - we need a package session to compile
        var packageSession = new PackageSession(package);
        var unloadableAsset = (Asset)UnloadableObjectInstantiator.CreateUnloadableObject(typeof(Asset), "FogAsset", "Acme.Fog.Assets", "the type is not loaded", []);
        var unloadable = new AssetItem("fog", unloadableAsset, package);
        var other = new AssetItem("other", new MyAsset5(), package);
        package.Assets.Add(unloadable);
        package.Assets.Add(other);

        var compiler = new AssetDependenciesCompiler(typeof(AssetCompilationContext)) { UnloadableAssetIsError = gameBuild };
        var result = compiler.PrepareMany(new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) }, [unloadable, other]);

        Assert.Equal(gameBuild, result.HasErrors);
        var expectedType = gameBuild ? LogMessageType.Error : LogMessageType.Warning;
        Assert.Contains(result.Messages, m => m.Type == expectedType && m.Text.Contains("[FogAsset] from [Acme.Fog.Assets] is not loaded", StringComparison.Ordinal));
        Assert.Equal(1, result.BuildSteps.Count);
    }

    [Fact]
    public void AGameBuildFailsOnAnUnloadableRootAsset()
    {
        var package = new Package();
        // ReSharper disable once UnusedVariable - we need a package session to compile
        var packageSession = new PackageSession(package);
        var unloadableAsset = (Asset)UnloadableObjectInstantiator.CreateUnloadableObject(typeof(Asset), "FogAsset", "Acme.Fog.Assets", "the type is not loaded", []);
        var unloadable = new AssetItem("fog", unloadableAsset, package);
        package.Assets.Add(unloadable);
        package.RootAssets.Add(new AssetReference(unloadable.Id, unloadable.Location));

        var result = new PackageCompiler(new RootPackageAssetEnumerator(package)).Prepare(new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) });

        Assert.Contains(result.Messages, m => m.Type == LogMessageType.Error && m.Text.Contains("The package providing it is missing.", StringComparison.Ordinal));
    }
}
