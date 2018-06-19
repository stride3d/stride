// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;

using NUnit.Framework;

using Xenko.Core.Assets;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;
using Xenko.Assets.Materials;
using Xenko.Rendering.Materials.ComputeColors;
using Xenko.Rendering;
using Xenko.Rendering.Materials;
using Xenko.Graphics;
using Xenko.Shaders.Compiler;
using Xenko.Shaders.Parser.Mixins;

namespace Xenko.Shaders.Tests
{
    /// <summary>
    /// Tests for the mixins code generation and runtime API.
    /// </summary>
    [TestFixture]
    [Ignore("This test fixture is unmaintained and currently doesn't pass")]
    public partial class TestMixinCompiler
    {
        public static void Main()
        {
            new TestMixinCompiler().TestMaterial();
        }
        /// <summary>
        /// Tests mixin and compose keys with compilation.
        /// </summary>
        [Test]
        public void TestMaterial()
        {
            var compiler = new EffectCompiler { UseFileSystem = true };
            var currentPath = Core.PlatformFolders.ApplicationBinaryDirectory;
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Graphics\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Core"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Lights"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Shadows"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Materials\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Materials\ComputeColors\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Skinning"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Shading"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Transformation"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Utils"));
            var compilerParameters = new CompilerParameters { EffectParameters = { Platform = GraphicsPlatform.OpenGL } };

            var layers = new MaterialBlendLayers();
            layers.Add(new MaterialBlendLayer
            {
                BlendMap = new ComputeFloat(0.5f),
                Material =  AttachedReferenceManager.CreateProxyObject<Material>(AssetId.Empty, "fake")
            });

            var materialAsset = new MaterialAsset
            {
                Attributes = new MaterialAttributes()
                {
                    Diffuse = new MaterialDiffuseMapFeature()
                    {
                        DiffuseMap = new ComputeColor(Color4.White)
                    },
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                },
                Layers = layers
            };

            var fakeAsset = new MaterialAsset
            {
                Attributes = new MaterialAttributes()
                {
                    Diffuse = new MaterialDiffuseMapFeature()
                    {
                        DiffuseMap = new ComputeColor(Color.Blue)
                    },
                }
            };

            var context = new MaterialGeneratorContext { FindAsset = reference => fakeAsset };
            var result = MaterialGenerator.Generate(new MaterialDescriptor { Attributes = materialAsset.Attributes, Layers = materialAsset.Layers }, context, "TestMaterial");

            compilerParameters.Set(MaterialKeys.PixelStageSurfaceShaders, result.Material.Passes[0].Parameters.Get(MaterialKeys.PixelStageSurfaceShaders));
            var directionalLightGroup = new ShaderClassSource("LightDirectionalGroup", 1);
            compilerParameters.Set(LightingKeys.DirectLightGroups, new ShaderSourceCollection { directionalLightGroup });
            //compilerParameters.Set(LightingKeys.CastShadows, false);
            //compilerParameters.Set(MaterialParameters.HasSkinningPosition, true);
            //compilerParameters.Set(MaterialParameters.HasSkinningNormal, true);
            compilerParameters.Set(MaterialKeys.HasNormalMap, true);

            var results = compiler.Compile(new ShaderMixinGeneratorSource("XenkoEffectBase"), compilerParameters);

            Assert.IsFalse(results.HasErrors);
        }


        [Test]
        public void TestStream()
        {
            var compiler = new EffectCompiler { UseFileSystem = true };
            var currentPath = Core.PlatformFolders.ApplicationBinaryDirectory;
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Shaders.Tests\GameAssets\Compiler"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Graphics\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Core"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Lights"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Shadows"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Materials\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Materials\ComputeColors\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Skinning"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Shading"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Transformation"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Engine\Rendering\Utils"));
            var compilerParameters = new CompilerParameters { EffectParameters = { Platform = GraphicsPlatform.Direct3D11 } };
            var results = compiler.Compile(new ShaderClassSource("TestStream"), compilerParameters);

            Assert.IsFalse(results.HasErrors);
        }


        /// <summary>
        /// Tests mixin and compose keys with compilation.
        /// </summary>
        [Test]
        public void TestMixinAndComposeKeys()
        {
            var compiler = new EffectCompiler { UseFileSystem = true };
            var currentPath = Core.PlatformFolders.ApplicationBinaryDirectory;
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Graphics\Shaders"));
            compiler.SourceDirectories.Add(Path.Combine(currentPath, @"..\..\sources\engine\Xenko.Shaders.Tests\GameAssets\Mixins"));

            var compilerParameters = new CompilerParameters { EffectParameters = { Platform = GraphicsPlatform.Direct3D11 } };

            var subCompute1Key = TestABC.TestParameters.UseComputeColor2.ComposeWith("SubCompute1");
            var subCompute2Key = TestABC.TestParameters.UseComputeColor2.ComposeWith("SubCompute2");
            var subComputesKey = TestABC.TestParameters.UseComputeColorRedirect.ComposeWith("SubComputes[0]");

            compilerParameters.Set(subCompute1Key, true);
            compilerParameters.Set(subComputesKey, true);

            var results = compiler.Compile(new ShaderMixinGeneratorSource("test_mixin_compose_keys"), compilerParameters);

            Assert.IsFalse(results.HasErrors);

            var mainBytecode = results.Bytecode.WaitForResult();
            Assert.IsFalse(mainBytecode.CompilationLog.HasErrors);

            Assert.NotNull(mainBytecode.Bytecode.Reflection.ConstantBuffers);
            Assert.AreEqual(1, mainBytecode.Bytecode.Reflection.ConstantBuffers.Count);
            var cbuffer = mainBytecode.Bytecode.Reflection.ConstantBuffers[0];

            Assert.NotNull(cbuffer.Members);
            Assert.AreEqual(2, cbuffer.Members.Length);


            // Check that ComputeColor2.Color is correctly composed for variables
            var computeColorSubCompute2 = ComputeColor2Keys.Color.ComposeWith("SubCompute1");
            var computeColorSubComputes = ComputeColor2Keys.Color.ComposeWith("ColorRedirect.SubComputes[0]");

            var members = cbuffer.Members.Select(member => member.KeyInfo.KeyName).ToList();
            Assert.IsTrue(members.Contains(computeColorSubCompute2.Name));
            Assert.IsTrue(members.Contains(computeColorSubComputes.Name));
        }

        private void CopyStream(DatabaseFileProvider database, string fromFilePath)
        {
            var shaderFilename = string.Format("shaders/{0}", Path.GetFileName(fromFilePath));
            using (var outStream = database.OpenStream(shaderFilename, VirtualFileMode.Create, VirtualFileAccess.Write, VirtualFileShare.Write))
            {
                using (var inStream = new FileStream(fromFilePath, FileMode.Open, FileAccess.Read))
                {
                    inStream.CopyTo(outStream);
                }
            }
        }

        /// <summary>
        /// Tests a simple mixin.
        /// </summary>
        [Test]
        public void TestReal()
        {
            // TODO: review this tet
            if (Directory.Exists("data"))
            {
                Directory.Delete("data", true);
            }

            CompilerResults results01;
            CompilerResults results02;

            // Test this with a new database completely clean
            TestNoClean(out results01, out results02);
            Assert.That(results01.Bytecode, Is.EqualTo(results02.Bytecode));

            // Test with the database with already the bytecode generated by previous step
            CompilerResults results11;
            CompilerResults results12;
            TestNoClean(out results11, out results12);
            Assert.That(results11.Bytecode, Is.EqualTo(results12.Bytecode));

            // Check previous result
            //Assert.That(results01.MainBytecode.Time, Is.EqualTo(results11.MainBytecode.Time)); -> crash, MainBytecode == null
        }

        public void TestNoClean(out CompilerResults left, out CompilerResults right)
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            using (var assetIndexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath))
            {
                var database = new DatabaseFileProvider(assetIndexMap, objDatabase);
                ContentManager.GetFileProvider = () => database;

                foreach (var shaderName in Directory.EnumerateFiles(@"..\..\sources\shaders", "*.xksl"))
                    CopyStream(database, shaderName);

                foreach (var shaderName in Directory.EnumerateFiles(@"..\..\sources\engine\Xenko.Shaders.Tests\GameAssets\Compiler", "*.xksl"))
                    CopyStream(database, shaderName);

                var compiler = new EffectCompiler();
                compiler.SourceDirectories.Add("assets/shaders");
                var compilerCache = new EffectCompilerCache(compiler);

                var compilerParameters = new CompilerParameters { EffectParameters = { Platform = GraphicsPlatform.Direct3D11 } };

                left = compilerCache.Compile(new ShaderMixinGeneratorSource("SimpleEffect"), compilerParameters);
                right = compilerCache.Compile(new ShaderMixinGeneratorSource("SimpleEffect"), compilerParameters);
            }
        }

        [Test]
        public void TestGlslCompiler()
        {
            VirtualFileSystem.RemountFileSystem("/shaders", "../../../../shaders");
            VirtualFileSystem.RemountFileSystem("/baseShaders", "../../../../engine/Xenko.Graphics/Shaders");
            VirtualFileSystem.RemountFileSystem("/compiler", "Compiler");


            var compiler = new EffectCompiler();

            compiler.SourceDirectories.Add("shaders");
            compiler.SourceDirectories.Add("compiler");
            compiler.SourceDirectories.Add("baseShaders");

            var compilerParameters = new CompilerParameters { EffectParameters = { Platform = GraphicsPlatform.OpenGL } };

            var results = compiler.Compile(new ShaderMixinGeneratorSource("ToGlslEffect"), compilerParameters);
        }

        [Test]
        public void TestGlslESCompiler()
        {
            VirtualFileSystem.RemountFileSystem("/shaders", "../../../../shaders");
            VirtualFileSystem.RemountFileSystem("/baseShaders", "../../../../engine/Xenko.Graphics/Shaders");
            VirtualFileSystem.RemountFileSystem("/compiler", "Compiler");

            var compiler = new EffectCompiler();

            compiler.SourceDirectories.Add("shaders");
            compiler.SourceDirectories.Add("compiler");
            compiler.SourceDirectories.Add("baseShaders");

            var compilerParameters = new CompilerParameters { EffectParameters = { Platform = GraphicsPlatform.OpenGLES } };

            var results = compiler.Compile(new ShaderMixinGeneratorSource("ToGlslEffect"), compilerParameters);
        }
    }
}
