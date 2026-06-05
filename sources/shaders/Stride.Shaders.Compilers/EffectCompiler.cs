// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Stride.Shaders.Spirv.Processing.Interfaces;
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

        private FileShaderLoader? shaderLoader;

        private readonly object shaderMixinParserLock = new object();

        public List<string> SourceDirectories { get; private set; }

        public Dictionary<string, string> UrlToFilePath { get; private set; }

        public override IVirtualFileProvider FileProvider { get; set; }
        public bool UseFileSystem { get; set; }

        /// <summary>
        /// When true, runs spirv-val on the SPIR-V bytecode after MergeSDSL.
        /// Validation errors block the compile via <c>log.Error</c>.
        /// Default: on in Debug builds, off in Release.
        /// </summary>
        public bool ValidateSpirv { get; set; }
#if DEBUG
            = true;
#endif

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
            if (!shaderMixer.MergeSDSL(shaderMixinSource, new ShaderMixer.Options(
                // D3D12 also goes through SPIR-V (then DXIL via mesa), so it needs the unified
                // binding scheme — only D3D11/FXC consumes the per-class b#/t#/u#/s# bank style.
                ResourcesRegisterSeparate: effectParameters.Platform is GraphicsPlatform.Direct3D11,
                StripGoogleUserType: effectParameters.Platform is GraphicsPlatform.Vulkan), log, out var spirvBytecode, out var effectReflection, out var usedHashSources, out var entryPoints))
                return new EffectBytecodeCompilerResult(null, log);

            // Optional SPIR-V validation (requires spirv-val from Vulkan SDK)
            if (ValidateSpirv && spirvBytecode is { Length: > 0 })
            {
                var validationResult = Spirv.Tools.Spv.ValidateBinary(spirvBytecode, targetVulkan: effectParameters.Platform is GraphicsPlatform.Vulkan);
                if (!validationResult.IsValid)
                    log.Error($"SPIR-V validation failed for effect {fullEffectName} (id: {mixinObjectId}): {validationResult.Output}");
            }

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

            // Select the correct backend compiler. D3D11 is the only platform that needs
            // SPIRV→HLSL via SPIRV-Cross today; Vulkan/D3D12 consume SPIR-V directly.
            IShaderCompiler? compiler;
            bool useSpirvCrossToHlsl = false;
            switch (effectParameters.Platform)
            {
#if STRIDE_PLATFORM_DESKTOP
                case GraphicsPlatform.Direct3D11:
                    if (Platform.Type != PlatformType.Windows && Platform.Type != PlatformType.UWP)
                        throw new NotSupportedException();
                    compiler = new Direct3D.ShaderCompiler();
                    useSpirvCrossToHlsl = true;
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
                if (useSpirvCrossToHlsl)
                {
                    // Legalize for SPIRV-Cross HLSL emission: const folding, DCE,
                    // SSA promotion, inlining. Avoids SPIRV-Cross emitting
                    // `if (true)` dead code from generic-template constants and
                    // FXC's 'argument pulled into unrelated predicate' on
                    // Prepare/Compute helpers over a static stream struct.
                    var legalizedSpirv = SpirvTools.LegalizeForHlsl(MemoryMarshal.Cast<byte, uint>(spirvBytecode));
                    var translator = new SpirvTranslator(legalizedSpirv.AsMemory());
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
                            _ => throw new NotSupportedException($"Unsupported execution model: {entryPoint.ExecutionModel}"),
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
                            _ => throw new NotSupportedException($"Unsupported shader stage: {shaderStage}"),
                        };
                        stageHlslSources.Add((stageSuffix, code));
                        string? stageFilename = null;
#else
                        string? stageFilename = null;
#endif
                        var result = compiler!.Compile(code, entryPoint.TranslatedName, shaderStage, effectParameters, bytecode.Reflection, stageFilename);
                        result.CopyTo(log);

                        if (result.HasErrors)
                        {
                            continue;
                        }

                        // Guard against a silent null bytecode (actionable message instead of NRE below).
                        if (result.Bytecode is null)
                        {
                            log.Error($"Shader compilation for stage {shaderStage} (entry '{entryPoint.TranslatedName}') produced no bytecode and no error.");
                            continue;
                        }

                        // -------------------------------------------------------
                        // Append bytecode id to shader log
#if STRIDE_PLATFORM_DESKTOP
                        stageStringBuilder.AppendLine($"@G    {shaderStage} => {result.Bytecode!.Id}");
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
                }
                // TODO: Move that code inside ShaderCompiler (need a new interface for processing SPIR-V)
                else if (effectParameters.Platform == GraphicsPlatform.Direct3D12)
                {
#if STRIDE_PLATFORM_DESKTOP
                    if (OperatingSystem.IsWindows())
                    {
                        // Check API
                        Spv2DXIL.spirv_to_dxil_get_version();
                        CompileDxilPipeline(spirvBytecode, entryPoints, shaderStageBytecodes);
                    }
                    else
#endif
                    {
                        throw new NotImplementedException("D3D12 shader compilation is not supported on this platform");
                    }
                }
                else
                {
                    var spirvBytecodeArray = spirvBytecode.ToArray();
                    var spirvBytecodeId = ObjectId.FromBytes(spirvBytecodeArray);
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
                    builder.AppendLine($"@P EffectName: {fullEffectName ?? "(none)"}");
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
                            builder.AppendLine($"cbuffer {cBuffer.Name} [Size: {cBuffer.Size}]");
                            foreach (var parameter in cBuffer.Members)
                            {
                                builder.AppendLine($"@C    {parameter.RawName} => {parameter.KeyInfo.KeyName} [LogicalGroup: {parameter.LogicalGroup ?? "(none)"}]");
                            }
                        }
                        builder.AppendLine("***************************");
                    }

                    if (bytecode.Reflection.ResourceBindings.Count > 0)
                    {
                        builder.AppendLine("******** Resources ********");
                        builder.AppendLine("***************************");

                        // Build sampler state lookup from ResourceGroups entries
                        var samplerStateByRawName = new Dictionary<string, Graphics.SamplerStateDescription>();
                        foreach (var group in bytecode.Reflection.ResourceGroups)
                            foreach (var entry in group.Entries)
                                if (entry.SamplerStateDescription.HasValue)
                                    samplerStateByRawName[entry.RawName] = entry.SamplerStateDescription.Value;

                        // Aggregate stages per resource (keyed by RawName) so we can show a bitfield instead of duplicates
                        var stagesByRawName = new Dictionary<string, (EffectResourceBindingDescription Binding, List<ShaderStage> Stages)>();
                        foreach (var resource in bytecode.Reflection.ResourceBindings)
                        {
                            if (resource.Stage == ShaderStage.None)
                                continue; // metadata-only entry, will be merged via KeyName
                            if (!stagesByRawName.TryGetValue(resource.RawName, out var entry))
                                stagesByRawName[resource.RawName] = (resource, new List<ShaderStage> { resource.Stage });
                            else
                                entry.Stages.Add(resource.Stage);
                        }
                        // For resources that only have Stage:None (no per-stage entry), include them too
                        foreach (var resource in bytecode.Reflection.ResourceBindings)
                        {
                            if (resource.Stage == ShaderStage.None && !stagesByRawName.ContainsKey(resource.RawName))
                                stagesByRawName[resource.RawName] = (resource, new List<ShaderStage>());
                        }

                        // Build cbuffer lookup by name
                        var cbuffersByName = new Dictionary<string, EffectConstantBufferDescription>();
                        foreach (var cb in bytecode.Reflection.ConstantBuffers)
                            cbuffersByName[cb.Name] = cb;

                        // Group by ResourceGroup, then LogicalGroup
                        var byResourceGroup = new Dictionary<string, List<(EffectResourceBindingDescription Binding, string StageStr)>>();
                        foreach (var (rawName, (binding, stages)) in stagesByRawName)
                        {
                            var rgKey = binding.ResourceGroup ?? "";
                            if (!byResourceGroup.TryGetValue(rgKey, out var list))
                                byResourceGroup[rgKey] = list = new();
                            var stageStr = stages.Count > 0 ? string.Join("|", stages.Select(s => s switch { ShaderStage.Vertex => "VS", ShaderStage.Hull => "HS", ShaderStage.Domain => "DS", ShaderStage.Geometry => "GS", ShaderStage.Pixel => "PS", ShaderStage.Compute => "CS", _ => s.ToString() })) : "None";
                            list.Add((binding, stageStr));
                        }

                        foreach (var (resourceGroup, resources) in byResourceGroup)
                        {
                            builder.AppendLine($"ResourceGroup: {(string.IsNullOrEmpty(resourceGroup) ? "(Default)" : resourceGroup)}");

                            // Group by LogicalGroup
                            var byLogicalGroup = new Dictionary<string, List<(EffectResourceBindingDescription Binding, string StageStr)>>();
                            foreach (var r in resources)
                            {
                                var lgKey = r.Binding.LogicalGroup ?? "";
                                if (!byLogicalGroup.TryGetValue(lgKey, out var list))
                                    byLogicalGroup[lgKey] = list = new();
                                list.Add(r);
                            }

                            foreach (var (logicalGroup, lgResources) in byLogicalGroup)
                            {
                                builder.AppendLine($"  LogicalGroup: {(string.IsNullOrEmpty(logicalGroup) ? "(Default)" : logicalGroup)}");

                                foreach (var (binding, stageStr) in lgResources)
                                {
                                    if (binding.Class == EffectParameterClass.ConstantBuffer && cbuffersByName.TryGetValue(binding.RawName, out var cb))
                                    {
                                        // Find members belonging to this logical group and compute offset/size range
                                        var lgMembers = new List<EffectValueDescription>();
                                        foreach (var m in cb.Members)
                                        {
                                            var memberLg = m.LogicalGroup ?? "";
                                            if (memberLg == logicalGroup)
                                                lgMembers.Add(m);
                                        }
                                        if (lgMembers.Count > 0)
                                        {
                                            var minOffset = lgMembers.Min(m => m.Offset);
                                            var maxEnd = lgMembers.Max(m => m.Offset + m.Size);
                                            builder.AppendLine($"    cbuffer {cb.Name} [slot: {binding.SlotStart}-{binding.SlotStart + binding.SlotCount - 1}, offset: {minOffset}, size: {maxEnd - minOffset}, stage: {stageStr}]");
                                            foreach (var m in lgMembers)
                                                builder.AppendLine($"      {m.RawName} => {m.KeyInfo.KeyName} (offset: {m.Offset}, size: {m.Size})");
                                        }
                                        else
                                        {
                                            builder.AppendLine($"    cbuffer {cb.Name} [slot: {binding.SlotStart}-{binding.SlotStart + binding.SlotCount - 1}, stage: {stageStr}]");
                                        }
                                    }
                                    else
                                    {
                                        // Texture, sampler, buffer, etc.
                                        var slotPrefix = binding.Class switch
                                        {
                                            EffectParameterClass.ShaderResourceView => "t",
                                            EffectParameterClass.UnorderedAccessView => "u",
                                            EffectParameterClass.Sampler => "s",
                                            _ => $"slot {binding.SlotStart}"
                                        };
                                        var slotStr = slotPrefix is "t" or "u" or "s" ? $"{slotPrefix}{binding.SlotStart}" : slotPrefix;
                                        var samplerInfo = binding.Class == EffectParameterClass.Sampler && samplerStateByRawName.TryGetValue(binding.RawName, out var sd)
                                            ? $" {{Filter={sd.Filter}, Compare={sd.CompareFunction}}}"
                                            : binding.Class == EffectParameterClass.Sampler ? " {NO SAMPLER STATE}" : "";
                                        builder.AppendLine($"    {binding.RawName} => {binding.KeyInfo.KeyName} [{slotStr}, stage: {stageStr}]{samplerInfo}");
                                    }
                                }
                            }
                        }
                        builder.AppendLine("****************************");
                    }

                    if (bytecode.HashSources.Count > 0)
                    {
                        builder.AppendLine("*****     Sources     *****");
                        builder.AppendLine("***************************");
                        foreach (var hashSource in bytecode.HashSources)
                        {
                            builder.AppendLine($"@S    {hashSource.Key} => {hashSource.Value}");
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

#if STRIDE_PLATFORM_DESKTOP
        /// <summary>
        /// Compile SPIR-V to DXIL for all stages in a single linked call. Needed so spirv_to_dxil
        /// can match PS inputs to VS outputs correctly when some varyings are unused.
        /// </summary>
        private static unsafe void CompileDxilPipeline(ReadOnlySpan<byte> spirvBytecode, List<InterfaceProcessor.EntryPointInfo> entryPoints, List<ShaderBytecode> shaderStageBytecodes)
        {
            var runtimeConf = new RuntimeConf
            {
                // Mesa-injected CBVs for sysvals/push-constants. Both go in a high register space
                // so they can't collide with Stride's resources (which all live in space 0).
                runtime_data_cbv = { base_shader_register = 0, register_space = 31 },
                push_constant_cbv = { base_shader_register = 1, register_space = 31 },
                yzflip_mode = FlipMode.YZFlipNone,
                // SM 6.2 minimum so mesa can lower native 16-bit types (Float16/Int16) when a
                // shader uses `half`. Mesa only ramps individual shaders up to 6.2 on demand.
                shader_model_max = dxil_shader_model.SHADER_MODEL_6_2,
            };
            _spvLogSink?.Clear();
            var logger = new DXILSpirvLogger { log = &SpvLogCallback };

            // Allocate native buffers for entry point names (UTF-8 null-terminated)
            var entryPointNameBuffers = new byte[entryPoints.Count][];
            for (int i = 0; i < entryPoints.Count; i++)
            {
                var name = entryPoints[i].Name;
                var bytes = new byte[Encoding.UTF8.GetByteCount(name) + 1];
                Encoding.UTF8.GetBytes(name, bytes);
                entryPointNameBuffers[i] = bytes;
            }

            var stages = stackalloc SpirvStageInput[entryPoints.Count];
            var outputs = stackalloc DXILSpirvObject[entryPoints.Count];

            fixed (byte* shaderData = spirvBytecode)
            {
                // Pin entry point name buffers
                var nameHandles = new System.Runtime.InteropServices.GCHandle[entryPoints.Count];
                try
                {
                    for (int i = 0; i < entryPoints.Count; i++)
                    {
                        nameHandles[i] = System.Runtime.InteropServices.GCHandle.Alloc(entryPointNameBuffers[i], System.Runtime.InteropServices.GCHandleType.Pinned);
                        stages[i] = new SpirvStageInput
                        {
                            words = (uint*)shaderData,
                            word_count = spirvBytecode.Length / 4,
                            stage = entryPoints[i].Stage switch
                            {
                                ShaderStage.Vertex => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_VERTEX,
                                ShaderStage.Hull => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_TESS_CTRL,
                                ShaderStage.Domain => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_TESS_EVAL,
                                ShaderStage.Geometry => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_GEOMETRY,
                                ShaderStage.Pixel => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_FRAGMENT,
                                ShaderStage.Compute => Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_COMPUTE,
                                _ => throw new NotSupportedException($"Unsupported shader stage: {entryPoints[i].Stage}"),
                            },
                            entry_point_name = (byte*)nameHandles[i].AddrOfPinnedObject(),
                        };
                    }

                    if (!Spv2DXIL.spirv_to_dxil_pipeline(stages, entryPoints.Count, ValidatorVersion.DXIL_VALIDATOR_1_4, ref runtimeConf, ref logger, outputs))
                    {
                        var dumpPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"stride-dxil-fail-{Guid.NewGuid():N}.spv");
                        System.IO.File.WriteAllBytes(dumpPath, spirvBytecode.ToArray());
                        var diag = _spvLogSink is { Length: > 0 } sb ? sb.ToString().TrimEnd() : "(no diagnostics from spirv_to_dxil)";
                        throw new InvalidOperationException($"spirv_to_dxil_pipeline failed; SPIR-V dumped to {dumpPath}\n{diag}");
                    }

                    for (int i = 0; i < entryPoints.Count; i++)
                    {
                        var dxil = outputs[i];
                        Span<byte> dxilSpan = new(dxil.buffer, (int)dxil.size);
                        fixed (byte* dxilSpanPtr = dxilSpan)
                            DxilHash.ComputeHashRetail(&dxilSpanPtr[20], (uint)(dxilSpan.Length - 20), &dxilSpanPtr[4]);
                        shaderStageBytecodes.Add(new ShaderBytecode(entryPoints[i].Stage, ObjectId.FromBytes(dxilSpan), dxilSpan.ToArray()));
                    }
                }
                finally
                {
                    for (int i = 0; i < entryPoints.Count; i++)
                        if (nameHandles[i].IsAllocated) nameHandles[i].Free();
                }
            }
        }

        // Thread-local sink for messages mesa logs through DXILSpirvLogger.log during a single
        // CompileDxilPipeline call. Drained into the exception text on failure.
        [ThreadStatic] private static StringBuilder _spvLogSink;

        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static unsafe void SpvLogCallback(void* priv, byte* message)
        {
            if (message is null)
                return;
            var sink = _spvLogSink ??= new StringBuilder();
            int len = 0;
            while (message[len] != 0) len++;
            sink.AppendLine(Encoding.UTF8.GetString(message, len));
        }

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
