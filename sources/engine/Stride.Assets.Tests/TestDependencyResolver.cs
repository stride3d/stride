// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// TODO: Adapt to new API
/*
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using Xunit;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Assets.SpriteFont;
using Stride.Rendering;

using System.Linq;

using Stride.Graphics;
namespace Stride.Assets.Tests

{
    public class TestDependencyResolver
    {
        [TestFixtureSetUp]
        public void Initialize()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(SpriteFontAsset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(Stride.Assets.Model.ModelAsset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(IVertex).Module.ModuleHandle);

            // load assembly to register the assets extensions
            var assetAssembly = Assembly.Load("Stride.Assets");
            AssetRegistry.RegisterAssembly(assetAssembly); 
            assetAssembly = Assembly.Load("Stride.Assets.Model");
            AssetRegistry.RegisterAssembly(assetAssembly);
        }

        [Fact]
        public void TestTextureItemAsset()
        {
            // load the project
            var projectSessionResult = PackageSession.Load(@"Stride.Assets.Tests\Projects\TextureDeps\Assets.sdpkg");
            var projectSession = projectSessionResult.Session;
            var textureItem = projectSession.RootPackage.Assets.First();

            var createdProjectSession = projectSession.CreateCompileProjectFromAsset(textureItem);

            Assert.Equal(projectSession.RootPackage.RootDirectory, createdProjectSession.RootPackage.RootDirectory);
            Assert.Equal(createdProjectSession.RootPackage.Assets.Count, 1);
        }

        [Fact]
        public void TestMaterialItemAsset()
        {
            // load the project
            var projectSessionResult = PackageSession.Load(@"Stride.Assets.Tests\Projects\MaterialDeps\Assets.sdpkg");
            var projectSession = projectSessionResult.Session;
            var materialItem = projectSession.RootPackage.Assets.First();

            var createdProjectSession = projectSession.CreateCompileProjectFromAsset(materialItem);

            Assert.Equal(projectSession.RootPackage.RootDirectory, createdProjectSession.RootPackage.RootDirectory);
            Assert.Equal(createdProjectSession.RootPackage.Assets.Count, 2);
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("77fb96f6-a38f-4a71-a43c-c8d566ea3825")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("cb1f44ac-a6bc-4b67-b74a-0c2859c55b3a")));
        }

        [Fact]
        public void TestCircularDependencies()
        {
            // load the project
            var projectSessionResult = PackageSession.Load(@"Stride.Assets.Tests\Projects\CircularDeps\Assets.sdpkg");
            var projectSession = projectSessionResult.Session;
            var materialItem = projectSession.RootPackage.Assets.First();

            var createdProjectSession = projectSession.CreateCompileProjectFromAsset(materialItem);

            Assert.Equal(projectSession.RootPackage.RootDirectory, createdProjectSession.RootPackage.RootDirectory);
            Assert.Equal(createdProjectSession.RootPackage.Assets.Count, 2);
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("dc4e6241-f10e-4d35-a47a-ed9ccf9b00dd")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("d7eccfd9-3c44-4d83-b141-eb72e65b1f81")));
        }

        [Fact]
        public void TestEntityItemAsset()
        {
            // load the project
            var projectSessionResult = PackageSession.Load(@"Stride.Assets.Tests\Projects\EntityDeps\Assets.sdpkg");
            var projectSession = projectSessionResult.Session;
            var entityItem = projectSession.RootPackage.Assets.First();

            var createdProjectSession = projectSession.CreateCompileProjectFromAsset(entityItem);

            Assert.Equal(projectSession.RootPackage.RootDirectory, createdProjectSession.RootPackage.RootDirectory);
            Assert.Equal(createdProjectSession.RootPackage.Assets.Count, 3);
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("be52722c-c19a-472e-9714-8706ed88bc45")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("ed0173f8-865e-4fea-8d7b-e8553eff5595")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("7fccdd7f-baf0-46a0-8526-0f34de5f72a1")));
        }

        [Fact]
        public void TestComplexDependencies()
        {
            // load the project
            var projectSessionResult = PackageSession.Load(@"Stride.Assets.Tests\Projects\ComplexDeps\Assets.sdpkg");
            var projectSession = projectSessionResult.Session;
            var entityItem = projectSession.RootPackage.Assets.First();

            var createdProjectSession = projectSession.CreateCompileProjectFromAsset(entityItem);

            Assert.Equal(projectSession.RootPackage.RootDirectory, createdProjectSession.RootPackage.RootDirectory);
            Assert.Equal(createdProjectSession.RootPackage.Assets.Count, 5);
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("360026b3-7636-456d-bcfe-a3bc8c6db2a9")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("6acc7d38-d227-4a0d-b966-5cef68aecec8")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("f2620870-6cd8-4e9f-8dfe-1431624562b1")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("9bd0e22c-5c8a-4ca5-a43a-4f101072e71c")));
            Assert.True(createdProjectSession.RootPackage.Assets.Any(x => x.Id == new Guid("e3043027-c216-438d-b59f-f3775e73de85")));
        }
    }
}*/
