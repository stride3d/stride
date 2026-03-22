// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders.Compiler.Direct3D;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.Direct3D;
using Stride.Shaders.Compilers.SDSL;
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
    public partial class EffectCompiler : EffectCompilerBase
    {
        private bool d3dCompilerLoaded = false;
        private static readonly Object WriterLock = new Object();

        private FileShaderLoader shaderLoader;

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
            return GetFileShaderLoader().SourceManager.GetShaderSourceHash(type);
        }

        /// <summary>
        /// Remove cached files for modified shaders
        /// </summary>
        /// <param name="modifiedShaders"></param>
        public override void ResetCache(HashSet<string> modifiedShaders)
        {
            GetFileShaderLoader().SourceManager.DeleteObsoleteCache(modifiedShaders);
        }

        public FileShaderLoader GetFileShaderLoader()
        {
            lock (shaderMixinParserLock)
            {
                if (shaderLoader == null)
                {
                    shaderLoader = new FileShaderLoader(FileProvider);
                    shaderLoader.SourceManager.LookupDirectoryList.AddRange(SourceDirectories); // TODO: temp
                    shaderLoader.SourceManager.UseFileSystem = UseFileSystem;
                    shaderLoader.SourceManager.UrlToFilePath = UrlToFilePath; // TODO: temp
                }

                return shaderLoader;
            }
        }

        public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters, ObjectId mixinObjectId)
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

            var shaderMixer = new ShaderMixer(GetFileShaderLoader());
            shaderMixer.MergeSDSL(shaderMixinSource, new ShaderMixer.Options(effectParameters.Platform is not GraphicsPlatform.Vulkan), log, out var spirvBytecode, out var effectReflection, out var usedHashSources, out var entryPoints);

            // -------------------------------------------------------
            // Prepare DynamicCache folder for debug files
#if STRIDE_PLATFORM_DESKTOP
            var effectDir = Path.Combine(
                PlatformFolders.ApplicationCacheDirectory,
                EffectCompilerCache.GetEffectCacheDirectory(fullEffectName));
            if (!Directory.Exists(effectDir))
                Directory.CreateDirectory(effectDir);
#endif

            if (log.HasErrors)
            {
#if STRIDE_PLATFORM_DESKTOP
                if (spirvBytecode is { Length: > 0 })
                    lock (WriterLock)
                        WriteSpvDebugFiles(effectDir, mixinObjectId.ToString(), spirvBytecode.ToArray());
#endif
                return new EffectBytecodeCompilerResult(null, log);
            }

            // Select the correct backend compiler
            IShaderCompiler compiler;
            // Set to null if translator is not needed
            Backend? translatorBackend = null;
            switch (effectParameters.Platform)
            {
#if STRIDE_PLATFORM_DESKTOP
                case GraphicsPlatform.Direct3D11:
                    if (Platform.Type != PlatformType.Windows && Platform.Type != PlatformType.UWP)
                        throw new NotSupportedException();
                    compiler = new Direct3D.ShaderCompiler();
                    translatorBackend = Backend.Hlsl;
                    break;
#endif
                case GraphicsPlatform.Vulkan:
                case GraphicsPlatform.Direct3D12:
                    compiler = null;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var shaderStageBytecodes = new List<ShaderBytecode>();

#if STRIDE_PLATFORM_DESKTOP
            var stageStringBuilder = new StringBuilder();
            var stageHlslSources = new List<(string Suffix, string Code)>();
#endif

            var bytecode = new EffectBytecode { Reflection = effectReflection, HashSources = usedHashSources };
#if STRIDE_PLATFORM_DESKTOP
            var spirvBytecodeForDebug = spirvBytecode.ToArray();
#endif

            try
            {
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

#if STRIDE_PLATFORM_DESKTOP
                        var stageSuffix = shaderStage switch
                        {
                            ShaderStage.Vertex => "vs",
                            ShaderStage.Hull => "hs",
                            ShaderStage.Domain => "ds",
                            ShaderStage.Geometry => "gs",
                            ShaderStage.Pixel => "ps",
                            ShaderStage.Compute => "cs",
                        };
                        stageHlslSources.Add((stageSuffix, code));
                        string stageFilename = null;
#else
                        string stageFilename = null;
#endif
                        var result = compiler.Compile(code, entryPoint.TranslatedName, shaderStage, effectParameters, bytecode.Reflection, stageFilename);
                        result.CopyTo(log);

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
                // TODO: Move that code inside ShaderCompiler (need a new interface for processing SPIR-V)
                else if (effectParameters.Platform == GraphicsPlatform.Direct3D12)
                {
                    // Check API
                    Spv2DXIL.spirv_to_dxil_get_version();
                    foreach (var entryPoint in entryPoints)
                    {
                        unsafe
                        {
                            fixed (byte* shaderData = spirvBytecode)
                            {
                                var debugOptions = new DebugOptions();
                                var runtimeConf = new RuntimeConf
                                {
                                    runtime_data_cbv = { base_shader_register = 0, register_space = 31 },
                                    //first_vertex_and_base_instance_mode = SysvalType.Zero,
                                    yzflip_mode = FlipMode.YZFlipNone,
                                    shader_model_max = dxil_shader_model.SHADER_MODEL_6_0,
                                };
                                var logger = new DXILSpirvLogger();
                                var result = Spv2DXIL.spirv_to_dxil((uint*)shaderData, spirvBytecode.Length / 4,
                                    null, 0,
                                    entryPoint.Stage switch
                                    {
                                        ShaderStage.Vertex => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_VERTEX,
                                        ShaderStage.Hull => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_TESS_CTRL,
                                        ShaderStage.Domain => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_TESS_CTRL,
                                        ShaderStage.Geometry => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_GEOMETRY,
                                        ShaderStage.Pixel => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_FRAGMENT,
                                        ShaderStage.Compute => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_COMPUTE,
                                    },
                                    entryPoint.Name,
                                    ValidatorVersion.DXIL_VALIDATOR_1_4,
                                    ref debugOptions, ref runtimeConf, ref logger, out var dxil);

                                Span<byte> dxilSpan = new(dxil.buffer, (int)dxil.size);
                                fixed (byte* dxilSpanPtr = dxilSpan)
                                    DxilHash.ComputeHashRetail(&dxilSpanPtr[20], (uint)(dxilSpan.Length - 20), &dxilSpanPtr[4]);
                                shaderStageBytecodes.Add(new ShaderBytecode(entryPoint.Stage, ObjectId.FromBytes(dxilSpan), dxilSpan.ToArray()));
                            }
                        }
                    }
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
            }
            catch (Exception)
            {
#if STRIDE_PLATFORM_DESKTOP
                lock (WriterLock)
                    WriteSpvDebugFiles(effectDir, mixinObjectId.ToString(), spirvBytecodeForDebug);
#endif
                throw;
            }

            bytecode.Stages = shaderStageBytecodes.ToArray();

            // -------------------------------------------------------
            // Write debug files using output hash (content-addressed, shared across inputs with same output)
#if STRIDE_PLATFORM_DESKTOP
            var outputHash = bytecode.ComputeId();
            var shaderBaseFilename = Path.Combine(effectDir, outputHash.ToString());
            lock (WriterLock) // protect write in case the same shader is created twice
            {
                // Note: we used input hash in previous case because bytecode was not valid, but when available we prefer bytecode hash
                WriteSpvDebugFiles(effectDir, outputHash.ToString(), spirvBytecodeForDebug);

                // Write per-stage HLSL
                foreach (var (suffix, hlslCode) in stageHlslSources)
                {
                    var stageFile = $"{shaderBaseFilename}_{suffix}.hlsl";
                    if (!File.Exists(stageFile))
                        File.WriteAllText(stageFile, hlslCode);
                }

                if (!File.Exists(shaderBaseFilename + "_meta.txt"))
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("/**************************");
                    builder.AppendLine("***** Compiler Parameters *****");
                    builder.AppendLine("***************************");
                    builder.Append("@P EffectName: ");
                    builder.AppendLine(fullEffectName ?? "");
                    builder.Append(compilerParameters?.ToStringPermutationsDetailed());
                    builder.AppendLine("***************************");
                    builder.AppendLine("****   Shader Source   ****");
                    builder.AppendLine("***************************");
                    builder.AppendLine("// ShaderSource in C# that generated this EffectBytecode. You can copy-psate it to reproduce it easily in a unit test.");
                    builder.AppendLine("// Note: Other slightly different ShaderSource inputs might produce the same EffectBytecode, only first one is saved here.");
                    builder.Append("var shaderSource = ");
                    builder.AppendLine(mixinTree.ToCode());
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

                    File.WriteAllText(shaderBaseFilename + "_meta.txt", builder.ToString());
                }
            }
#endif

            return new EffectBytecodeCompilerResult(bytecode, log);
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

#if STRIDE_PLATFORM_DESKTOP
        /// <summary>
        /// Writes .spv and .spvdis files. Caller must hold WriterLock.
        /// </summary>
        private static void WriteSpvDebugFiles(string effectDir, string hashName, byte[] spirvBytecode)
        {
            var baseFilename = Path.Combine(effectDir, hashName);
            if (!File.Exists(baseFilename + ".spv"))
            {
                File.WriteAllBytes(baseFilename + ".spv", spirvBytecode);
                File.WriteAllText(baseFilename + ".spvdis", Spirv.Tools.Spv.Dis(SpirvBytecode.CreateFromSpan(spirvBytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex));
            }
        }
#endif
    }
}
