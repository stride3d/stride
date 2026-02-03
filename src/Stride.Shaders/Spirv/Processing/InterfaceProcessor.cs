using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System.IO;
using Stride.Shaders.Parsing.Analysis;
using static Stride.Shaders.Spirv.Specification;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CommunityToolkit.HighPerformance;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Models;

namespace Stride.Shaders.Spirv.Processing
{
    /// <summary>
    /// Help to process streams and simplify the interface (resources, methods, cbuffer) of the shader.
    /// </summary>
    public class InterfaceProcessor
    {
        public delegate void CodeInsertedDelegate(int index, int count);

        public CodeInsertedDelegate CodeInserted { get; set; }

        public record Result(List<(string Name, int Id, ShaderStage Stage)> EntryPoints, List<ShaderInputAttributeDescription> InputAttributes);

        Symbol? ResolveEntryPoint(SymbolTable table, string name)
        {
            table.TryResolveSymbol(name, out var entryPoint);
            return entryPoint?.Type switch
            {
                FunctionGroupType => entryPoint.GroupMembers[^1],
                _ => entryPoint
            };
        }
        
        public Result Process(SymbolTable table, NewSpirvBuffer buffer, SpirvContext context)
        {
            var entryPoints = new List<(string Name, int Id, ShaderStage Stage)>();

            var entryPointVS = ResolveEntryPoint(table, "VSMain");
            var entryPointHS = ResolveEntryPoint(table, "HSMain");
            var entryPointDS = ResolveEntryPoint(table, "DSMain");
            var entryPointGS = ResolveEntryPoint(table, "GSMain");
            var entryPointPS = ResolveEntryPoint(table, "PSMain");
            var entryPointCS = ResolveEntryPoint(table, "CSMain");

            var entryPointPSOrCS = entryPointCS ?? entryPointPS ?? throw new InvalidOperationException($"{nameof(InterfaceProcessor)}: At least a pixel or compute shader is expected");
            if (entryPointPS == null && entryPointCS == null)
                throw new InvalidOperationException($"{nameof(InterfaceProcessor)}: Found both a pixel and a compute shader");

            var analysisResult = Analyze(buffer, context);
            MergeSameSemanticVariables(table, context, buffer, analysisResult);
            var streams = analysisResult.Streams;

            var liveAnalysis = new LiveAnalysis();
            AnalyzeStreamReadWrites(buffer, context, entryPointPSOrCS.IdRef, analysisResult, liveAnalysis);

            if (entryPointCS != null)
            {
                (var csWrapperId, var csWrapperName) = GenerateStreamWrapper(table, buffer, context, ExecutionModel.GLCompute, entryPointCS, analysisResult, liveAnalysis, false);
                entryPoints.Add((csWrapperName, csWrapperId, ShaderStage.Compute));
            }
            
            if (entryPointHS != null || entryPointDS != null)
                context.Add(new OpCapability(Capability.Tessellation));
            else if (entryPointGS != null)
                context.Add(new OpCapability(Capability.Geometry));

            var inputAttributes = new List<ShaderInputAttributeDescription>();
            
            if (entryPointPS != null)
            {
                // If written to, they are expected at the end of pixel shader
                foreach (var stream in streams)
                {
                    if (stream.Value.Semantic is { } semantic)
                    {
                        if ((semantic.ToUpperInvariant().StartsWith("SV_TARGET") || semantic.ToUpperInvariant() == "SV_DEPTH") && stream.Value.Write)
                            stream.Value.Output = true;
                    }
                }

                // Check if there is any output
                // (if PSMain has been overriden with an empty method, it means we don't want to output anything and remove the pixel shader, i.e. for shadow caster)
                if (streams.Any(x => x.Value.Output))
                {
                    (var psWrapperId, var psWrapperName) = GenerateStreamWrapper(table, buffer, context, ExecutionModel.Fragment, entryPointPS, analysisResult, liveAnalysis, false);
                    entryPoints.Add((psWrapperName, psWrapperId, ShaderStage.Pixel));

                    buffer.Add(new OpExecutionMode(psWrapperId, ExecutionMode.OriginUpperLeft, []));
                }

                // Those semantic variables are implicit in pixel shader, no need to forward them from previous stages
                foreach (var stream in streams)
                {
                    if (stream.Value.Semantic is { } semantic && (semantic.ToUpperInvariant() == "SV_COVERAGE" || semantic.ToUpperInvariant() == "SV_ISFRONTFACE" || semantic.ToUpperInvariant() == "VFACE"))
                        stream.Value.Read = false;
                }

                // Reset cbuffer/resource/methods used for next stage
                ResetUsedThisStage(analysisResult, liveAnalysis);

                PropagateStreamsFromPreviousStage(streams);

                foreach (var entryPoint in new[] { (ExecutionModel.TessellationControl, entryPointHS), (ExecutionModel.TessellationEvaluation, entryPointDS), (ExecutionModel.Geometry, entryPointGS) })
                {
                    if (entryPoint.Item2 != null)
                    {
                        AnalyzeStreamReadWrites(buffer, context, entryPoint.Item2.IdRef, analysisResult, liveAnalysis);

                        // Find patch constant entry point and process it as well
                        var patchConstantEntryPoint = entryPoint.Item1 == ExecutionModel.TessellationControl ? ResolveHullPatchConstantEntryPoint(table, context, entryPoint.Item2) : null;
                        if (patchConstantEntryPoint != null)
                            AnalyzeStreamReadWrites(buffer, context, patchConstantEntryPoint.IdRef, analysisResult, liveAnalysis);
                    
                        // If specific semantic are written to (i.e. SV_Position), they are expected at the end of vertex shader
                        foreach (var stream in streams)
                        {
                            if (stream.Value.Semantic is { } semantic)
                            {
                                if (semantic.ToUpperInvariant().StartsWith("SV_POSITION"))
                                    stream.Value.Output = true;
                                
                                if (entryPoint.Item1 == ExecutionModel.TessellationControl
                                    && (semantic.ToUpperInvariant().StartsWith("SV_TESSFACTOR") || semantic.ToUpperInvariant().StartsWith("SV_INSIDETESSFACTOR")))
                                    stream.Value.Output = true;
                            }
                        }
                    
                        (var wrapperId, var wrapperName) = GenerateStreamWrapper(table, buffer, context, entryPoint.Item1, entryPoint.Item2, analysisResult, liveAnalysis, false);
                        var stage = entryPoint.Item1 switch
                        {
                            ExecutionModel.TessellationControl => ShaderStage.Hull,
                            ExecutionModel.TessellationEvaluation => ShaderStage.Domain,
                            ExecutionModel.Geometry => ShaderStage.Geometry,
                        };
                        entryPoints.Add((wrapperName, wrapperId, stage));
                    
                        // Reset cbuffer/resource/methods used for next stage
                        ResetUsedThisStage(analysisResult, liveAnalysis);
                    
                        PropagateStreamsFromPreviousStage(streams);
                    
                        if (entryPointVS == null)
                            throw new InvalidOperationException($"{nameof(InterfaceProcessor)}: If a {stage} shader is specified, a vertex shader is needed too");
                    }
                }
                
                if (entryPointVS != null)
                {
                    AnalyzeStreamReadWrites(buffer, context, entryPointVS.IdRef, analysisResult, liveAnalysis);

                    // If specific semantic are written to (i.e. SV_Position), they are expected at the end of vertex shader
                    foreach (var stream in streams)
                    {
                        if (stream.Value.Semantic is { } semantic && semantic.ToUpperInvariant().StartsWith("SV_POSITION"))
                            stream.Value.Output = true;
                    }

                    (var vsWrapperId, var vsWrapperName) = GenerateStreamWrapper(table, buffer, context, ExecutionModel.Vertex, entryPointVS, analysisResult, liveAnalysis, true);
                    entryPoints.Add((vsWrapperName, vsWrapperId, ShaderStage.Vertex));
                    
                    // Process shader input attributes
                    foreach (var stream in streams)
                    {
                        // Note: built-ins won't have a inputLayoutLocation so they will be skipped
                        if (stream.Value.Input && stream.Value.InputLayoutLocation is {} inputLayoutLocation)
                        {
                            if (stream.Value.Semantic == null)
                                throw new InvalidOperationException($"Vertex shader input {stream.Value.Name} doesn't have semantic");
                            var semantic = ParseSemantic(stream.Value.Semantic);
                            inputAttributes.Add(new ShaderInputAttributeDescription { Location = inputLayoutLocation, SemanticName = semantic.Name, SemanticIndex = semantic.Index });
                        }
                    }
                }
            }

            // This will remove a lot of unused methods, resources and variables
            // (while following proper rules to preserve rgroup, cbuffer, logical groups, etc.)
            RemoveUnreferencedCode(buffer, context, analysisResult, streams, liveAnalysis);

            return new(entryPoints, inputAttributes);
        }

        private static void ResetUsedThisStage(AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
        {
            foreach (var variable in analysisResult.Variables)
                variable.Value.UsedThisStage = false;
            foreach (var resource in analysisResult.Resources)
                resource.Value.UsedThisStage = false;
            foreach (var cbuffer in analysisResult.CBuffers)
                cbuffer.Value.UsedThisStage = false;
            foreach (var method in liveAnalysis.ReferencedMethods)
            {
                method.Value.UsedThisStage = false;
                method.Value.ThisStageMethodId = null;
            }
        }

        private static void RemoveUnreferencedCode(NewSpirvBuffer buffer, SpirvContext context, AnalysisResult analysisResult, Dictionary<int, StreamVariableInfo> streams, LiveAnalysis liveAnalysis)
        {
            // Remove unreferenced code
            var removedIds = new HashSet<int>();

            // First, build resource group (used status and logical groups)
            var logicalGroups = new Dictionary<string, LogicalGroupInfo>();
            foreach (var resourceGroup in analysisResult.ResourceGroups)
            {
                foreach (var resource in resourceGroup.Value.Resources)
                {
                    if (resource.UsedAnyStage)
                    {
                        resourceGroup.Value.Used = true;
                    }

                    if (resourceGroup.Value.LogicalGroup != null)
                    {
                        ref var logicalGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(logicalGroups, $"{resourceGroup.Value.Name}.{resourceGroup.Value.LogicalGroup}", out var exists);
                        if (!exists)
                            logicalGroup = new();
                        logicalGroup.Resources.Add(resourceGroup.Value);
                    }
                }
            }
            // Complete logical groups with cbuffers
            foreach (var cbuffer in analysisResult.CBuffers)
            {
                if (cbuffer.Value.LogicalGroup != null)
                {
                    ref var logicalGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(logicalGroups, $"{cbuffer.Value.Name}.{cbuffer.Value.LogicalGroup}", out var exists);
                    if (!exists)
                        logicalGroup = new();
                    logicalGroup.CBuffers.Add(cbuffer.Value);
                }
            }

            // Check logical group: if any resource is used, mark everything as used
            // TODO: make sure register allocation is contiguous
            foreach (var logicalGroup in logicalGroups)
            {
                var logicalGroupUsed = false;
                foreach (var resource in logicalGroup.Value.Resources)
                    logicalGroupUsed |= resource.Used;
                foreach (var cbuffer in logicalGroup.Value.CBuffers)
                    logicalGroupUsed |= cbuffer.UsedAnyStage;

                if (logicalGroupUsed)
                {
                    // Mark everything as used
                    foreach (var resource in logicalGroup.Value.Resources)
                        resource.Used = logicalGroupUsed;
                    foreach (var cbuffer in logicalGroup.Value.CBuffers)
                        cbuffer.UsedThisStage = logicalGroupUsed;
                }
            }

            for (var index = 0; index < buffer.Count; index++)
            {
                var i = buffer[index];
                if (i.Op == Op.OpFunction && (OpFunction)i is { } function)
                {
                    bool isReferenced = liveAnalysis.ReferencedMethods.ContainsKey(function.ResultId)
                        || liveAnalysis.ExtraReferencedMethods.Contains(function.ResultId);
                    if (!isReferenced)
                    {
                        removedIds.Add(function.ResultId);
                        while (buffer[index].Op != Op.OpFunctionEnd)
                        {
                            if (buffer[index].Data.IdResult is int resultId)
                                removedIds.Add(resultId);
                            SpirvBuilder.SetOpNop(buffer[index++].Data.Memory.Span);
                        }

                        SpirvBuilder.SetOpNop(buffer[index].Data.Memory.Span);
                    }
                }

                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                    {
                        Storageclass: Specification.StorageClass.Uniform,
                        ResultId: int
                    } variable
                    && analysisResult.CBuffers.TryGetValue(variable, out var cbufferInfo))
                {
                    if (!cbufferInfo.UsedAnyStage)
                    {
                        removedIds.Add(variable.ResultId);
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                    }
                }

                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                    {
                        Storageclass: Specification.StorageClass.UniformConstant,
                        ResultId: int
                    } resource)
                {
                    var resourceInfo = analysisResult.Resources[resource.ResultId];
                    // If resource has a rgroup, check its state (if any resource is used in the group, we need to keep every resource)
                    // If no rgroup, we check the resource itself
                    if (!(resourceInfo.ResourceGroup?.Used ?? false || resourceInfo.UsedAnyStage))
                    {
                        removedIds.Add(resource.ResultId);
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                    }
                }

                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                    {
                        Storageclass: Specification.StorageClass.Private or Specification.StorageClass.Workgroup,
                        ResultId: int
                    } variable2)
                {
                    if (variable2.Flags.HasFlag(VariableFlagsMask.Stream))
                    {
                        // Always removed as we now use streams structure
                        removedIds.Add(variable2.ResultId);
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                    }
                    else
                    {
                        if (!analysisResult.Variables.TryGetValue(variable2.ResultId, out var variableInfo) || !variableInfo.UsedAnyStage)
                        {
                            removedIds.Add(variable2.ResultId);
                            SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                        }
                    }
                }
            }

            // Remove all OpTypeStreamsSDSL, OpTypePatchSDSL and OpTypeGeometryStreamOutputSDSL or any type that depends on it
            // (we do that before the OpName/OpDecorate pass)
            foreach (var i in context)
            {
                if (i.Op == Op.OpTypeStreamsSDSL || i.Op == Op.OpTypeGeometryStreamOutputSDSL || i.Op == Op.OpTypePatchSDSL || i.Op == Op.OpTypeFunctionSDSL || i.Op == Op.OpTypePointer || i.Op == Op.OpTypeArray)
                {
                    if (context.ReverseTypes.TryGetValue(i.Data.IdResult.Value, out var type))
                    {
                        var streamsTypeSearch = new StreamsTypeSearch();
                        streamsTypeSearch.VisitType(type);
                        if (streamsTypeSearch.Found)
                        {
                            removedIds.Add(i.Data.IdResult.Value);
                            SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                        }
                    }
                }
            }

            // Remove OpName/OpDecorate
            context.RemoveNameAndDecorations(removedIds);
        }

        private void MergeSameSemanticVariables(SymbolTable table, SpirvContext context, NewSpirvBuffer buffer, AnalysisResult analysisResult)
        {
            Dictionary<int, int> remapIds = new();
            foreach (var streamWithSameSemantic in analysisResult.Streams.Where(x => x.Value.Semantic != null).GroupBy(x => x.Value.Semantic))
            {
                // Make sure they all have the same type
                var firstStream = streamWithSameSemantic.First();
                foreach (var stream in streamWithSameSemantic.Skip(1))
                {
                    if (stream.Value.Type != firstStream.Value.Type)
                        throw new InvalidOperationException($"Two variables with same semantic {stream.Value.Semantic} have different types {stream.Value.Type} and {firstStream.Value.Type}");

                    // Remap variable
                    remapIds.Add(stream.Key, firstStream.Key);
                }
            }

            // Remove duplicate streams
            HashSet<int> removedIds = new();
            foreach (var i in buffer)
            {
                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is { } variable && remapIds.ContainsKey(variable.ResultId))
                {
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(variable.ResultId);
                }
            }
            
            // Remove OpName/OpDecorate
            context.RemoveNameAndDecorations(removedIds);

            foreach (var remapId in remapIds)
                analysisResult.Streams.Remove(remapId.Key);

            SpirvBuilder.RemapIds(buffer, 0, buffer.Count, remapIds);
        }
        
        static int FindOutputPatchSize(SpirvContext context, Symbol entryPoint)
        {
            foreach (var i in context)
            {
                if (i.Op == Op.OpExecutionMode && (OpExecutionMode)i is
                    {
                        EntryPoint: var target,
                        Mode: ExecutionMode.OutputVertices,
                        ModeParameters: { } m,
                    } && target == entryPoint.IdRef)
                {
                    return m.Span[0];
                }
            }

            throw new InvalidOperationException($"outputcontrolpoints not found on hull shader {entryPoint.Id.Name}");
        }

        private static void PropagateStreamsFromPreviousStage(Dictionary<int, StreamVariableInfo> streams)
        {
            foreach (var stream in streams)
            {
                stream.Value.OutputLayoutLocation = stream.Value.InputLayoutLocation;
                stream.Value.InputLayoutLocation = null;
                stream.Value.Output = stream.Value.Input;
                stream.Value.Read = false;
                stream.Value.Write = false;
            }
        }

        private AnalysisResult Analyze(NewSpirvBuffer buffer, SpirvContext context)
        {
            var streams = new Dictionary<int, StreamVariableInfo>();

            HashSet<int> blockTypes = [];
            Dictionary<int, int> blockPointerTypes = [];
            Dictionary<int, CBufferInfo> cbuffers = [];
            Dictionary<int, ResourceInfo> resources = [];
            Dictionary<int, VariableInfo> variables = [];

            // Build name table
            Dictionary<int, string> nameTable = [];
            Dictionary<int, string> semanticTable = [];
            HashSet<int> patchVariables = [];
            foreach (var i in context)
            {
                // Names
                {
                    if (i.Op == Op.OpName
                        && ((OpName)i) is
                        {
                            Target: int t,
                            Name: string n
                        }
                        )
                    {
                        nameTable[t] = new(n);
                    }
                    else if (i.Op == Op.OpMemberName
                        && ((OpMemberName)i) is
                        {
                            Type: int t2,
                            Member: int m,
                            Name: string n2
                        }
                        )
                    {
                        nameTable[t2] = new(n2);
                    }
                }

                // Semantic
                {
                    if (i.Op == Op.OpDecorateString
                        && ((OpDecorateString)i) is
                        {
                            Target: int t,
                            Decoration: Decoration.UserSemantic,
                            Value: string m
                        }
                        )
                    {
                        semanticTable[t] = m;
                    }
                }
                
                // Patch
                if (i.Op == Op.OpDecorate && (OpDecorate)i is { Target: int t3, Decoration: Decoration.Patch })
                {
                    patchVariables.Add(t3);
                }
            }

            // Analyze streams
            foreach (var i in buffer)
            {
                if (i.Op == Op.OpVariableSDSL
                    && ((OpVariableSDSL)i) is { Storageclass: Specification.StorageClass.Uniform, ResultType: var pointerType2, ResultId: var bufferId }
                    && context.ReverseTypes[pointerType2] is PointerType { BaseType: ConstantBufferSymbol })
                {
                    var name = nameTable[bufferId];
                    // Note: cbuffer names might be suffixed with .0 .1 (as in Shader.RenameCBufferVariables)
                    // Adjust for it
                    cbuffers.Add(bufferId, new(name));
                }

                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                    {
                        Storageclass: Specification.StorageClass.Private or Specification.StorageClass.Workgroup,
                        ResultId: int
                    } variable)
                {
                    var name = nameTable.TryGetValue(variable.ResultId, out var nameId)
                        ? nameId
                        : $"unnamed_{variable.ResultId}";
                    var type = (PointerType)context.ReverseTypes[variable.ResultType];

                    if (variable.Flags.HasFlag(VariableFlagsMask.Stream))
                    {
                        semanticTable.TryGetValue(variable.ResultId, out var semantic);

                        if (variable.MethodInitializer != null)
                            throw new NotImplementedException("Variable initializer is not supported on streams variable");

                        streams.Add(variable.ResultId, new StreamVariableInfo(semantic, name, type, variable.ResultId) { Patch = patchVariables.Contains(variable.ResultId) });
                    }
                    else
                    {
                        variables.Add(variable.ResultId, new VariableInfo(name, type, variable.ResultId)
                        {
                            VariableMethodInitializerId = variable.MethodInitializer,
                        });
                    }
                }

                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                    {
                        Storageclass: Specification.StorageClass.UniformConstant or Specification.StorageClass.StorageBuffer,
                        ResultId: int
                    } resource)
                {
                    var name = nameTable.TryGetValue(resource.ResultId, out var nameId)
                        ? nameId
                        : $"unnamed_{resource.ResultId}";
                    var type = context.ReverseTypes[resource.ResultType];

                    resources.Add(resource.ResultId, new ResourceInfo(name));
                }
            }

            // Process ResourceGroupId and build ResourceGroups
            Dictionary<int, ResourceGroup> resourceGroups = new();
            foreach (var i in context)
            {
                if (i.Op == Op.OpDecorate && (OpDecorate)i is { Decoration: Decoration.ResourceGroupIdSDSL, DecorationParameters: { } m } resourceGroupIdDecorate)
                {
                    var n = m.To<DecorationParams.ResourceGroupIdSDSL>();

                    if (resources.TryGetValue(resourceGroupIdDecorate.Target, out var resourceInfo))
                    {
                        if (!resourceGroups.TryGetValue(n.ResourceGroup, out var resourceGroup))
                            resourceGroups.Add(n.ResourceGroup, resourceGroup = new());

                        resourceGroup.Resources.Add(resourceInfo);

                        resourceInfo.ResourceGroup = resourceGroup;

                    }
                }
            }

            // Process ResourceGroup and LogicalGroup decorations
            foreach (var i in context)
            {
                if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: Decoration.ResourceGroupSDSL, Value: string m2  } resourceGroupDecorate)
                {
                    if (resources.TryGetValue(resourceGroupDecorate.Target, out var resourceInfo)
                        // Note: ResourceGroup should not be null if set
                        && resourceInfo.ResourceGroup.Name == null)
                    {
                        resourceInfo.ResourceGroup.Name = m2;
                    }
                }
                else if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: Decoration.LogicalGroupSDSL, Value: string m3 } logicalGroupDecorate)
                {
                    if (resources.TryGetValue(logicalGroupDecorate.Target, out var resourceInfo)
                        // Note: ResourceGroup should not be null if this decoration is set
                        && resourceInfo.ResourceGroup.LogicalGroup == null)
                    {
                        resourceInfo.ResourceGroup.LogicalGroup = m3;
                    }
                    else if (cbuffers.TryGetValue(logicalGroupDecorate.Target, out var cbufferInfo))
                    {
                        cbufferInfo.LogicalGroup = m3;
                    }
                }
            }

            return new(nameTable, streams, variables, cbuffers, resourceGroups, resources);
        }

        private (int Id, string Name) GenerateStreamWrapper(SymbolTable table, NewSpirvBuffer buffer, SpirvContext context, ExecutionModel executionModel, Symbol entryPoint, AnalysisResult analysisResult, LiveAnalysis liveAnalysis, bool isFirstActiveShader)
        {
            var streams = analysisResult.Streams;

            var stage = executionModel switch
            {
                ExecutionModel.Vertex => "VS",
                ExecutionModel.TessellationControl => "HS",
                ExecutionModel.TessellationEvaluation => "DS",
                ExecutionModel.Geometry => "GS",
                ExecutionModel.Fragment => "PS",
                ExecutionModel.GLCompute => "CS",
                _ => throw new NotImplementedException()
            };
            List<(StreamVariableInfo Info, int InterfaceId, SymbolType InterfaceType)> inputStreams = [];
            List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> outputStreams = [];
            List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> patchInputStreams = [];
            List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> patchOutputStreams = [];
            List<int> entryPointExtraVariables = [];

            int inputLayoutLocationCount = 0;
            int outputLayoutLocationCount = 0;

            foreach (var stream in streams)
            {
                if (stream.Value.Output)
                {
                    if (stream.Value.OutputLayoutLocation is { } outputLayoutLocation)
                    {
                        outputLayoutLocationCount = Math.Max(outputLayoutLocation + 1, outputLayoutLocationCount);
                    }
                }
            }

            bool AddBuiltin(int variable, BuiltIn builtin)
            {
                context.Add(new OpDecorate(variable, Decoration.BuiltIn, [(int)builtin]));
                return true;
            }
            
            bool AddLocation(int variable, string location)
            {
                // If it fails, default is 0
                int.TryParse(location, out var targetIndex);
                context.Add(new OpDecorate(variable, Decoration.Location, [targetIndex]));
                return true;
            }

            // Handle some conversions for builtins where type is not flexible
            // need to handle array size adjust, and vector size adjust as per ProcessBuiltinsDecoration() symbolType processing
            int ConvertInterfaceVariable(SymbolType sourceType, SymbolType castType, int value)
            {
                if (sourceType == castType)
                    return value;

                if (sourceType is VectorType v1 && castType is VectorType v2 && v1.BaseType == v2.BaseType)
                {
                    Span<int> components = stackalloc int[v2.Size];
                    for (int i = 0; i < v2.Size; ++i)
                    {
                        components[i] = i < v1.Size
                            ? buffer.Add(new OpCompositeExtract(context.GetOrRegister(v1.BaseType), context.Bound++, value, [i])).ResultId
                            : context.CreateDefaultConstantComposite(v1.BaseType).Id;
                    }

                    return buffer.Add(new OpCompositeConstruct(context.GetOrRegister(v2), context.Bound++, new(components))).ResultId;
                }
                
                if (sourceType is ArrayType a1 && castType is ArrayType a2 && a1.BaseType == a2.BaseType)
                {
                    Span<int> components = stackalloc int[a2.Size];
                    for (int i = 0; i < a2.Size; ++i)
                    {
                        components[i] = i < a1.Size
                            ? buffer.Add(new OpCompositeExtract(context.GetOrRegister(a1.BaseType), context.Bound++, value, [i])).ResultId
                            : context.CreateDefaultConstantComposite(a1.BaseType).Id;
                    }

                    return buffer.Add(new OpCompositeConstruct(context.GetOrRegister(a2), context.Bound++, new(components))).ResultId;
                }
                
                throw new InvalidOperationException($"Can't convert interface variable from {sourceType} to {castType}");
            }
            
            bool ProcessBuiltinsDecoration(int variable, StreamVariableType type, string? semantic, ref SymbolType symbolType)
            {
                semantic = semantic?.ToUpperInvariant();
                symbolType = (executionModel, type, semantic) switch
                {
                    // DX might use float[2] or float[3] or float[4] but Vulkan expects float[4] in all cases
                    (ExecutionModel.TessellationControl, StreamVariableType.Output, "SV_TESSFACTOR") => new ArrayType(ScalarType.Float, 4),
                    // DX might use float or float[2] but Vulkan expects float[2] in all cases
                    (ExecutionModel.TessellationControl, StreamVariableType.Output, "SV_INSIDETESSFACTOR") => new ArrayType(ScalarType.Float, 2),
                    // DX might use float2 or float3 but Vulkan expects float3 in all cases
                    (ExecutionModel.TessellationControl, StreamVariableType.Output, "SV_DOMAINLOCATION") => new VectorType(ScalarType.Float, 3),
                    _ => symbolType,
                };
                
                // Note: false means it needs to be forwarded
                // TODO: review the case where we don't use automatic forwarding for HS/DS/GS stages, i.e. SV_POSITION and SV_PrimitiveID
                return (executionModel, type, semantic) switch
                {
                    // SV_Depth/SV_Target
                    (ExecutionModel.Fragment, StreamVariableType.Output, "SV_DEPTH") => AddBuiltin(variable, BuiltIn.FragDepth),
                    (ExecutionModel.Fragment, StreamVariableType.Output, {} semantic2) when semantic2.StartsWith("SV_TARGET") => AddLocation(variable, semantic2.Substring("SV_TARGET".Length)),
                    // SV_Position
                    (not ExecutionModel.Fragment, StreamVariableType.Output, "SV_POSITION") => AddBuiltin(variable, BuiltIn.Position),
                    (not ExecutionModel.Fragment and not ExecutionModel.Vertex, StreamVariableType.Input, "SV_POSITION") => AddBuiltin(variable, BuiltIn.Position),
                    (ExecutionModel.Fragment, StreamVariableType.Input, "SV_POSITION") => AddBuiltin(variable, BuiltIn.FragCoord),
                    // SV_InstanceID/SV_VertexID
                    (ExecutionModel.Vertex, StreamVariableType.Input, "SV_INSTANCEID") => AddBuiltin(variable, BuiltIn.InstanceIndex),
                    (ExecutionModel.Vertex, StreamVariableType.Input, "SV_VERTEXID") => AddBuiltin(variable, BuiltIn.VertexIndex),
                    (not ExecutionModel.Vertex, StreamVariableType.Input, "SV_INSTANCEID" or "SV_VERTEXID") => false,
                    // SV_IsFrontFace
                    (ExecutionModel.Fragment, StreamVariableType.Input, "SV_ISFRONTFACE") => AddBuiltin(variable, BuiltIn.FrontFacing),
                    // SV_PrimitiveID
                    (ExecutionModel.Geometry, StreamVariableType.Output, "SV_PRIMITIVEID") => AddBuiltin(variable, BuiltIn.PrimitiveId),
                    (not ExecutionModel.Vertex, StreamVariableType.Input, "SV_PRIMITIVEID") => AddBuiltin(variable, BuiltIn.PrimitiveId),
                    // Tessellation
                    (ExecutionModel.TessellationControl or ExecutionModel.TessellationEvaluation, _, "SV_TESSFACTOR") => AddBuiltin(variable, BuiltIn.TessLevelOuter),
                    (ExecutionModel.TessellationControl or ExecutionModel.TessellationEvaluation, _, "SV_INSIDETESSFACTOR") => AddBuiltin(variable, BuiltIn.TessLevelInner),
                    (ExecutionModel.TessellationEvaluation, StreamVariableType.Input, "SV_DOMAINLOCATION") => AddBuiltin(variable, BuiltIn.TessCoord),
                    (ExecutionModel.TessellationControl, StreamVariableType.Input, "SV_OUTPUTCONTROLPOINTID") => AddBuiltin(variable, BuiltIn.InvocationId),
                    // Compute shaders
                    (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_GROUPID") => AddBuiltin(variable, BuiltIn.WorkgroupId),
                    (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_GROUPINDEX") => AddBuiltin(variable, BuiltIn.LocalInvocationIndex),
                    (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_GROUPTHREADID") => AddBuiltin(variable, BuiltIn.LocalInvocationId),
                    (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_DISPATCHTHREADID") => AddBuiltin(variable, BuiltIn.GlobalInvocationId),
                    (_, _, {} semantic2) when semantic2.StartsWith("SV_") => throw new NotImplementedException($"System-value Semantic not implemented: {semantic2} for stage {executionModel} as {type}"),
                    _ => false,
                };
            }

            var entryPointFunctionType = (FunctionType)entryPoint.Type;
            // TODO: check all parameters instead of hardcoded 0
            int? arrayInputSize = executionModel switch
            {
                ExecutionModel.Geometry => ((ArrayType)((PointerType)entryPointFunctionType.ParameterTypes[0].Type).BaseType).Size,
                ExecutionModel.TessellationControl or ExecutionModel.TessellationEvaluation => ((PatchType)((PointerType)entryPointFunctionType.ParameterTypes[0].Type).BaseType).Size,
                _ => null,
            };
            int? arrayOutputSize = executionModel switch
            {
                ExecutionModel.TessellationControl => FindOutputPatchSize(context, entryPoint),
                _ => null,
            };

            foreach (var stream in streams)
            {
                if (stream.Value.Input)
                {
                    var variableId = context.Bound++;
                    var variableType = stream.Value.Type;
                    if (!ProcessBuiltinsDecoration(variableId, StreamVariableType.Input, stream.Value.Semantic, ref variableType))
                    {
                        if (stream.Value.InputLayoutLocation == null)
                            stream.Value.InputLayoutLocation = inputLayoutLocationCount++;
                        context.Add(new OpDecorate(variableId, Decoration.Location, [stream.Value.InputLayoutLocation.Value]));
                        if (stream.Value.Semantic != null)
                            context.Add(new OpDecorateString(variableId, Decoration.UserSemantic, stream.Value.Semantic));
                    }

                    // Note: for geometry & tessellation shader, we process multiple inputs at once (in an array), except for patch constants
                    var streamInputType = new PointerType(!stream.Value.Patch && arrayInputSize != null
                            ? new ArrayType(variableType, arrayInputSize.Value)
                            : variableType,
                        Specification.StorageClass.Input);
                    var variable = context.Add(new OpVariable(context.GetOrRegister(streamInputType), variableId, Specification.StorageClass.Input, null));
                    context.AddName(variable, $"in_{stage}_{stream.Value.Name}");
                    
                    if (stream.Value.Type is ScalarType or VectorType or MatrixType && !stream.Value.Type.GetElementType().IsFloating())
                        context.Add(new OpDecorate(variable, Decoration.Flat, []));

                    stream.Value.InputId = variable.ResultId;
                    (stream.Value.Patch ? patchInputStreams : inputStreams).Add((stream.Value, variable.ResultId, variableType));
                }

                if (stream.Value.Output)
                {
                    var variableId = context.Bound++;
                    var variableType = stream.Value.Type;
                    if (!ProcessBuiltinsDecoration(variableId, StreamVariableType.Output, stream.Value.Semantic, ref variableType))
                    {
                        // TODO: this shouldn't be necessary if we allocated layout during first forward pass for any SV_ semantic
                        if (stream.Value.OutputLayoutLocation == null)
                        {
                            if (stream.Value.Semantic?.ToUpperInvariant().StartsWith("SV_") ?? false)
                                stream.Value.OutputLayoutLocation = outputLayoutLocationCount++;
                            else
                                throw new InvalidOperationException($"Can't find output layout location for variable [{stream.Value.Name}]");
                        }

                        context.Add(new OpDecorate(variableId, Decoration.Location, [stream.Value.OutputLayoutLocation.Value]));
                        if (stream.Value.Semantic != null)
                            context.Add(new OpDecorateString(variableId, Decoration.UserSemantic, stream.Value.Semantic));
                    }

                    // Note: for geometry & tessellation shader, we process multiple inputs at once (in an array), except for patch constants
                    var streamOutputType = new PointerType(!stream.Value.Patch && arrayOutputSize != null
                            ? new ArrayType(variableType, arrayOutputSize.Value)
                            : variableType,
                        Specification.StorageClass.Output);
                    var variable = context.Add(new OpVariable(context.GetOrRegister(streamOutputType), variableId, Specification.StorageClass.Output, null));
                    context.AddName(variable, $"out_{stage}_{stream.Value.Name}");
                    
                    if (stream.Value.Type is ScalarType or VectorType or MatrixType && !stream.Value.Type.GetElementType().IsFloating())
                        context.Add(new OpDecorate(variable, Decoration.Flat, []));

                    stream.Value.OutputId = variable.ResultId;
                    (stream.Value.Patch ? patchOutputStreams : outputStreams).Add((stream.Value, variable.ResultId, variableType));
                }
            }

            var streamFields = new List<StructuredTypeMember>();
            var constantFields = new List<StructuredTypeMember>();
            var inputFields = new List<StructuredTypeMember>();
            var outputFields = new List<StructuredTypeMember>();
            foreach (var stream in streams)
            {
                stream.Value.InputStructFieldIndex = null;
                stream.Value.OutputStructFieldIndex = null;
                if (stream.Value.UsedThisStage)
                {
                    var fields = (stream.Value.Patch) ? constantFields : streamFields;
                    stream.Value.StreamStructFieldIndex = fields.Count;
                    fields.Add(new(stream.Value.Name, stream.Value.Type, default));
                }
            }

            foreach (var stream in inputStreams)
            {
                stream.Info.InputStructFieldIndex = inputFields.Count;
                inputFields.Add(new(stream.Info.Name, stream.Info.Type, default));
            }

            foreach (var stream in outputStreams)
            {
                stream.Info.OutputStructFieldIndex = outputFields.Count;
                outputFields.Add(new(stream.Info.Name, stream.Info.Type, default));
            }

            var inputType = new StructType($"{stage}_INPUT", inputFields);
            var outputType = new StructType($"{stage}_OUTPUT", outputFields);
            var streamsType = new StructType($"{stage}_STREAMS", streamFields);
            bool hasConstants = executionModel is ExecutionModel.TessellationControl or ExecutionModel.TessellationEvaluation;
            var constantsType = hasConstants ? new StructType($"{stage}_CONSTANTS", constantFields) : null;
            context.DeclareStructuredType(inputType, context.Bound++);
            context.DeclareStructuredType(outputType, context.Bound++);
            context.DeclareStructuredType(streamsType, context.Bound++);
            if (hasConstants)
                context.DeclareStructuredType(constantsType, context.Bound++);

            // Create a static global streams variable
            var streamsVariable = context.Add(new OpVariable(context.GetOrRegister(new PointerType(streamsType, Specification.StorageClass.Private)), context.Bound++, Specification.StorageClass.Private, null));
            context.AddName(streamsVariable.ResultId, $"streams{stage}");
            
            // Find patch constant entry point
            var patchConstantEntryPoint = executionModel == ExecutionModel.TessellationControl ? ResolveHullPatchConstantEntryPoint(table, context, entryPoint) : null;

            // Patch any OpStreams/OpAccessChain to use the new struct
            foreach (var method in liveAnalysis.ReferencedMethods)
            {
                if (method.Value.UsedThisStage && method.Value.HasStreamAccess)
                {
                    DuplicateMethodIfNecessary(buffer, context, method.Key, analysisResult, liveAnalysis);
                    PatchStreamsAccesses(table, buffer, context, method.Key, streamsType, inputType, outputType, constantsType, streamsVariable.ResultId, analysisResult, liveAnalysis);
                }
            }

            var voidType = context.GetOrRegister(ScalarType.Void);

            // Add new entry point wrapper
            var newEntryPointFunctionType = context.GetOrRegister(new FunctionType(ScalarType.Void, []));
            var newEntryPointFunction = buffer.Add(new OpFunction(voidType, context.Bound++, FunctionControlMask.None, newEntryPointFunctionType));
            buffer.Add(new OpLabel(context.Bound++));
            var variableInsertIndex = buffer.Count;
            var entryPointName = $"{entryPoint.Id.Name}_Wrapper";
            context.AddName(newEntryPointFunction, entryPointName);

            {
                // Variable initializers
                foreach (var variable in analysisResult.Variables)
                {
                    // Note: we check UsedThisStage to make sure variable is actually used in the shader (otherwise it won't be emitted if not part of all used variables in OpEntryPoint)
                    if (variable.Value.UsedThisStage
                        && variable.Value.VariableMethodInitializerId is int methodInitializerId)
                    {
                        liveAnalysis.ExtraReferencedMethods.Add(methodInitializerId);

                        var variableValueType = variable.Value.Type.BaseType;
                        var methodInitializerCall = buffer.Add(new OpFunctionCall(context.GetOrRegister(variableValueType), context.Bound++, methodInitializerId, []));
                        buffer.Add(new OpStore(variable.Value.VariableId, methodInitializerCall.ResultId, null, []));
                    }
                }

                // Update entry point type (since Streams type might have been replaced)
                entryPointFunctionType = (FunctionType)entryPoint.Type;
                
                var builtinVariables = new Dictionary<string, (SymbolType Type, int Id)>();
                int GetOrDeclareBuiltInValue(SymbolType type, string semantic)
                {
                    semantic = semantic.ToUpperInvariant();
                    if (builtinVariables.TryGetValue(semantic, out var result))
                    {
                        if (result.Type != type)
                            throw new InvalidOperationException($"Semantic {semantic} requested with type {type} but last time with {result.Type}");
                        return result.Id;
                    }

                    // Declare the global builtin
                    var variableId = context.Bound++;
                    if (!ProcessBuiltinsDecoration(variableId, StreamVariableType.Input, semantic, ref type))
                        throw new InvalidOperationException();
                    var variable = context.Add(new OpVariable(context.GetOrRegister(new PointerType(type, Specification.StorageClass.Input)), variableId, Specification.StorageClass.Input, null)).ResultId;
                    entryPointExtraVariables.Add(variable);
                    var value = buffer.Add(new OpLoad(context.GetOrRegister(type), context.Bound++, variable, null, [])).ResultId;
                    builtinVariables.Add(semantic, (type, value));
                    return value;
                }
                void FillSemanticArguments(FunctionType functionType, Span<int> arguments)
                {
                    foreach (var i in context)
                    {
                        if (i.Op == Op.OpMemberDecorateString
                            && ((OpMemberDecorateString)i) is
                            {
                                StructType: int t,
                                Decoration: Decoration.UserSemantic,
                                Value: string semantic,
                                Member: int argumentIndex,
                            } && t == entryPoint.IdRef
                           )
                        {
                            var argumentType = ((PointerType)functionType.ParameterTypes[argumentIndex].Type).BaseType;
                            
                            var value = GetOrDeclareBuiltInValue(argumentType, semantic);

                            // Create local variable with StorageClass.Function that we can use as argument
                            var localVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(argumentType, Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
                            buffer.Add(new OpStore(localVariable, value, null, []));
                            arguments[argumentIndex] = localVariable;

                            SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                        }
                    }
                }
                
                // Fill parameters with semantics
                Span<int> arguments = stackalloc int[entryPointFunctionType.ParameterTypes.Count];
                FillSemanticArguments(entryPointFunctionType, arguments);

                // Setup input and call original main()
                if (arrayInputSize != null)
                {
                    // Copy variables to Input[X] which is first method parameter of main()
                    // Pattern is a loop over index i looking like:
                    //  inputs[i].Position = gl_Position[i];
                    //  inputs[i].Normal = in_GS_normals[i];
                    var inputsVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(new ArrayType(inputType, arrayInputSize.Value), Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
                    context.AddName(inputsVariable, "inputs");

                    int ConvertInputsArray()
                    {
                        Span<int> inputLoadValues = stackalloc int[inputFields.Count];
                        for (var inputIndex = 0; inputIndex < inputStreams.Count; inputIndex++)
                        {
                            var stream = inputStreams[inputIndex];
                            var loadedValue = buffer.Add(new OpLoad(context.GetOrRegister(new ArrayType(stream.Info.Type, arrayInputSize.Value)), context.Bound++, stream.InterfaceId, null, []));
                            inputLoadValues[inputIndex] = loadedValue.ResultId;
                        }
                    
                        Span<int> inputFieldValues = stackalloc int[inputFields.Count];
                        Span<int> inputValues = stackalloc int[arrayInputSize.Value];
                        for (int arrayIndex = 0; arrayIndex < arrayInputSize; ++arrayIndex)
                        {
                            for (var inputIndex = 0; inputIndex < inputStreams.Count; inputIndex++)
                            {
                                var stream = inputStreams[inputIndex];
                                inputFieldValues[inputIndex] = buffer.Add(new OpCompositeExtract(context.Types[stream.Info.Type], context.Bound++, inputLoadValues[inputIndex], [arrayIndex])).ResultId;
                                inputFieldValues[inputIndex] = ConvertInterfaceVariable(stream.InterfaceType, stream.Info.Type, inputFieldValues[inputIndex]);
                            }
                        
                            inputValues[arrayIndex] = buffer.Add(new OpCompositeConstruct(context.GetOrRegister(inputType), context.Bound++, [..inputFieldValues])).ResultId;
                        }
                    
                        var inputsData1 = buffer.Add(new OpCompositeConstruct(context.GetOrRegister(new ArrayType(inputType, arrayInputSize.Value)), context.Bound++, [..inputValues])).ResultId;
                        return inputsData1;
                    }

                    var inputsData = ConvertInputsArray();

                    buffer.Add(new OpStore(inputsVariable, inputsData, null, []));

                    var entryPointTypeId = context.GetOrRegister(entryPoint.Type);
                    if (executionModel == ExecutionModel.TessellationControl || executionModel == ExecutionModel.TessellationEvaluation)
                    {
                        bool hullTessellationOutputsGenerated = false;
                        int GenerateHullTessellationOutputs()
                        {
                            if (hullTessellationOutputsGenerated)
                                throw new InvalidOperationException("Hull OutputPatch can only be used in once place (constant patch)");
                            hullTessellationOutputsGenerated = true;
                            var outputsVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(new ArrayType(outputType, arrayInputSize.Value), Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
                            context.AddName(outputsVariable, "outputs");

                            for (int arrayIndex = 0; arrayIndex < arrayInputSize; ++arrayIndex)
                            {
                                for (var outputIndex = 0; outputIndex < outputStreams.Count; outputIndex++)
                                {
                                    var stream = outputStreams[outputIndex];
                                    var outputsVariablePtr = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Function)),
                                        context.Bound++, outputsVariable,
                                        [context.CompileConstant(arrayIndex).Id, context.CompileConstant(outputIndex).Id])).ResultId;
                                    var outputSourcePtr = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Output)),
                                        context.Bound++, stream.Id,
                                        [context.CompileConstant(arrayIndex).Id])).ResultId;
                                    var outputsSourceValue = buffer.Add(new OpLoad(context.GetOrRegister(stream.Info.Type), context.Bound++, outputSourcePtr, null, [])).ResultId;
                                    outputsSourceValue = ConvertInterfaceVariable(stream.Info.Type, stream.InterfaceType, outputsSourceValue);
                                    buffer.Add(new OpStore(outputsVariablePtr, outputsSourceValue, null, []));
                                }
                            }

                            return outputsVariable;
                        }

                        void FillTessellationArguments(Symbol function, Span<int> arguments)
                        {
                            var functionType = (FunctionType)function.Type;
                            var functionTypeId = context.GetOrRegister(functionType);
                            for (int i = 0; i < functionType.ParameterTypes.Count; i++)
                            {
                                var parameterType = ((PointerType)functionType.ParameterTypes[i].Type).BaseType;
                                var parameterModifiers = functionType.ParameterTypes[i].Modifiers;
                                switch (parameterType)
                                {
                                    // Hull/Domain inputs
                                    case PatchType inputPatchType when
                                        (inputPatchType.Kind == PatchTypeKindSDSL.Input && executionModel == ExecutionModel.TessellationControl)
                                        || (inputPatchType.Kind == PatchTypeKindSDSL.Output && executionModel == ExecutionModel.TessellationEvaluation):
                                    {
                                        // Change signature of main() to use an array instead of InputPatch
                                        // InputPatch<HS_INPUT, X> becomes HS_INPUT[X]
                                        SpirvBuilder.FunctionReplaceArgument(context, buffer, function, i, new PointerType(new ArrayType(inputPatchType.BaseType, inputPatchType.Size), Specification.StorageClass.Function));
                                        context.ReplaceType(function.Type, functionTypeId);
                                        arguments[i] = inputsVariable;
                                        break;
                                    }
                                    // Hull outputs
                                    case PatchType { Kind: PatchTypeKindSDSL.Output } outputPatchType when executionModel == ExecutionModel.TessellationControl:
                                    {
                                        // Change signature of main() to use an array instead of InputPatch
                                        // InputPatch<HS_INPUT, X> becomes HS_INPUT[X]
                                        SpirvBuilder.FunctionReplaceArgument(context, buffer, function, i, new PointerType(new ArrayType(outputPatchType.BaseType, outputPatchType.Size), Specification.StorageClass.Function));
                                        context.ReplaceType(function.Type, functionTypeId);
                                        arguments[i] = GenerateHullTessellationOutputs();
                                        break;
                                    }
                                    case StructType t when (t == constantsType) && parameterModifiers is ParameterModifiers.None or ParameterModifiers.In:
                                    {
                                        // Parameter is "HS_CONSTANTS constants"
                                        var constantVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(t, Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
                                        arguments[i] = constantVariable;
                                        // Copy back values from semantic/builtin variables to Constants struct
                                        foreach (var stream in patchInputStreams)
                                        {
                                            var inputPtr = buffer.Add(new OpAccessChain(context.GetOrRegister(stream.Info.Type), context.Bound++, constantVariable, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                                            var inputResult = buffer.Add(new OpLoad(context.GetOrRegister(stream.Info.Type), stream.Id, constantVariable, null, [])).ResultId;
                                            inputResult = ConvertInterfaceVariable(stream.InterfaceType, stream.Info.Type, inputResult);
                                            buffer.Add(new OpStore(inputPtr, inputResult, null, []));
                                        }
                                        break;
                                    }
                                    case StructType t when (t == outputType || t == constantsType) && parameterModifiers == ParameterModifiers.Out:
                                    {
                                        // Parameter is "out HS_OUTPUT output" or "out HS_CONSTANTS constants"
                                        var outVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(t, Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
                                        arguments[i] = outVariable;
                                        break;
                                    }
                                    case var t when arguments[i] == 0:
                                        throw new NotImplementedException($"Can't process argument {i + 1} of type {parameterType} in method {entryPoint.Id.Name}");
                                }
                            }
                        }

                        void ProcessTessellationArguments(Symbol function, Span<int> arguments)
                        {
                            var functionType = (FunctionType)function.Type;
                            for (int i = 0; i < functionType.ParameterTypes.Count; i++)
                            {
                                var parameterType = ((PointerType)functionType.ParameterTypes[i].Type).BaseType;
                                var parameterModifiers = functionType.ParameterTypes[i].Modifiers;
                                switch (parameterType)
                                {
                                    case StructType t when t == outputType && parameterModifiers == ParameterModifiers.Out:
                                    {
                                        // Parameter is "out HS_OUTPUT output"
                                        var outputVariable = arguments[i];
                                        // Load as value
                                        outputVariable = buffer.Add(new OpLoad(context.GetOrRegister(t), context.Bound++, outputVariable, null, [])).ResultId;
                                        // Do we need to index into array? if yes, get index (gl_invocationID)
                                        int? invocationIdValue = arrayOutputSize != null ? GetOrDeclareBuiltInValue(ScalarType.UInt, "SV_OutputControlPointID") : null;
                                        // Copy back values from Output struct to semantic/builtin variables
                                        for (var outputIndex = 0; outputIndex < outputStreams.Count; outputIndex++)
                                        {
                                            var stream = outputStreams[outputIndex];
                                            var outputResult = buffer.Add(new OpCompositeExtract(context.GetOrRegister(stream.Info.Type), context.Bound++, outputVariable, [outputIndex])).ResultId;
                                            outputResult = ConvertInterfaceVariable(stream.Info.Type, stream.InterfaceType, outputResult);
                                            var outputTargetPtr = arrayOutputSize != null
                                                ? buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Output)),
                                                    context.Bound++, stream.Id,
                                                    [invocationIdValue.Value])).ResultId
                                                : stream.Id;
                                            buffer.Add(new OpStore(outputTargetPtr, outputResult, null, []));
                                        }
                                        break;
                                    }
                                    case StructType t when t == constantsType && parameterModifiers == ParameterModifiers.Out:
                                    {
                                        // Parameter is "out HS_OUTPUT output"
                                        var outputVariable = arguments[i];
                                        // Load as value
                                        outputVariable = buffer.Add(new OpLoad(context.GetOrRegister(t), context.Bound++, outputVariable, null, [])).ResultId;
                                        // Copy back values from Output struct to semantic/builtin variables
                                        foreach (var stream in patchOutputStreams)
                                        {
                                            var outputResult = buffer.Add(new OpCompositeExtract(context.GetOrRegister(stream.Info.Type), context.Bound++, outputVariable, [stream.Info.StreamStructFieldIndex])).ResultId;
                                            outputResult = ConvertInterfaceVariable(stream.Info.Type, stream.InterfaceType, outputResult);
                                            buffer.Add(new OpStore(stream.Id, outputResult, null, []));
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        
                        FillTessellationArguments(entryPoint, arguments);

                        // Call main(inputs, output, ...)
                        buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPoint.IdRef, new(arguments)));

                        ProcessTessellationArguments(entryPoint, arguments);

                        if (patchConstantEntryPoint != null)
                        {
                            // Insert a barrier
                            buffer.Add(new OpControlBarrier(context.CompileConstant(2).Id, context.CompileConstant(4).Id, context.CompileConstant(0).Id));
                            
                            liveAnalysis.MarkMethodUsed(patchConstantEntryPoint.IdRef);

                            // Load gl_InvocationID to check if we're invocation 0
                            var invocationIdValue = GetOrDeclareBuiltInValue(ScalarType.UInt, "SV_OutputControlPointID");

                            // Compare with 0
                            var zeroConstant = context.CompileConstant(0u).Id;
                            var isInvocationZero = buffer.Add(new OpIEqual(context.GetOrRegister(ScalarType.Boolean), context.Bound++, invocationIdValue, zeroConstant)).ResultId;

                            // Create labels for if-then-merge
                            var thenLabel = context.Bound++;
                            var mergeLabel = context.Bound++;

                            // Branch based on condition
                            buffer.Add(new OpSelectionMerge(mergeLabel, SelectionControlMask.None));
                            buffer.Add(new OpBranchConditional(isInvocationZero, thenLabel, mergeLabel, []));

                            // Then block: call patch constant function
                            buffer.Add(new OpLabel(thenLabel));

                            var patchConstantEntryPointType = (FunctionType)patchConstantEntryPoint.Type;
                            Span<int> patchArguments = stackalloc int[patchConstantEntryPointType.ParameterTypes.Count];
                            FillSemanticArguments(patchConstantEntryPointType, patchArguments);
                            FillTessellationArguments(patchConstantEntryPoint, patchArguments);
                            buffer.Add(new OpFunctionCall(voidType, context.Bound++, patchConstantEntryPoint.IdRef, new(patchArguments)));
                            ProcessTessellationArguments(patchConstantEntryPoint, patchArguments);
                            
                            buffer.Add(new OpBranch(mergeLabel));

                            // Merge block
                            buffer.Add(new OpLabel(mergeLabel));
                        }
                    }
                    else if (executionModel == ExecutionModel.Geometry)
                    {
                        // Change signature of main() to not use the output Stream anymore
                        SpirvBuilder.FunctionRemoveArgument(context, buffer, entryPoint, 1);
                        
                        // Extract and remove execution mode (line, point, triangleadj, etc.)
                        var executionMode = entryPointFunctionType.ParameterTypes[0].Modifiers;
                        if (executionMode == ParameterModifiers.None)
                            throw new InvalidOperationException("Execution mode primitive is missing for first parameter of geometry shader");
                        entryPointFunctionType.ParameterTypes[0] = entryPointFunctionType.ParameterTypes[0] with { Modifiers = ParameterModifiers.None };
                        
                        context.ReplaceType(entryPoint.Type, entryPointTypeId);
                        context.Add(new OpExecutionMode(entryPoint.IdRef, executionMode switch
                        {
                            ParameterModifiers.Point => ExecutionMode.InputPoints,
                            ParameterModifiers.Line => ExecutionMode.InputLines,
                            ParameterModifiers.LineAdjacency => ExecutionMode.InputLinesAdjacency,
                            ParameterModifiers.Triangle => ExecutionMode.Triangles,
                            ParameterModifiers.TriangleAdjacency => ExecutionMode.InputTrianglesAdjacency,
                        }, []));

                        arguments[0] = inputsVariable;
                        
                        // Call main(inputs)
                        buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPoint.IdRef, new(arguments)));
                    }
                }
                else
                {
                    // We assume a void returning function and Input/Output is all handled with streams
                    // Note: we could in the future support having Input/Output in the function signature, just like we do for HS/DS/GS
                    
                    // Copy variables from input to streams struct
                    foreach (var stream in inputStreams)
                    {
                        var streamPointer = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Private)), context.Bound++, streamsVariable.ResultId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                        var inputResult = buffer.Add(new OpLoad(context.Types[stream.Info.Type], context.Bound++, stream.InterfaceId, null, [])).ResultId;
                        inputResult = ConvertInterfaceVariable(stream.InterfaceType, stream.Info.Type, inputResult);
                        buffer.Add(new OpStore(streamPointer, inputResult, null, []));
                    }
                    
                    // Call main()
                    buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPoint.IdRef, new(arguments)));
                    
                    // Copy variables from streams struct to output
                    foreach (var stream in outputStreams)
                    {
                        var baseType = stream.Info.Type;
                        var streamPointer = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Private)), context.Bound++, streamsVariable.ResultId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                        var outputResult = buffer.Add(new OpLoad(context.Types[baseType], context.Bound++, streamPointer, null, [])).ResultId;
                        outputResult = ConvertInterfaceVariable(stream.Info.Type, stream.InterfaceType, outputResult);
                        buffer.Add(new OpStore(stream.Id, outputResult, null, []));
                    }
                }

                buffer.Add(new OpReturn());
                buffer.Add(new OpFunctionEnd());

                // Note: we overallocate and filter with UsedThisStage after
                Span<int> entryPointInterfaceVariables = stackalloc int[inputStreams.Count + outputStreams.Count + patchInputStreams.Count + patchOutputStreams.Count + 1 + analysisResult.Variables.Count + analysisResult.CBuffers.Count + analysisResult.Resources.Count + entryPointExtraVariables.Count];
                int pvariableIndex = 0;
                foreach (var inputStream in inputStreams)
                    entryPointInterfaceVariables[pvariableIndex++] = inputStream.InterfaceId;
                foreach (var outputStream in outputStreams)
                    entryPointInterfaceVariables[pvariableIndex++] = outputStream.Id;
                foreach (var inputStream in patchInputStreams)
                    entryPointInterfaceVariables[pvariableIndex++] = inputStream.Id;
                foreach (var outputStream in patchOutputStreams)
                    entryPointInterfaceVariables[pvariableIndex++] = outputStream.Id;
                entryPointInterfaceVariables[pvariableIndex++] = streamsVariable.ResultId;
                foreach (var variable in analysisResult.Variables)
                {
                    if (variable.Value.UsedThisStage)
                        entryPointInterfaceVariables[pvariableIndex++] = variable.Key;
                }
                foreach (var cbuffer in analysisResult.CBuffers)
                {
                    if (cbuffer.Value.UsedThisStage)
                        entryPointInterfaceVariables[pvariableIndex++] = cbuffer.Key;
                }
                foreach (var resource in analysisResult.Resources)
                {
                    if (resource.Value.UsedThisStage)
                        entryPointInterfaceVariables[pvariableIndex++] = resource.Key;
                }

                foreach (var variable in entryPointExtraVariables)
                {
                    entryPointInterfaceVariables[pvariableIndex++] = variable;
                }

                liveAnalysis.ExtraReferencedMethods.Add(newEntryPointFunction);
                context.Add(new OpEntryPoint(executionModel, newEntryPointFunction, entryPointName, [.. entryPointInterfaceVariables.Slice(0, pvariableIndex)]));
            }
            
            // Move OpExecutionMode on new wrapper
            foreach (var i in context)
            {
                if (i.Op == Op.OpExecutionMode && (OpExecutionMode)i is { } executionMode)
                {
                    if (executionMode.EntryPoint == entryPoint.IdRef)
                        executionMode.EntryPoint = newEntryPointFunction.ResultId;
                }
            }

            return (newEntryPointFunction.ResultId, entryPointName);
        }

        private Symbol? ResolveHullPatchConstantEntryPoint(SymbolTable table, SpirvContext context, Symbol entryPoint)
        {
            // Check if there's a patch constant function and call it when gl_InvocationID == 0
            string? patchConstantFuncName = null;
            foreach (var i in context)
            {
                if (i.Op == Op.OpDecorateString && (OpDecorateString)i is
                    {
                        Target: int target,
                        Decoration: Decoration.PatchConstantFuncSDSL,
                        Value: string funcName
                    } && target == entryPoint.IdRef)
                {
                    patchConstantFuncName = funcName;
                    break;
                }
            }

            Symbol? patchConstantEntryPoint = null;
            if (patchConstantFuncName != null)
            {
                // Resolve the patch constant function
                patchConstantEntryPoint = ResolveEntryPoint(table, patchConstantFuncName);
                if (patchConstantEntryPoint == null)
                    throw new InvalidOperationException($"Hull shader patch constant function {patchConstantFuncName} was not found");
            }

            return patchConstantEntryPoint;
        }

        void DuplicateMethodIfNecessary(NewSpirvBuffer buffer, SpirvContext context, int functionId, AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
        {
            var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);

            (var methodStart, var methodEnd) = SpirvBuilder.FindMethodBounds(buffer, functionId);

            // One function might need to be duplicated in case it is used by different shader stages with STREAMS:
            // On first time (in a stage), we backup method original content before mutation
            // On second time (in a different stage), we copy the method (from original content)
            if (methodInfo.OriginalMethodCode == null)
            {
                // Copy instructions memory (since we're going to mutate them and want to retain original version)
                var methodInstructions = buffer.Slice(methodStart, methodEnd - methodStart);
                foreach (ref var i in methodInstructions.AsSpan())
                    i = new OpData(i.Memory.Span);

                methodInfo.OriginalMethodCode = methodInstructions;
            }
            else
            {
                // Need to reinsert method with new IDs
                var remapIds = new Dictionary<int, int>();
                var copiedInstructions = new List<OpData>();
                foreach (var i in methodInfo.OriginalMethodCode)
                {
                    // Save copied function ID
                    if (i.Op == Op.OpFunction)
                        methodInfo.ThisStageMethodId = context.Bound;

                    var i2 = new OpData(i.Memory.Span);
                    if (i2.IdResult.HasValue)
                        remapIds.Add(i2.IdResult.Value, context.Bound++);
                    SpirvBuilder.RemapIds(remapIds, ref i2);
                    copiedInstructions.Add(i2);

                    // Copy names too
                    if (i.IdResult is int resultId)
                    {
                        if (analysisResult.Names.TryGetValue(resultId, out var name))
                            context.AddName(i2.IdResult!.Value, name);
                    }
                }

                if (methodInfo.ThisStageMethodId == null)
                    throw new InvalidOperationException();

                liveAnalysis.ExtraReferencedMethods.Add(methodInfo.ThisStageMethodId.Value);

                // TODO: adjust mixin instructions ranges
                buffer.InsertRange(methodEnd, copiedInstructions.AsSpan());
                CodeInserted?.Invoke(methodEnd, copiedInstructions.Count);
            }
        }

        class StreamsTypeReplace(SymbolType streamsReplacement, SymbolType inputReplacement, SymbolType outputReplacement, SymbolType? constantsReplacement) : TypeRewriter
        {
            public override SymbolType Visit(StreamsType streamsType)
            {
                return streamsType.Kind switch
                {
                    StreamsKindSDSL.Streams => streamsReplacement,
                    StreamsKindSDSL.Input => inputReplacement,
                    StreamsKindSDSL.Output => outputReplacement,
                    StreamsKindSDSL.Constants => constantsReplacement ?? throw new InvalidOperationException(),
                };
            }
        }

        class StreamsTypeSearch : TypeWalker
        {
            public bool Found { get; private set; }
            public override void Visit(StreamsType streamsType)
            {
                Found = true;
            }
            public override void Visit(GeometryStreamType geometryStreamsType)
            {
                Found = true;
            }

            public override void Visit(PatchType patchType)
            {
                Found = true;
            }
        }

        void PatchStreamsAccesses(SymbolTable table, NewSpirvBuffer buffer, SpirvContext context, int functionId, StructType streamsStructType, StructType inputStructType, StructType outputStructType, StructType? constantsStructType, int streamsVariableId, AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
        {
            var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);

            (var methodStart, _) = SpirvBuilder.FindMethodBounds(buffer, methodInfo.ThisStageMethodId ?? functionId);

            var streams = analysisResult.Streams;
            // true => implicit (streams.), false => specific variable
            var streamsInstructionIds = new Dictionary<int, bool>();

            var method = (OpFunction)buffer[methodStart];
            var methodType = (FunctionType)context.ReverseTypes[method.FunctionType];

            var streamTypeReplacer = new StreamsTypeReplace(streamsStructType, inputStructType, outputStructType, constantsStructType);
            var newMethodType = (FunctionType)streamTypeReplacer.Visit(methodType);
            if (!ReferenceEquals(newMethodType, methodType))
            {
                methodType = newMethodType;
                method.FunctionType = context.GetOrRegister(methodType);
                var symbol = table.ResolveSymbol(functionId);
                symbol.Type = methodType;
            }
            
            // Remap ids for streams type to actual struct type
            var remapIds = new Dictionary<int, int>();
            var processedIds = new HashSet<int>();

            // Check if type contains any Streams/Input/Output (and if yes, register the replacement)
            void CheckStreamTypes(int id)
            {
                if (processedIds.Add(id) && context.ReverseTypes.TryGetValue(id, out var type))
                {
                    // New type, check it
                    var replacedType = streamTypeReplacer.VisitType(type);
                    if (!ReferenceEquals(replacedType, type))
                        remapIds.Add(id, context.GetOrRegister(replacedType));
                }
            }

            // TODO: remap method type!
            Span<int> tempIdsForStreamCopy = stackalloc int[streams.Values.Count];
            for (int index = methodStart; ; ++index)
            {
                var i = buffer[index];

                if (i.Op == Op.OpFunctionEnd)
                    break;
                
                if (i.Op == Op.OpStreamsSDSL && (OpStreamsSDSL)i is { } streamsInstruction)
                {
                    streamsInstructionIds.Add(streamsInstruction.ResultId, true);
                    remapIds.Add(streamsInstruction.ResultId, streamsVariableId);
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
                else if (i.Op is Op.OpVariable && (OpVariable)i is { } variable)
                {
                    var type = context.ReverseTypes[variable.ResultType];
                    if (type is PointerType { BaseType: StreamsType })
                        streamsInstructionIds.Add(variable.ResultId, false);
                }
                else if (i.Op is Op.OpFunctionParameter && (OpFunctionParameter)i is { } functionParameter)
                {
                    var type = context.ReverseTypes[functionParameter.ResultType];
                    if (type is PointerType { BaseType: StreamsType })
                        streamsInstructionIds.Add(functionParameter.ResultId, false);
                }
                else if (i.Op == Op.OpAccessChain && (OpAccessChain)i is { } accessChain)
                {
                    // In case it's a streams access, patch acces to use STREAMS struct with proper index
                    if (streamsInstructionIds.TryGetValue(accessChain.BaseId, out var isImplicit))
                    {
                        var streamVariableId = accessChain.Values.Elements.Span[0];
                        var streamInfo = streams[streamVariableId];
                        var streamStructMemberIndex = streamInfo.StreamStructFieldIndex;

                        // TODO: this won't update accessChain.Memory yet but setting accessChain.Base later will fix that
                        //       we'll need a better way to update LiteralArray and propagate changes
                        accessChain.Values.Elements.Span[0] = context.CompileConstant(streamStructMemberIndex).Id;

                        if (isImplicit)
                            accessChain.BaseId = streamsVariableId;
                        else
                            // Force refresh of InstructionMemory
                            // TODO: remove when accessChain.Values update properly the instruction
                            accessChain.BaseId = accessChain.BaseId; 
                    }
                }
                else if (i.Op == Op.OpCopyLogical && (OpCopyLogical)i is { } copyLogical)
                {
                    // Cast input to streams
                    var targetType = context.ReverseTypes[copyLogical.ResultType];
                    if (targetType is StreamsType { Kind: StreamsKindSDSL.Streams })
                    {
                        foreach (var stream in streams)
                        {
                            // Part of streams?
                            if (!stream.Value.Patch && stream.Value.UsedThisStage)
                            {
                                if (stream.Value.Input)
                                {
                                    // Extract value from streams
                                    tempIdsForStreamCopy[stream.Value.StreamStructFieldIndex] = buffer.Insert(index++,
                                        new OpCompositeExtract(context.GetOrRegister(stream.Value.Type),
                                            context.Bound++,
                                            copyLogical.Operand,
                                            [stream.Value.InputStructFieldIndex.Value])).ResultId;
                                }
                                else
                                {
                                    // Otherwise use default value
                                    tempIdsForStreamCopy[stream.Value.StreamStructFieldIndex] = context.CreateDefaultConstantComposite(stream.Value.Type).Id;
                                }
                            }
                        }
                        
                        // Update index (otherwise copyLogical fields will point to invalid data)
                        i.Index = index;
                        buffer.Replace(index, new OpCompositeConstruct(copyLogical.ResultType, copyLogical.ResultId, [..tempIdsForStreamCopy.Slice(0, streamsStructType.Members.Count)]));
                    }
                    else if (targetType is StreamsType { Kind: StreamsKindSDSL.Output })
                    {
                        foreach (var stream in streams)
                        {
                            // Part of streams?
                            if (!stream.Value.Patch && stream.Value.Output)
                            {
                                // Extract value from streams
                                tempIdsForStreamCopy[stream.Value.OutputStructFieldIndex.Value] = buffer.Insert(index++,
                                    new OpCompositeExtract(context.GetOrRegister(stream.Value.Type),
                                        context.Bound++,
                                        copyLogical.Operand,
                                        [stream.Value.StreamStructFieldIndex])).ResultId;
                            }
                        }
                        
                        // Update index (otherwise copyLogical fields will point to invalid data)
                        i.Index = index;
                        buffer.Replace(index, new OpCompositeConstruct(copyLogical.ResultType, copyLogical.ResultId, [..tempIdsForStreamCopy.Slice(0, outputStructType.Members.Count)]));
                    }
                }
                else if (i.Op == Op.OpEmitVertexSDSL && (OpEmitVertexSDSL)i is { } emitVertex)
                {
                    var output = emitVertex.Output;
                    foreach (var stream in streams)
                    {
                        if (stream.Value.Output)
                        {
                            var outputValue = buffer.Insert(index++, new OpCompositeExtract(context.GetOrRegister(stream.Value.Type), context.Bound++, output, [stream.Value.OutputStructFieldIndex.Value])).ResultId;
                            buffer.Insert(index++, new OpStore(stream.Value.OutputId.Value, outputValue, MemoryAccessMask.None, []));
                        }
                    }

                    buffer.Replace(index, new OpEmitVertex());
                }
                else if (i.Op == Op.OpFunctionCall && (OpFunctionCall)i is { } call)
                {
                    var calledMethodInfo = liveAnalysis.ReferencedMethods[call.Function];
                    // In case we copied the method, use the new ID
                    if (calledMethodInfo.ThisStageMethodId is int updatedMethodId)
                        call.Function = updatedMethodId;
                }

                SpirvBuilder.CollectIds(i.Data, CheckStreamTypes);
                
                SpirvBuilder.RemapIds(remapIds, ref i.Data);
            }
        }

        /// <summary>
        /// Figure out (recursively) which streams are being read from and written to.
        /// </summary>
        private bool AnalyzeStreamReadWrites(NewSpirvBuffer buffer, SpirvContext context, int functionId, AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
        {
            // Check if already processed
            var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);
            if (methodInfo.UsedThisStage)
            {
                return methodInfo.HasStreamAccess;
            }

            // Mark as used
            methodInfo.UsedThisStage = true;

            // If method was mutated by another stage, we work on the original copy instead
            List<OpData> methodInstructions;
            if (methodInfo.OriginalMethodCode != null)
            {
                methodInstructions = methodInfo.OriginalMethodCode;
            }
            else
            {
                (var methodStart, var methodEnd) = SpirvBuilder.FindMethodBounds(buffer, functionId);
                methodInstructions = buffer.Slice(methodStart, methodEnd - methodStart);
            }

            var streamsInstructionIds = new HashSet<int>();
            var streams = analysisResult.Streams;
            var variables = analysisResult.Variables;
            var accessChainBases = new Dictionary<int, int>();

            foreach (ref var i in methodInstructions.AsSpan())
            {
                // Check for any Streams variable
                if (i.Op is Op.OpFunction && new OpFunction(ref i) is { } function)
                {
                    var functionType = (FunctionType)context.ReverseTypes[function.FunctionType];
                    var streamsTypeSearch = new StreamsTypeSearch();
                    streamsTypeSearch.Visit(functionType);
                    if (streamsTypeSearch.Found)
                        methodInfo.HasStreamAccess = true;
                }
                else if (i.Op is Op.OpVariable && new OpVariable(ref i) is { } variable)
                {
                    var type = context.ReverseTypes[variable.ResultType];
                    if (type is PointerType { BaseType: StreamsType })
                    {
                        // Note: we should restrict to R except if inout variable
                        streamsInstructionIds.Add(variable.ResultId);
                        methodInfo.HasStreamAccess = true;
                    }
                }
                // and for any Streams parameter
                else if (i.Op is Op.OpFunctionParameter && new OpFunctionParameter(ref i) is { } functionParameter)
                {
                    var type = context.ReverseTypes[functionParameter.ResultType];
                    if (type is PointerType { BaseType: StreamsType })
                    {
                        // Note: we should restrict to R except if inout variable
                        streamsInstructionIds.Add(functionParameter.ResultId);
                        methodInfo.HasStreamAccess = true;
                    }
                }
                else if (i.Op is Op.OpLoad && new OpLoad(ref i) is { } load)
                {
                    // Check for indirect access chains
                    if (!accessChainBases.TryGetValue(load.Pointer, out var pointer))
                        pointer = load.Pointer;

                    if (streams.TryGetValue(pointer, out var streamInfo) && !streamInfo.Write)
                        streamInfo.Read = true;
                    if (variables.TryGetValue(pointer, out var variableInfo))
                        variableInfo.UsedThisStage = true;
                    if (analysisResult.Resources.TryGetValue(pointer, out var resourceInfo))
                        resourceInfo.UsedThisStage = true;
                    if (analysisResult.CBuffers.TryGetValue(pointer, out var cbufferInfo))
                        cbufferInfo.UsedThisStage = true;
                }
                else if (i.Op is Op.OpStore && new OpStore(ref i) is { } store)
                {
                    // Check for indirect access chains
                    if (!accessChainBases.TryGetValue(store.Pointer, out var pointer))
                        pointer = store.Pointer;

                    if (streams.TryGetValue(pointer, out var streamInfo))
                        streamInfo.Write = true;
                    if (variables.TryGetValue(pointer, out var variableInfo))
                        variableInfo.UsedThisStage = true;
                    if (analysisResult.Resources.TryGetValue(pointer, out var resourceInfo))
                        resourceInfo.UsedThisStage = true;
                    if (analysisResult.CBuffers.TryGetValue(pointer, out var cbufferInfo))
                        cbufferInfo.UsedThisStage = true;
                }
                else if (i.Op == Op.OpStreamsSDSL && new OpStreamsSDSL(ref i) is { } streamsInstruction)
                {
                    streamsInstructionIds.Add(streamsInstruction.ResultId);
                    methodInfo.HasStreamAccess = true;
                }
                else if (i.Op == Op.OpAccessChain && new OpAccessChain(ref i) is { } accessChain)
                {
                    var currentBase = accessChain.BaseId;

                    // In case it's a streams access, mark the stream as being the base
                    if (streamsInstructionIds.Contains(currentBase))
                    {
                        var streamVariableId = accessChain.Values.Elements.Span[0];
                        var streamInfo = streams[streamVariableId];

                        // Set this base for OpStore/OpLoad stream R/W analysis
                        currentBase = streamVariableId;
                    }

                    // Any read or write through an access chain will be treated as doing it on the main variable.
                    // i.e., streams.A.B will share same streamInfo as streams.A
                    // TODO: what happens in case of partial write?
                    // Recurse in case we have multiple access chain chained after each other
                    while (accessChainBases.TryGetValue(currentBase, out var nextBase))
                        currentBase = nextBase;
                    accessChainBases.Add(accessChain.ResultId, currentBase);
                }
                else if (i.Op == Op.OpFunctionCall && new OpFunctionCall(ref i) is { } call)
                {
                    // Process call
                    methodInfo.HasStreamAccess |= AnalyzeStreamReadWrites(buffer, context, call.Function, analysisResult, liveAnalysis);
                }
            }

            return methodInfo.HasStreamAccess;
        }

        private static readonly Regex MatchSemanticName = new Regex(@"([A-Za-z_]+)(\d*)");
        private static (string Name, int Index) ParseSemantic(string semantic)
        {
            var match = MatchSemanticName.Match(semantic);
            if (!match.Success)
                return (semantic, 0);

            string baseName = match.Groups[1].Value;
            int value = 0;
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                value = int.Parse(match.Groups[2].Value);
            }

            return (baseName, value);
        }
    }
}
