// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parser;
using Stride.Shaders.Parser.Mixins;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using Encoding = System.Text.Encoding;
using LoggerResult = Stride.Core.Diagnostics.LoggerResult;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// An <see cref="IEffectCompiler"/> which will compile effect into multiple shader code, and compile them with a <see cref="IShaderCompiler"/>.
    /// </summary>
    public class EffectCompiler : EffectCompilerBase
    {
        private bool d3dCompilerLoaded = false;
        private static readonly Object WriterLock = new Object();

        private ShaderMixinParser shaderMixinParser;

        private readonly object shaderMixinParserLock = new object();

        public List<string> SourceDirectories { get; private set; }

        public Dictionary<string, string> UrlToFilePath { get; private set; }

        public override IVirtualFileProvider FileProvider { get; set; }
        public bool UseFileSystem { get; set; }

        public EffectCompiler(IVirtualFileProvider fileProvider)
        {
            FileProvider = fileProvider;
            if (Platform.IsWindowsDesktop && !d3dCompilerLoaded)
            {
                NativeLibraryHelper.PreloadLibrary("d3dcompiler_47", typeof(EffectCompiler));
                d3dCompilerLoaded = true;
            }
            SourceDirectories = new List<string>();
            UrlToFilePath = new Dictionary<string, string>();
        }

        public override ObjectId GetShaderSourceHash(string type)
        {
            return GetMixinParser().SourceManager.GetShaderSourceHash(type);
        }

        /// <summary>
        /// Remove cached files for modified shaders
        /// </summary>
        /// <param name="modifiedShaders"></param>
        public override void ResetCache(HashSet<string> modifiedShaders)
        {
            GetMixinParser().DeleteObsoleteCache(modifiedShaders);
        }

        private ShaderMixinParser GetMixinParser()
        {
            lock (shaderMixinParserLock)
            {
                // Generate the AST from the mixin description
                if (shaderMixinParser == null)
                {
                    shaderMixinParser = new ShaderMixinParser(FileProvider);
                    shaderMixinParser.SourceManager.LookupDirectoryList.AddRange(SourceDirectories); // TODO: temp
                    shaderMixinParser.SourceManager.UseFileSystem = UseFileSystem;
                    shaderMixinParser.SourceManager.UrlToFilePath = UrlToFilePath; // TODO: temp
                }
                return shaderMixinParser;
            }
        }

        class ShaderLoader(IVirtualFileProvider FileProvider) : ShaderLoaderBase(new ShaderCache())
        {
            protected override bool ExternalFileExists(string name)
            {
                var path = $"shaders/{name}.sdsl";
                return FileProvider.FileExists(path);
            }

            public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
            {
                var path = $"shaders/{name}.sdsl";
                
                using var sourceStream = FileProvider.OpenStream(path, VirtualFileMode.Open, VirtualFileAccess.Read);
                using var reader = new StreamReader(sourceStream);
                code = reader.ReadToEnd();

                var databaseStream = sourceStream as IDatabaseStream;
                if (databaseStream == null)
                {
                    sourceStream.Position = 0;
                    var data = new byte[sourceStream.Length];
                    var readBytes = sourceStream.Read(data, 0, (int)sourceStream.Length);
                    if (readBytes != sourceStream.Length)
                        throw new InvalidOperationException();
                    hash = ObjectId.FromBytes(data);
                }
                else
                {
                    hash = databaseStream.ObjectId;
                }

                filename = path;
                return true;
            }
        }

        //Parsing.SDSL.ShaderMixinSource ConvertAndEnsureMixin(ShaderSource shaderSource)
        //{
        //    var result = Convert(shaderSource);
        //    return result switch
        //    {
        //        Parsing.SDSL.ShaderMixinSource mixinSource => mixinSource,
        //        Parsing.SDSL.ShaderClassSource classSource => new Parsing.SDSL.ShaderMixinSource { Mixins = { classSource } },
        //    };
        //}

        //Parsing.SDSL.ShaderSource Convert(ShaderSource shaderSource)
        //{
        //    return shaderSource switch
        //    {
        //        ShaderClassSource classSource => new Parsing.SDSL.ShaderClassSource(classSource.ClassName) { GenericArguments = classSource.GenericArguments },
        //        ShaderMixinSource mixinSource => new Parsing.SDSL.ShaderMixinSource { Compositions = new Dictionary<string, Parsing.SDSL.ShaderMixinSource>(mixinSource.Compositions.Select(x => KeyValuePair.Create(x.Key, ConvertAndEnsureMixin(x.Value)))) },
        //    };
        //}


        public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters)
        {
            var log = new LoggerResult();

            // Load D3D compiler dll
            // Note: No lock, it's probably fine if it gets called from multiple threads at the same time.
            if (Platform.IsWindowsDesktop && !d3dCompilerLoaded)
            {
                NativeLibraryHelper.PreloadLibrary("d3dcompiler_47", typeof(EffectCompiler));
                d3dCompilerLoaded = true;
            }

            var shaderMixinSource = mixinTree;
            var fullEffectName = mixinTree.Name;

            // Make a copy of shaderMixinSource. Use deep clone since shaderMixinSource can be altered during compilation (e.g. macros)
            var shaderMixinSourceCopy = new ShaderMixinSource();
            shaderMixinSourceCopy.DeepCloneFrom(shaderMixinSource);
            shaderMixinSource = shaderMixinSourceCopy;

            // Generate platform-specific macros
            switch (effectParameters.Platform)
            {
                case GraphicsPlatform.Direct3D11:
                    shaderMixinSource.AddMacro("STRIDE_GRAPHICS_API_DIRECT3D", 1);
                    shaderMixinSource.AddMacro("STRIDE_GRAPHICS_API_DIRECT3D11", 1);
                    break;
                case GraphicsPlatform.Direct3D12:
                    shaderMixinSource.AddMacro("STRIDE_GRAPHICS_API_DIRECT3D", 1);
                    shaderMixinSource.AddMacro("STRIDE_GRAPHICS_API_DIRECT3D12", 1);
                    break;
                case GraphicsPlatform.Vulkan:
                    shaderMixinSource.AddMacro("STRIDE_GRAPHICS_API_VULKAN", 1);
                    break;
                default:
                    throw new NotSupportedException();
            }

            // Generate profile-specific macros
            shaderMixinSource.AddMacro("STRIDE_GRAPHICS_PROFILE", (int)effectParameters.Profile);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_9_1", (int)GraphicsProfile.Level_9_1);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_9_2", (int)GraphicsProfile.Level_9_2);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_9_3", (int)GraphicsProfile.Level_9_3);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_10_0", (int)GraphicsProfile.Level_10_0);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_10_1", (int)GraphicsProfile.Level_10_1);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_11_0", (int)GraphicsProfile.Level_11_0);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_11_1", (int)GraphicsProfile.Level_11_1);
            shaderMixinSource.AddMacro("GRAPHICS_PROFILE_LEVEL_11_2", (int)GraphicsProfile.Level_11_2);

            // In .sdsl, class has been renamed to shader to avoid ambiguities with HLSL
            shaderMixinSource.AddMacro("class", "shader");

            var shaderMixer = new ShaderMixer(new ShaderLoader(FileProvider));
            shaderMixer.MergeSDSL(shaderMixinSource, new ShaderMixer.Options(effectParameters.Platform is not GraphicsPlatform.Vulkan), out var spirvBytecode, out var effectReflection, out var usedHashSources, out var entryPoints);

            /*var parsingResult = GetMixinParser().Parse(shaderMixinSource, shaderMixinSource.Macros.ToArray());

            // Copy log from parser results to output
            CopyLogs(parsingResult, log);

            // Return directly if there are any errors
            if (parsingResult.HasErrors)
            {
                return new EffectBytecodeCompilerResult(null, log);
            }

            // Convert the AST to HLSL
            var writer = new Stride.Core.Shaders.Writer.Hlsl.HlslWriter
            {
                EnablePreprocessorLine = false, // Allow to output links to original pdxsl via #line pragmas
            };
            writer.Visit(parsingResult.Shader);
            var shaderSourceText = writer.Text;

            if (string.IsNullOrEmpty(shaderSourceText))
            {
                log.Error($"No code generated for effect [{fullEffectName}]");
                return new EffectBytecodeCompilerResult(null, log);
            }*/

            // -------------------------------------------------------
            // Save shader log
            // TODO: TEMP code to allow debugging generated shaders on Windows Desktop
#if STRIDE_PLATFORM_DESKTOP
            var shaderId = ObjectId.FromBytes(spirvBytecode);

            var logDir = Path.Combine(PlatformFolders.ApplicationBinaryDirectory, "log");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            var shaderSourceFilename = Path.Combine(logDir, "shader_" + fullEffectName.Replace('.', '_') + "_" + shaderId + ".hlsl");
            lock (WriterLock) // protect write in case the same shader is created twice
            {
                // Write shader before generating to make sure that we are having a trace before compiling it (compiler may crash...etc.)
                if (!File.Exists(shaderSourceFilename))
                {
                    File.WriteAllBytes(Path.ChangeExtension(shaderSourceFilename, ".spv"), spirvBytecode);
                    File.WriteAllText(Path.ChangeExtension(shaderSourceFilename, ".spvdis"), Spirv.Tools.Spv.Dis(SpirvBytecode.CreateBufferFromBytecode(spirvBytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex));
                }
            }
#else
            string shaderSourceFilename = null;
#endif

            // Select the correct backend compiler
            IShaderCompiler compiler;
            // Set to null if translator is not needed
            Backend? translatorBackend = null;
            switch (effectParameters.Platform)
            {
#if STRIDE_PLATFORM_DESKTOP
                case GraphicsPlatform.Direct3D11:
                case GraphicsPlatform.Direct3D12:
                    if (Platform.Type != PlatformType.Windows && Platform.Type != PlatformType.UWP)
                        throw new NotSupportedException();
                    compiler = new Direct3D.ShaderCompiler();
                    translatorBackend = Backend.Hlsl;
                    break;
#endif
                case GraphicsPlatform.Vulkan:
                    compiler = null;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var shaderStageBytecodes = new List<ShaderBytecode>();

#if STRIDE_PLATFORM_DESKTOP
            var stageStringBuilder = new StringBuilder();
#endif

            var bytecode = new EffectBytecode { Reflection = effectReflection, HashSources = usedHashSources };

            var shaderSourceText = new StringBuilder();

            if (translatorBackend != null)
            {
                var translator = new SpirvTranslator(spirvBytecode.ToArray().AsMemory().Cast<byte, uint>());
                var translatorEntryPoints = translator.GetEntryPoints();
                foreach (var entryPoint in translatorEntryPoints)
                {
                    var code = translator.Translate(Backend.Hlsl, entryPoint);

                    // Compile
                    // TODO: We could compile stages in different threads to improve compiler throughput?
                    var shaderStage = entryPoint.ExecutionModel switch
                    {
                        ExecutionModel.Vertex => ShaderStage.Vertex,
                        ExecutionModel.TessellationControl => ShaderStage.Hull,
                        ExecutionModel.TessellationEvaluation => ShaderStage.Domain,
                        ExecutionModel.Geometry => ShaderStage.Geometry,
                        ExecutionModel.Fragment => ShaderStage.Pixel,
                        ExecutionModel.GLCompute => ShaderStage.Compute,
                    };
                    var result = compiler.Compile(code, entryPoint.TranslatedName, shaderStage, effectParameters, bytecode.Reflection, shaderSourceFilename);
                    result.CopyTo(log);

                    shaderSourceText.AppendLine("// ==========================================");
                    shaderSourceText.AppendLine($"//         {shaderStage} shader");
                    shaderSourceText.AppendLine(code);
                    shaderSourceText.AppendLine();

                    shaderSourceText.AppendLine($"// {result.Messages.Count} errors & messages:");
                    foreach (var message in result.Messages)
                    {
                        shaderSourceText.AppendLine($"[{message.Type}] {message.Text}");
                        shaderSourceText.AppendLine();
                    }

                    if (result.HasErrors)
                    {
                        continue;
                    }

                    // -------------------------------------------------------
                    // Append bytecode id to shader log
#if STRIDE_PLATFORM_DESKTOP
                    stageStringBuilder.AppendLine("@G    {0} => {1}".ToFormat(shaderStage, result.Bytecode.Id));
                    if (result.DisassembleText != null)
                    {
                        stageStringBuilder.Append(result.DisassembleText);
                    }
#endif
                    // -------------------------------------------------------

                    shaderStageBytecodes.Add(result.Bytecode);

                    // When this is a compute shader, there is no need to scan other stages
                    if (shaderStage == ShaderStage.Compute)
                        break;
                }

                // Remove unused reflection data, as it is entirely resolved at compile time.
                CleanupReflection(bytecode.Reflection);
            }
            else
            {
                var spirvBytecodeArray = spirvBytecode.ToArray();
                var spirvBytecodeId = ObjectId.FromBytes(spirvBytecode);
                foreach (var entryPoint in entryPoints)
                {
                    var entryPointName = new byte[Encoding.UTF8.GetByteCount(entryPoint.Name) + 1];
                    entryPointName[^1] = 0;
                    Encoding.UTF8.GetBytes(entryPoint.Name.AsSpan(), entryPointName);

                    shaderStageBytecodes.Add(new ShaderBytecode
                    {
                        Stage = entryPoint.Stage,
                        Data = spirvBytecodeArray,
                        Id = spirvBytecodeId,
                        EntryPoint = entryPointName,
                    });
                }
            }

            bytecode.Stages = shaderStageBytecodes.ToArray();

#if STRIDE_PLATFORM_DESKTOP
            int shaderSourceLineOffset = 0;
            int shaderSourceCharacterOffset = 0;
            string outputShaderLog;
            lock (WriterLock) // protect write in case the same shader is created twice
            {
                var builder = new StringBuilder();
                builder.AppendLine("/**************************");
                builder.AppendLine("***** Compiler Parameters *****");
                builder.AppendLine("***************************");
                builder.Append("@P EffectName: ");
                builder.AppendLine(fullEffectName ?? "");
                builder.Append(compilerParameters?.ToStringPermutationsDetailed());
                builder.AppendLine("***************************");

                if (bytecode.Reflection.ConstantBuffers.Count > 0)
                {
                    builder.AppendLine("****  ConstantBuffers  ****");
                    builder.AppendLine("***************************");
                    foreach (var cBuffer in bytecode.Reflection.ConstantBuffers)
                    {
                        builder.AppendFormat("cbuffer {0} [Size: {1}]", cBuffer.Name, cBuffer.Size).AppendLine();
                        foreach (var parameter in cBuffer.Members)
                        {
                            builder.AppendFormat("@C    {0} => {1} [LogicalGroup: {2}]", parameter.RawName, parameter.KeyInfo.KeyName, parameter.LogicalGroup).AppendLine();
                        }
                    }
                    builder.AppendLine("***************************");
                }

                if (bytecode.Reflection.ResourceBindings.Count > 0)
                {
                    builder.AppendLine("******  Resources    ******");
                    builder.AppendLine("***************************");
                    foreach (var resource in bytecode.Reflection.ResourceBindings)
                    {
                        builder.AppendFormat("@R    {0} => {1} [LogicalGroup: {2} Stage: {3}, Slot: ({4}-{5})]", resource.RawName, resource.KeyInfo.KeyName, resource.LogicalGroup, resource.Stage, resource.SlotStart, resource.SlotStart + resource.SlotCount - 1).AppendLine();
                    }
                    builder.AppendLine("***************************");
                }

                if (bytecode.HashSources.Count > 0)
                {
                    builder.AppendLine("*****     Sources     *****");
                    builder.AppendLine("***************************");
                    foreach (var hashSource in bytecode.HashSources)
                    {
                        builder.AppendFormat("@S    {0} => {1}", hashSource.Key, hashSource.Value).AppendLine();
                    }
                    builder.AppendLine("***************************");
                }

                if (bytecode.Stages.Length > 0)
                {
                    builder.AppendLine("*****     Stages      *****");
                    builder.AppendLine("***************************");
                    builder.Append(stageStringBuilder);
                    builder.AppendLine("***************************");
                }
                builder.AppendLine("*************************/");

                shaderSourceCharacterOffset = builder.Length;

                // Re-append the shader with all informations
                builder.Append(shaderSourceText);

                outputShaderLog = builder.ToString();

                File.WriteAllText(shaderSourceFilename, outputShaderLog);
            }

            // Count lines till source start
            for (int i = 0; i < shaderSourceCharacterOffset-1;)
            {
                if (outputShaderLog[i] == '\r' && outputShaderLog[i + 1] == '\n')
                {
                    shaderSourceLineOffset++;
                    i += 2;
                }
                else
                    i++;
            }

            // Rewrite shader log
            Regex shaderLogReplace = new Regex(@"\.hlsl\((\d+),[0-9\-]+\):");
            foreach (var msg in log.Messages)
            {
                var match = shaderLogReplace.Match(msg.Text);
                if (match.Success)
                {
                    int line = int.Parse(match.Groups[1].Value);
                    line += shaderSourceLineOffset;

                    msg.Text = msg.Text.Remove(match.Groups[1].Index, match.Groups[1].Length)
                        .Insert(match.Groups[1].Index, line.ToString());
                }
            }
#endif

            return new EffectBytecodeCompilerResult(bytecode, log);
        }

        private static void CopyLogs(Stride.Core.Shaders.Utility.LoggerResult inputLog, LoggerResult outputLog)
        {
            foreach (var inputMessage in inputLog.Messages)
            {
                var logType = LogMessageType.Info;
                switch (inputMessage.Level)
                {
                    case ReportMessageLevel.Error:
                        logType = LogMessageType.Error;
                        break;
                    case ReportMessageLevel.Info:
                        logType = LogMessageType.Info;
                        break;
                    case ReportMessageLevel.Warning:
                        logType = LogMessageType.Warning;
                        break;
                }
                var outputMessage = new LogMessage(inputMessage.Span.ToString(), logType, string.Format(" {0}: {1}", inputMessage.Code, inputMessage.Text));
                outputLog.Log(outputMessage);
            }
            outputLog.HasErrors = inputLog.HasErrors;
        }

        private static void CleanupReflection(EffectReflection reflection)
        {
            // TODO GRAPHICS REFACTOR we hardcode several resource group we want to preserve or optimize completly
            // Somehow this should be handled some other place (or probably we shouldn't cleanup reflection at all?)
            bool hasMaterialGroup = false;
            bool hasLightingGroup = false;

            foreach (var resourceBinding in reflection.ResourceBindings)
            {
                if (resourceBinding.Stage != ShaderStage.None)
                {
                    if (!hasLightingGroup && resourceBinding.ResourceGroup == "PerLighting")
                        hasLightingGroup = true;
                    else if (!hasMaterialGroup && resourceBinding.ResourceGroup == "PerMaterial")
                        hasMaterialGroup = true;
                }
            }

            var usedConstantBuffers = new HashSet<string>();

            for (int i = reflection.ResourceBindings.Count - 1; i >= 0; i--)
            {
                var resourceBinding = reflection.ResourceBindings[i];

                // Do not touch anything if there is logical groups
                // TODO: We can do better than that: remove only if the full group can be optimized away
                if (resourceBinding.LogicalGroup != null)
                    continue;

                if (resourceBinding.Stage == ShaderStage.None && !(hasMaterialGroup && resourceBinding.ResourceGroup == "PerMaterial") && !(hasLightingGroup && resourceBinding.ResourceGroup == "PerLighting"))
                {
                    reflection.ResourceBindings.RemoveAt(i);
                }
                else if (resourceBinding.Class == EffectParameterClass.ConstantBuffer
                    || resourceBinding.Class == EffectParameterClass.TextureBuffer)
                {
                    // Mark associated cbuffer/tbuffer as used
                    usedConstantBuffers.Add(resourceBinding.RawName);
                }
            }

            // Remove unused cbuffer
            for (int i = reflection.ConstantBuffers.Count - 1; i >= 0; i--)
            {
                var cbuffer = reflection.ConstantBuffers[i];

                // Do not touch anything if there is logical groups
                // TODO: We can do better than that: remove only if the full group can be optimized away
                var hasLogicalGroup = false;
                foreach (var member in cbuffer.Members)
                {
                    if (member.LogicalGroup != null)
                    {
                        hasLogicalGroup = true;
                        break;
                    }
                }

                if (hasLogicalGroup)
                    continue;

                if (!usedConstantBuffers.Contains(cbuffer.Name))
                {
                    reflection.ConstantBuffers.RemoveAt(i);
                }
            }
        }
    }
}
