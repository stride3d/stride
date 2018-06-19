// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;

using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;
using Xenko.Graphics;
using Xenko.Shaders.Compiler;

namespace Xenko.Shaders.Tests
{
    [TestFixture]
    [Ignore("This test fixture is unmaintained and currently doesn't pass")]
    public class TestShaderMixer2
    {
        public EffectCompiler Compiler;

        public LoggerResult ResultLogger;

        public CompilerParameters MixinParameters;

        [TestFixtureSetUp]
        public void Init()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);
            ContentManager.GetFileProvider = () => databaseFileProvider;

            Compiler = new EffectCompiler();
            Compiler.SourceDirectories.Add("shaders");
            MixinParameters = new CompilerParameters();
            MixinParameters.EffectParameters.Platform = GraphicsPlatform.Direct3D11;
            MixinParameters.EffectParameters.Profile = GraphicsProfile.Level_11_0;
            ResultLogger = new LoggerResult();
        }

        [Test]
        public void TestRenaming()
        {
            var color1Mixin = new ShaderClassSource("ComputeColorFixed", "Material.DiffuseColorValue");
            var color2Mixin = new ShaderClassSource("ComputeColorFixed", "Material.SpecularColorValue");
            
            var compMixin = new ShaderMixinSource();
            compMixin.Mixins.Add(new ShaderClassSource("ComputeColorMultiply"));
            compMixin.AddComposition("color1", color1Mixin);
            compMixin.AddComposition("color2", color2Mixin);

            var mixinSource = new ShaderMixinSource { Name = "testRenaming" };
            mixinSource.Mixins.Add(new ShaderClassSource("ShadingBase"));
            mixinSource.Mixins.Add(new ShaderClassSource("AlbedoFlatShading"));
            mixinSource.AddComposition("albedoDiffuse", compMixin);

            var byteCode = Compiler.Compile(mixinSource, MixinParameters.EffectParameters, MixinParameters);
            Assert.IsNotNull(byteCode);
        }

        [Test]
        public void TestRenaming2()
        {
            var color1Mixin = new ShaderMixinSource();
            color1Mixin.Mixins.Add(new ShaderClassSource("ComputeColorFixed", "Material.DiffuseColorValue"));
            var color2Mixin = new ShaderMixinSource();
            color2Mixin.Mixins.Add(new ShaderClassSource("ComputeColorFixed", "Material.SpecularColorValue"));

            var compMixin = new ShaderMixinSource();
            compMixin.Mixins.Add(new ShaderClassSource("ComputeColorMultiply"));
            compMixin.AddComposition("color1", color1Mixin);
            compMixin.AddComposition("color2", color2Mixin);

            var mixinSource = new ShaderMixinSource { Name = "TestRenaming2" };
            mixinSource.Mixins.Add(new ShaderClassSource("ShadingBase"));
            mixinSource.Mixins.Add(new ShaderClassSource("AlbedoFlatShading"));
            mixinSource.AddComposition("albedoDiffuse", compMixin);

            var byteCode = Compiler.Compile(mixinSource, MixinParameters.EffectParameters, MixinParameters);
            Assert.IsNotNull(byteCode);
        }

        [Test]
        public void TestRenamingBoth()
        {
            TestRenaming();
            TestRenaming2();
        }
        [Test]
        public void TestRenamingBothInverse()
        {
            TestRenaming2();
            TestRenaming();
        }

        public static void Main4()
        {
            var testClass = new TestShaderMixer2();
            testClass.Init();
            testClass.TestRenaming();
            testClass.TestRenaming2();
        }
    }
}
