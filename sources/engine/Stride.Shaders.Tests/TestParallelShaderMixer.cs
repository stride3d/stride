// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Xunit;

using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Tests
{
    public class TestParallelShaderMixer
    {
        private static EffectCompiler compiler;

        private static int NumThreads = 15;

        public static void Main3()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var assetIndexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            var databaseFileProvider = new DatabaseFileProvider(assetIndexMap, objDatabase);

            compiler = new EffectCompiler(databaseFileProvider);
            compiler.SourceDirectories.Add("shaders");
            var shaderMixinSource = new ShaderMixinSource();
            shaderMixinSource.Mixins.Add(new ShaderClassSource("ShaderBase"));
            shaderMixinSource.Mixins.Add(new ShaderClassSource("TransformationWVP"));
            shaderMixinSource.Mixins.Add(new ShaderClassSource("ShadingBase"));

            var shaderMixinSource2 = new ShaderMixinSource();
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("ShaderBase"));
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("TransformationWVP"));
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("ShadingBase"));
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("ShadingOverlay"));

            var allThreads = new List<Thread>();

            for (int i = 0; i < NumThreads; ++i)
            {
                CompilerThread compilerThread;
                if (i % 2 == 0)
                    compilerThread = new CompilerThread(compiler, shaderMixinSource);
                else
                    compilerThread = new CompilerThread(compiler, shaderMixinSource2);
                allThreads.Add(new Thread(compilerThread.Compile));
            }

            foreach (var thread in allThreads)
            {
                thread.Start();
            }
        }
        
    }

    public class CompilerThread
    {
        private volatile EffectCompiler effectCompiler;

        private volatile ShaderMixinSource mixinSource;

        public CompilerThread(EffectCompiler compiler, ShaderMixinSource source)
        {
            effectCompiler = compiler;
            mixinSource = source;
        }

        public void Compile()
        {
            Console.WriteLine(@"Inside Thread");
            
            var parameters = new CompilerParameters();
            parameters.EffectParameters.Platform = GraphicsPlatform.Direct3D11;
            parameters.EffectParameters.Profile = GraphicsProfile.Level_11_0;

            var mixinTree = new ShaderMixinSource() { Name = "TestParallelMix" };

            var result = effectCompiler.Compile(mixinTree, parameters.EffectParameters, parameters).WaitForResult();

            Assert.False(result.CompilationLog.HasErrors);
            Assert.NotNull(result);

            Console.WriteLine(@"Thread end");
        }
    }
}
