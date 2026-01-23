using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Core.Serialization.Contents;
using Stride.Shaders.Compiler;
using Stride.Shaders.Compilers.SDSL;
using Xunit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Stride.Shaders.Tests
{
    // Temporary test for old vs new shader system
    public class BenchmarkShaderSystems
    {
        EffectCompiler compiler;
        ShaderMixinSource shaderMixinSource;

        public BenchmarkShaderSystems()
        {
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var database = new DatabaseFileProvider(objDatabase);
            compiler = new EffectCompiler(database);
            compiler.SourceDirectories.Add(EffectCompilerBase.DefaultSourceShaderFolder);

            shaderMixinSource = new ShaderMixinSource
            {
                Mixins =
            {
                new ShaderClassSource("ShaderBase"),
                new ShaderClassSource("ShadingBase"),
                new ShaderClassSource("TransformationBase"),
                new ShaderClassSource("NormalStream"),
                new ShaderClassSource("TransformationWAndVP"),
                new ShaderClassSource("NormalFromMesh"),
                new ShaderClassSource("MaterialSurfacePixelStageCompositor"),
            },
                Compositions =
            {
                ["directLightGroups"] = new ShaderArraySource
                {
                    new ShaderMixinSource
                    {
                        Mixins =
                        {
                            new ShaderClassSource("LightDirectionalGroup", "1"),
                            new ShaderClassSource("ShadowMapReceiverDirectional", "1", "1", "true", "true", "false", "false"),
                            new ShaderClassSource("ShadowMapFilterDefault", "PerView.Lighting"),
                        },
                    },
                    new ShaderMixinSource
                    {
                        Mixins = { new ShaderClassSource("LightClusteredPointGroup") },
                    },
                    new ShaderMixinSource
                    {
                        Mixins = { new ShaderClassSource("LightClusteredSpotGroup") },
                    },
                },
                ["environmentLights"] = new ShaderArraySource
                {
                    new ShaderMixinSource
                    {
                        Mixins = { new ShaderClassSource("LightSimpleAmbient") },
                    },
                    new ShaderMixinSource
                    {
                        Mixins = { new ShaderClassSource("EnvironmentLight") },
                    },
                },
                ["materialPixelStage"] = new ShaderMixinSource
                {
                    Mixins = { new ShaderClassSource("MaterialSurfaceArray") },
                    Compositions =
                    {
                        ["layers"] = new ShaderArraySource
                        {
                            new ShaderMixinSource
                            {
                                Mixins = { new ShaderClassSource("MaterialSurfaceDiffuse") },
                                Compositions = { ["diffuseMap"] = new ShaderClassSource("ComputeColorConstantColorLink", "Material.DiffuseValue") },
                            },
                            new ShaderMixinSource
                            {
                                Mixins = { new ShaderClassSource("MaterialSurfaceLightingAndShading") },
                                Compositions =
                                {
                                    ["surfaces"] = new ShaderArraySource
                                    {
                                        new ShaderClassSource("MaterialSurfaceShadingDiffuseLambert", "false"),
                                    },
                                },
                            },
                        },
                    },
                },
                ["streamInitializerPixelStage"] = new ShaderMixinSource
                {
                    Mixins =
                    {
                        new ShaderClassSource("MaterialStream"),
                        new ShaderClassSource("MaterialPixelShadingStream"),
                    },
                },
            },
            };
        }

        [Benchmark]
        public void OldSystem()
        {
            // Old system
            var parsingResult = compiler.GetMixinParser().Parse(shaderMixinSource, shaderMixinSource.Macros.ToArray());
        }

        [Benchmark]
        public void NewSystem()
        {
            // New system
            var shaderMixer = new ShaderMixer(new EffectCompiler.ShaderLoader(compiler.FileProvider));
            shaderMixer.MergeSDSL(shaderMixinSource, new ShaderMixer.Options(false), out var spirvBytecode, out var effectReflection, out var usedHashSources, out var entryPoints);
        }
    }

    public class BenchmarkProgram
    {
        public static void Main(string[] args)
        {
            /*var test = new TestShaderSystems();
            for (int i = 0; i < 5; ++i)
                test.OldSystem();
            for (int i = 0; i < 5; ++i)
                test.NewSystem();

            return;*/
            // TODO: somehow iteration count is not respected, need to review that
            var config = new DebugInProcessConfig()
                // Enable for debug mode
                //.WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(Job.Default
                .WithWarmupCount(0)
                .WithIterationCount(1)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
                .AsDefault());

            var summary = BenchmarkRunner.Run(typeof(BenchmarkProgram).Assembly, config);
        }
    }
}
