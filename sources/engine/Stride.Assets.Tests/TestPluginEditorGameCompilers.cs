// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Audio.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Editor.Preview;
using Stride.Video.Assets;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The editor's game compiles sounds and videos into empty placeholders, with compilers from the Assets packages, which
    /// always load: with no Editor package loaded, the scene editor still doesn't encode them with the game's compiler.
    /// </summary>
    public class TestPluginEditorGameCompilers
    {
        [Theory]
        [InlineData(typeof(SoundAsset), typeof(SoundAssetEditorGameCompiler))]
        [InlineData(typeof(VideoAsset), typeof(VideoAssetEditorGameCompiler))]
        public void TheAssetsPackageCompilesForTheEditorGame(Type assetType, Type compilerType)
        {
            using (AssetAssemblyScope.Register(compilerType.Assembly))
            {
                var registry = new AssetCompilerRegistry();

                Assert.IsType(compilerType, registry.GetCompiler(assetType, typeof(EditorGameCompilationContext)));
                // The game itself still compiles the file
                Assert.IsNotType(compilerType, registry.GetCompiler(assetType, typeof(AssetCompilationContext)));
            }

            // This project references no Editor package: nothing above came from one
            Assert.False(AssetAssemblyScope.AnyEditorPackageLoaded());
        }
    }
}
