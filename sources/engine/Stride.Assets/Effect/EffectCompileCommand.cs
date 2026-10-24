// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Effect
{
    /// <summary>
    /// This command is responsible to compile a single permutation of an effect (sdfx or sdsl)
    /// </summary>
    internal sealed class EffectCompileCommand : IndexFileCommand
    {
        private static readonly PropertyKey<EffectCompilerBase> CompilerKey = new PropertyKey<EffectCompilerBase>("CompilerKey", typeof(EffectCompileCommand));

        private readonly AssetCompilerContext context;
        private readonly UDirectory baseUrl;
        private string effectName;
        private CompilerParameters compilerParameters;
        private readonly Package package;
        private static Dictionary<string, int> PermutationCount = new Dictionary<string, int>();

        public EffectCompileCommand(AssetCompilerContext context, UDirectory baseUrl, string effectName, CompilerParameters compilerParameters, Package package)
        {
            this.context = context;
            this.baseUrl = baseUrl;
            this.effectName = effectName;
            this.compilerParameters = compilerParameters;
            this.package = package;
        }

        public override string Title => $"EffectCompile [{effectName}]";

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);
            uint effectbyteCodeMagicNumber = EffectBytecode.MagicHeader;
            writer.Serialize(ref effectbyteCodeMagicNumber, ArchiveMode.Serialize);
            writer.Serialize(ref effectName, ArchiveMode.Serialize);
            writer.Serialize(ref compilerParameters, ArchiveMode.Serialize);
            writer.Serialize(ref compilerParameters.EffectParameters, ArchiveMode.Serialize);
        }

        protected override void ComputeAssemblyHash(BinarySerializationWriter writer)
        {
            writer.Write(DataSerializer.BinaryFormatVersion);
            writer.Write(EffectBytecode.MagicHeader);
            writer.Write(ShaderCompilerVersion.Shader);
            writer.Write(ShaderCompilerVersion.Effect);
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var compiler = GetOrCreateEffectCompiler(context);

            // Get main effect name (before the first dot)
            var isXkfx = ShaderMixinManager.Contains(effectName);
            var source = isXkfx ? new ShaderMixinGeneratorSource(effectName) : (ShaderSource)new ShaderClassSource(effectName);

            int permutationCount;
            lock (PermutationCount)
            {
                PermutationCount.TryGetValue(effectName, out permutationCount);
                permutationCount++;
                PermutationCount[effectName] = permutationCount;
            }
            commandContext.Logger.Verbose($"Trying permutation #{permutationCount} for effect [{effectName}]: \n{compilerParameters.ToStringPermutationsDetailed()}");

            var compilerResults = compiler.Compile(source, compilerParameters);

            // Copy logs and if there are errors, exit directly
            compilerResults.CopyTo(commandContext.Logger);
            if (compilerResults.HasErrors)
            {
                return Task.FromResult(ResultStatus.Failed);
            }

            // wait for result an check compilation status
            var completedTask = compilerResults.Bytecode.WaitForResult();
            completedTask.CompilationLog.CopyTo(commandContext.Logger);
            if (completedTask.CompilationLog.HasErrors)
            {
                return Task.FromResult(ResultStatus.Failed);
            }

            // Register all dependencies
            var allSources = new HashSet<string>(completedTask.Bytecode.HashSources.Select(keyPair => keyPair.Key));
            foreach (var className in allSources)
            {
                commandContext.RegisterInputDependency(new ObjectUrl(UrlType.Content, EffectCompilerBase.GetStoragePathFromShaderType(className)));
            }

            // Generate sourcecode if configured
            if (compilerParameters.ContainsKey(EffectSourceCodeKeys.Enable))
            {
                var outputDirectory = UPath.Combine(package.RootDirectory, baseUrl).ToOSPath();
                var fullOutputClassFile = Path.Combine(outputDirectory, EffectBytecodeSourceWriter.GetFileName(effectName, compilerParameters));

                commandContext.Logger.Verbose($"Writing shader bytecode to .cs source [{fullOutputClassFile}]");
                using (var stream = new FileStream(fullOutputClassFile, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    // The command line, with paths relative to the generated file, so it is the same on every machine
                    var commandLine = string.Join(' ', Environment.GetCommandLineArgs().Select(argument => RelativeArgument(argument, outputDirectory)));
                    EffectBytecodeSourceWriter.Write(effectName, compilerParameters, compilerResults.Bytecode.WaitForResult().Bytecode, new StreamWriter(stream, System.Text.Encoding.UTF8), commandLine);
                }
            }

            return Task.FromResult(ResultStatus.Successful);
        }

        public override string ToString()
        {
            return Title;
        }

        // A path, or the path of an --option=path, relative to the output folder
        private static string RelativeArgument(string argument, string outputDirectory)
        {
            var separator = argument.StartsWith('-') ? argument.IndexOf('=') : -1;
            var value = argument[(separator + 1)..];
            return Path.IsPathFullyQualified(value)
                ? argument[..(separator + 1)] + Path.GetRelativePath(outputDirectory, value)
                : argument;
        }

        private static EffectCompilerBase GetOrCreateEffectCompiler(AssetCompilerContext context)
        {
            lock (context)
            {
                var compiler = context.Properties.Get(CompilerKey);
                if (compiler == null)
                {
                    // Create compiler
                    var effectCompiler = new EffectCompiler(MicrothreadLocalDatabases.DatabaseFileProvider);
                    effectCompiler.SourceDirectories.Add(EffectCompilerBase.DefaultSourceShaderFolder);
                    compiler = new EffectCompilerCache(effectCompiler, MicrothreadLocalDatabases.DatabaseFileProvider) { CurrentCache = EffectBytecodeCacheLoadSource.StartupCache };
                    context.Properties.Set(CompilerKey, compiler);

                    var shaderLocations = context.Properties.Get(EffectShaderAssetCompiler.ShaderLocationsKey);

                    // Temp copy URL to absolute file path to inform the compiler the absolute file location
                    // of all sdsl files.
                    if (shaderLocations != null)
                    {
                        foreach (var shaderLocation in shaderLocations)
                        {
                            effectCompiler.UrlToFilePath[shaderLocation.Key] = shaderLocation.Value;
                        }
                    }
                }

                return compiler;
            }
        }

        public static BuildStep FromRequest(AssetCompilerContext context, Package package, UDirectory urlRoot, EffectCompileRequest effectCompileRequest)
        {
            var compilerParameters = new CompilerParameters(effectCompileRequest.UsedParameters);
            compilerParameters.EffectParameters.Platform = context.GetGraphicsPlatform(package);
            compilerParameters.EffectParameters.Profile = context.GetGameSettingsAsset().GetOrCreate<RenderingSettings>().DefaultGraphicsProfile;
            compilerParameters.EffectParameters.ApplyCompilationMode(context.GetCompilationMode());
            // Bytecode written as C# source is committed: no debug info, so the same sources always give the same bytes
            // (FXC's debug info gets a new signature at each compile, and SPIR-V's has the source paths and text)
            if (compilerParameters.ContainsKey(EffectSourceCodeKeys.Enable))
            {
                compilerParameters.EffectParameters.Debug = false;
                // The level FXC uses when it compiles with debug info, so the code stays the same
                compilerParameters.EffectParameters.OptimizationLevel = Math.Max(compilerParameters.EffectParameters.OptimizationLevel, 1);
            }
            return new CommandBuildStep(new EffectCompileCommand(context, urlRoot, effectCompileRequest.EffectName, compilerParameters, package));
        }
    }
}
