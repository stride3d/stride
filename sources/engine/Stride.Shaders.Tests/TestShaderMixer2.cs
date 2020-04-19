// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Tests
{
    public class TestShaderMixer2
    {
        public EffectCompiler Compiler;

        public LoggerResult ResultLogger;

        public CompilerParameters MixinParameters;

        private void Init()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);

            Compiler = new EffectCompiler(databaseFileProvider);
            Compiler.SourceDirectories.Add("shaders");
            MixinParameters = new CompilerParameters();
            MixinParameters.EffectParameters.Platform = GraphicsPlatform.Direct3D11;
            MixinParameters.EffectParameters.Profile = GraphicsProfile.Level_11_0;
            ResultLogger = new LoggerResult();
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestRenaming()
        {
            Init();

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
            Assert.NotEqual(default(TaskOrResult<EffectBytecodeCompilerResult>), byteCode);
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestRenaming2()
        {
            Init();

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
            Assert.NotEqual(default(TaskOrResult<EffectBytecodeCompilerResult>), byteCode);
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestRenamingBoth()
        {
            Init();

            TestRenaming();
            TestRenaming2();
        }
        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestRenamingBothInverse()
        {
            Init();

            TestRenaming2();
            TestRenaming();
        }

        internal static void Main4()
        {
            var testClass = new TestShaderMixer2();
            testClass.Init();
            testClass.TestRenaming();
            testClass.TestRenaming2();
        }
    }
}
