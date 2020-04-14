// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
/*
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
using System.IO;

using Xunit;
using Stride.Core.IO;
using Stride.Core.Serialization.Assets;
using Stride.Core.Storage;
using Stride.Rendering;
using Stride.Games;
using Stride.Shaders;
using Stride.Shaders.Compiler;
using EffectCompiler = Stride.Shaders.Compiler.EffectCompiler;

namespace Stride.Graphics
{
    public class TestEffect : Game
    {
        private bool isTestGlsl = false;
        private bool isTestGlslES = false;
        
        public TestEffect()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 800,
                PreferredBackBufferHeight = 480,
                //PreferredGraphicsProfile = new[] { GraphicsProfile.Level_9_1 }
                PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 }
            };
        }

        protected override void Update(GameTime gameTime)
        {
            if (isTestGlsl)
            {
                base.Update(gameTime);
                isTestGlsl = false;
                RuntimeToGlslEffect();
            }
            else if (isTestGlslES)
            {
                base.Update(gameTime);
                isTestGlslES = false;
                RuntimeToGlslESEffect();
            }
            else
            {
                Exit();
            }
        }


        [Fact]
        public void TestSimpleEffect()
        {
            EffectBytecode effectBytecode;

            // Create and mount database file system
            var objDatabase = new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath);
            using (var contentIndexMap = new contentIndexMap("/assets"))
            {
                contentIndexMap.LoadNewValues();
                var database = new DatabaseFileProvider(contentIndexMap, objDatabase);

                foreach (var shaderName in Directory.EnumerateFiles(@"..\..\..\..\shaders", "*.sdsl"))
                    CopyStream(database, shaderName);

                foreach (var shaderName in Directory.EnumerateFiles(@"Compiler", "*.sdsl"))
                    CopyStream(database, shaderName);

                var compiler = new EffectCompiler();
                compiler.SourceDirectories.Add("assets/shaders");
                var compilerCache = new EffectCompilerCache(compiler);

                var compilerParmeters = new CompilerParameters { Platform = GraphicsPlatform.Direct3D };

                var compilerResults = compilerCache.Compile(new ShaderMixinSource("SimpleEffect"), compilerParmeters);
                Assert.That(compilerResults.HasErrors, Is.False);

                effectBytecode = compilerResults.Bytecodes[0];
            }

            var graphicsDevice = GraphicsDevice.New();

            var effect = new Effect(graphicsDevice, effectBytecode);
            effect.Apply();
        }

        [Fact]
        public void TestToGlslEffect()
        {
            isTestGlsl = true;
            this.Run();
        }

        private void RuntimeToGlslEffect()
        {
            EffectBytecode effectBytecode;

            // Create and mount database file system
            var objDatabase = new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath);
            using (var contentIndexMap = new contentIndexMap("/assets"))
            {
                contentIndexMap.LoadNewValues();
                var database = new DatabaseFileProvider(contentIndexMap, objDatabase);

                foreach (var shaderName in Directory.EnumerateFiles(@"..\..\..\..\shaders", "*.sdsl"))
                    CopyStream(database, shaderName);

                foreach (var shaderName in Directory.EnumerateFiles(@"Compiler", "*.sdsl"))
                    CopyStream(database, shaderName);

                foreach (var shaderName in Directory.EnumerateFiles(@"..\..\..\..\engine\Stride.Graphics\Shaders", "*.sdsl"))
                    CopyStream(database, shaderName);

                var compiler = new EffectCompiler();
                compiler.SourceDirectories.Add("assets/shaders");
                var compilerCache = new EffectCompilerCache(compiler);

                var compilerParameters = new CompilerParameters { Platform = GraphicsPlatform.OpenGLCore };

                var compilerResults = compilerCache.Compile(new ShaderMixinSource("ToGlslEffect"), compilerParameters);
                Assert.That(compilerResults.HasErrors, Is.False);

                effectBytecode = compilerResults.Bytecodes[0];
            }

            this.GraphicsDevice.Begin();

            var effect = new Effect(this.GraphicsDevice, effectBytecode);
            effect.Apply();
        }

        [Fact]
        public void TestToGlslESEffect()
        {
            isTestGlslES = true;
            this.Run();
        }

        private void RuntimeToGlslESEffect()
        {
            EffectBytecode effectBytecode;

            // Create and mount database file system
            var objDatabase = new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath);
            using (var contentIndexMap = new contentIndexMap("/assets"))
            {
                contentIndexMap.LoadNewValues();
                var database = new DatabaseFileProvider(contentIndexMap, objDatabase);

                foreach (var shaderName in Directory.EnumerateFiles(@"..\..\..\..\shaders", "*.sdsl"))
                    CopyStream(database, shaderName);

                foreach (var shaderName in Directory.EnumerateFiles(@"Compiler", "*.sdsl"))
                    CopyStream(database, shaderName);

                foreach (var shaderName in Directory.EnumerateFiles(@"..\..\..\..\engine\Stride.Graphics\Shaders", "*.sdsl"))
                    CopyStream(database, shaderName);

                var compiler = new EffectCompiler();
                compiler.SourceDirectories.Add("assets/shaders");
                var compilerCache = new EffectCompilerCache(compiler);

                var compilerParameters = new CompilerParameters { Platform = GraphicsPlatform.OpenGLES };

                var compilerResults = compilerCache.Compile(new ShaderMixinSource("ToGlslEffect"), compilerParameters);
                Assert.That(compilerResults.HasErrors, Is.False);

                effectBytecode = compilerResults.Bytecodes[0];
            }

            this.GraphicsDevice.Begin();

            var effect = new Effect(this.GraphicsDevice, effectBytecode);
            effect.Apply();
        }

        private void CopyStream(DatabaseFileProvider database, string fromFilePath)
        {
            var shaderFilename = string.Format("shaders/{0}", Path.GetFileName(fromFilePath));
            if (!database.FileExists(shaderFilename))
            {
                using (var outStream = database.OpenStream(shaderFilename, VirtualFileMode.Create, VirtualFileAccess.Write, VirtualFileShare.Write))
                {
                    using (var inStream = new FileStream(fromFilePath, FileMode.Open, FileAccess.Read))
                    {
                        inStream.CopyTo(outStream);
                    }
                }
            }
        }
    }
}
#endif
*/
