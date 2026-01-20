using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System.IO;
using Stride.Shaders.Parsing.Analysis;
using static Stride.Shaders.Spirv.Specification;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;

namespace Stride.Shaders.Spirv.Processing
{
    /// <summary>
    /// Help to process streams and simplify the interface (resources, methods, cbuffer) of the shader.
    /// </summary>
    public class InterfaceProcessor
    {
        public delegate void CodeInsertedDelegate(int index, int count);

        public CodeInsertedDelegate CodeInserted { get; set; }
        
        enum StreamVariableType
        {
            Input,
            Output,
        }
        
        class StreamInfo(string? semantic, string name, SymbolType type, int variableId)
        {
            public string? Semantic { get; } = semantic;
            public string Name { get; } = name;
            public SymbolType Type { get; } = type;
            public int VariableId { get; } = variableId;

            public int? InputLayoutLocation { get; set; }
            public int? OutputLayoutLocation { get; set; }

            /// <summary>
            /// We automatically mark input: a variable read before it's written to, or an output without a write.
            /// </summary>
            public bool Input => Read || (Output && !Write);
            public bool Output { get => field; set { field = value; UsedAnyStage = true; } }
            public bool UsedThisStage => Input || Output || Read || Write;

            public bool Read { get => field; set { field = value; UsedAnyStage = true; } }
            public bool Write { get => field; set { field = value; UsedAnyStage = true; } }
            public bool UsedAnyStage { get; private set; }
            public int StreamStructFieldIndex { get; internal set; }

            public override string ToString() => $"{Type} {Name} {(Read ? "R" : "")} {(Write ? "W" : "")}";
        }

        class VariableInfo(string name, SymbolType type, int variableId)
        {
            public string Name { get; } = name;
            public SymbolType Type { get; } = type;

            public int VariableId { get; } = variableId;
            public int? VariableMethodInitializerId { get; set; }


            /// <summary>
            /// Used during current stage being processed?
            /// </summary>
            public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
            /// <summary>
            /// Used at all (in any stage)
            /// </summary>
            public bool UsedAnyStage { get; private set; }
        }

        class ResourceInfo(string name)
        {
            public string Name { get; } = name;

            public ResourceGroup ResourceGroup { get; set; }

            /// <summary>
            /// Used during current stage being processed?
            /// </summary>
            public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
            /// <summary>
            /// Used at all (in any stage)
            /// </summary>
            public bool UsedAnyStage { get; private set; }
        }

        record class ResourceGroup
        {
            public bool Used { get; set; }
            public string Name { get; set; }
            public string? LogicalGroup { get; set; }
            public List<ResourceInfo> Resources { get; } = new();
        }

        record class CBufferInfo(string name)
        {
            public string Name { get; } = name;

            public string? LogicalGroup { get; set; }

            /// <summary>
            /// Used during current stage being processed?
            /// </summary>
            public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
            /// <summary>
            /// Used at all (in any stage)
            /// </summary>
            public bool UsedAnyStage { get; private set; }
        }

        class LogicalGroupInfo
        {
            public List<ResourceGroup> Resources { get; } = new();
            public List<CBufferInfo> CBuffers { get; } = new();
        }

        record struct AnalysisResult(Dictionary<int, string> Names, Dictionary<int, StreamInfo> Streams, Dictionary<int, VariableInfo> Variables, Dictionary<int, CBufferInfo> CBuffers, Dictionary<int, ResourceGroup> ResourceGroups, Dictionary<int, ResourceInfo> Resources);

        class MethodInfo
        {
            /// <summary>
            /// Used during current stage being processed?
            /// </summary>
            public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
            /// <summary>
            /// Used at all (in any stage)
            /// </summary>
            public bool UsedAnyStage { get; private set; }

            public List<OpData>? OriginalMethodCode { get; set; }
            public int? ThisStageMethodId { get; set; }

            // True if the method depends on STREAMS type (also if used by any OpFunctionCall recursively)
            public bool HasStreamAccess { get; internal set; }
        }

        class LiveAnalysis
        {
            public Dictionary<int, MethodInfo> ReferencedMethods { get; } = new();

            public HashSet<int> ExtraReferencedMethods { get; } = new();

            public MethodInfo GetOrCreateMethodInfo(int functionId)
            {
                if (!ReferencedMethods.TryGetValue(functionId, out MethodInfo methodInfo))
                    ReferencedMethods.Add(functionId, methodInfo = new MethodInfo());

                return methodInfo;
            }

            public bool MarkMethodUsed(int functionId)
            {
                var methodInfo = GetOrCreateMethodInfo(functionId);

                var previousValue = methodInfo.UsedThisStage;
                methodInfo.UsedThisStage = true;
                // Returns tree when added first time
                return !previousValue;
            }
        }

        public void Process(SymbolTable table, NewSpirvBuffer buffer, SpirvContext context)
        {
            table.TryResolveSymbol("VSMain", out var entryPointVS);
            table.TryResolveSymbol("PSMain", out var entryPointPS);
            table.TryResolveSymbol("CSMain", out var entryPointCS);

            if (entryPointCS.Type is FunctionGroupType)
                entryPointCS = entryPointCS.GroupMembers[^1];
            if (entryPointVS.Type is FunctionGroupType)
                entryPointVS = entryPointVS.GroupMembers[^1];
            if (entryPointPS.Type is FunctionGroupType)
                entryPointPS = entryPointPS.GroupMembers[^1];

            if (entryPointPS.IdRef == 0 && entryPointCS.IdRef == 0)
                throw new InvalidOperationException($"{nameof(InterfaceProcessor)}: At least a pixel or compute shader is expected");
            if (entryPointPS.IdRef != 0 && entryPointCS.IdRef != 0)
                throw new InvalidOperationException($"{nameof(InterfaceProcessor)}: Found both a pixel and a compute shader");

            var entryPointPSOrCS = entryPointCS.IdRef != 0 ? entryPointCS : entryPointPS;

            var analysisResult = Analyze(buffer, context);
            MergeSameSemanticVariables(table, context, buffer, analysisResult);
            var streams = analysisResult.Streams;

            var liveAnalysis = new LiveAnalysis();
            AnalyzeStreamReadWrites(buffer, context, entryPointPSOrCS.IdRef, analysisResult, liveAnalysis);

            if (entryPointCS.IdRef != 0)
            {
                var csWrapperId = GenerateStreamWrapper(buffer, context, ExecutionModel.GLCompute, entryPointCS.IdRef, entryPointCS.Id.Name, analysisResult, liveAnalysis, false);

                // Move OpExecutionMode on new CSMain wrapper (and remove others)
                foreach (var i in context)
                {
                    if (i.Op == Op.OpExecutionMode && (OpExecutionMode)i is { } executionMode)
                    {
                        if (executionMode.EntryPoint == entryPointCS.IdRef)
                            executionMode.EntryPoint = csWrapperId;
                        else
                            SpirvBuilder.SetOpNop(executionMode.OpData.Memory.Span);
                    }
                }
            }
            if (entryPointPS.IdRef != 0)
            {
                // If written to, they are expected at the end of pixel shader
                foreach (var stream in streams)
                {
                    if (stream.Value.Semantic is { } semantic && (semantic.ToUpperInvariant().StartsWith("SV_TARGET") || semantic.ToUpperInvariant() == "SV_DEPTH")
                                                              && stream.Value.Write)
                        stream.Value.Output = true;
                }

                // Check if there is any output
                // (if PSMain has been overriden with an empty method, it means we don't want to output anything and remove the pixel shader, i.e. for shadow caster)
                if (streams.Any(x => x.Value.Output))
                {
                    var psWrapperId = GenerateStreamWrapper(buffer, context, ExecutionModel.Fragment, entryPointPS.IdRef, entryPointPS.Id.Name, analysisResult, liveAnalysis, false);
                    buffer.FluentAdd(new OpExecutionMode(psWrapperId, ExecutionMode.OriginUpperLeft, []));
                }

                // Those semantic variables are implicit in pixel shader, no need to forward them from previous stages
                foreach (var stream in streams)
                {
                    if (stream.Value.Semantic is { } semantic && (semantic.ToUpperInvariant() == "SV_COVERAGE" || semantic.ToUpperInvariant() == "SV_ISFRONTFACE" || semantic.ToUpperInvariant() == "VFACE"))
                        stream.Value.Read = false;
                }

                // Reset cbuffer/resource/methods used for next stage
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

                PropagateStreamsFromPreviousStage(streams);
                if (entryPointVS.IdRef != 0)
                {
                    AnalyzeStreamReadWrites(buffer, context, entryPointVS.IdRef, analysisResult, liveAnalysis);

                    // If written to, they are expected at the end of vertex shader
                    foreach (var stream in streams)
                    {
                        if (stream.Value.Semantic is { } semantic && (semantic.ToUpperInvariant().StartsWith("SV_POSITION"))
                                                                  && stream.Value.Write)
                            stream.Value.Output = true;
                    }

                    GenerateStreamWrapper(buffer, context, ExecutionModel.Vertex, entryPointVS.IdRef, entryPointVS.Id.Name, analysisResult, liveAnalysis, true);
                }
            }

            // This will remove a lot of unused methods, resources and variables
            // (while following proper rules to preserve rgroup, cbuffer, logical groups, etc.)
            RemoveUnreferencedCode(buffer, context, analysisResult, streams, liveAnalysis);
        }

        private static void RemoveUnreferencedCode(NewSpirvBuffer buffer, SpirvContext context, AnalysisResult analysisResult, Dictionary<int, StreamInfo> streams, LiveAnalysis liveAnalysis)
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
                        Storageclass: StorageClass.Uniform,
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
                        Storageclass: StorageClass.UniformConstant,
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
                        Storageclass: StorageClass.Private,
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

            foreach (var i in context)
            {
                if (i.Op == Op.OpName && ((OpName)i) is { } name)
                {
                    if (removedIds.Contains(name.Target))
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
                else if (i.Op == Op.OpDecorate && ((OpDecorate)i) is { } decorate)
                {
                    if (removedIds.Contains(decorate.Target))
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
                else if (i.Op == Op.OpDecorateString && ((OpDecorateString)i) is { } decorateString)
                {
                    if (removedIds.Contains(decorateString.Target))
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
                else if (i.Op == Op.OpTypeStreamsSDSL || i.Op == Op.OpTypeFunctionSDSL || i.Op == Op.OpTypePointer)
                {
                    if (context.ReverseTypes.TryGetValue(i.Data.IdResult.Value, out var type))
                    {
                        var streamsTypeSearch = new StreamsTypeSearch();
                        streamsTypeSearch.VisitType(type);
                        if (streamsTypeSearch.Found)
                            SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                    }
                }
            }
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
            foreach (var i in buffer)
            {
                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is { } variable && remapIds.ContainsKey(variable.ResultId))
                {
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
            }

            foreach (var remapId in remapIds)
                analysisResult.Streams.Remove(remapId.Key);

            SpirvBuilder.RemapIds(buffer, 0, buffer.Count, remapIds);
        }

        private static void PropagateStreamsFromPreviousStage(Dictionary<int, StreamInfo> streams)
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
            var streams = new Dictionary<int, StreamInfo>();

            HashSet<int> blockTypes = [];
            Dictionary<int, int> blockPointerTypes = [];
            Dictionary<int, CBufferInfo> cbuffers = [];
            Dictionary<int, ResourceInfo> resources = [];
            Dictionary<int, VariableInfo> variables = [];

            // Build name table
            Dictionary<int, string> nameTable = [];
            Dictionary<int, string> semanticTable = [];
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
            }

            // Analyze streams
            foreach (var i in buffer)
            {
                if (i.Op == Op.OpVariableSDSL
                    && ((OpVariableSDSL)i) is { Storageclass: StorageClass.Uniform, ResultType: var pointerType2, ResultId: var bufferId }
                    && context.ReverseTypes[pointerType2] is PointerType { BaseType: ConstantBufferSymbol })
                {
                    var name = nameTable[bufferId];
                    // Note: cbuffer names might be suffixed with .0 .1 (as in Shader.RenameCBufferVariables)
                    // Adjust for it
                    cbuffers.Add(bufferId, new(name));
                }

                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                    {
                        Storageclass: StorageClass.Private,
                        ResultId: int
                    } variable)
                {
                    var name = nameTable.TryGetValue(variable.ResultId, out var nameId)
                        ? nameId
                        : $"unnamed_{variable.ResultId}";
                    var type = context.ReverseTypes[variable.ResultType];

                    if (variable.Flags.HasFlag(VariableFlagsMask.Stream))
                    {
                        semanticTable.TryGetValue(variable.ResultId, out var semantic);

                        if (variable.MethodInitializer != null)
                            throw new NotImplementedException("Variable initializer is not supported on streams variable");

                        streams.Add(variable.ResultId, new StreamInfo(semantic, name, type, variable.ResultId));
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
                        Storageclass: StorageClass.UniformConstant or StorageClass.StorageBuffer,
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

        private int GenerateStreamWrapper(NewSpirvBuffer buffer, SpirvContext context, ExecutionModel executionModel, int entryPointId, string entryPointName, AnalysisResult analysisResult, LiveAnalysis liveAnalysis, bool isFirstActiveShader)
        {
            var streams = analysisResult.Streams;

            var stage = executionModel switch
            {
                ExecutionModel.Fragment => "PS",
                ExecutionModel.Vertex => "VS",
                ExecutionModel.GLCompute => "CS",
                _ => throw new NotImplementedException()
            };
            List<(StreamInfo Info, int Id)> inputStreams = [];
            List<(StreamInfo Info, int Id)> outputStreams = [];
            List<StreamInfo> privateStreams = [];

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
            
            bool ProcessBuiltinsDecoration(int variable, StreamVariableType type, StreamInfo stream)
            {
                switch (stream.Semantic?.ToUpperInvariant())
                {
                    case "SV_DEPTH":
                        if (executionModel is ExecutionModel.Fragment && type == StreamVariableType.Output)
                            return AddBuiltin(variable, BuiltIn.FragDepth);
                        return false;
                    case {} semantic when semantic.StartsWith("SV_TARGET"):
                        if (executionModel is ExecutionModel.Fragment && type == StreamVariableType.Output)
                        {
                            // If it fails, default is 0
                            int.TryParse(semantic.Substring("SV_TARGET".Length), out var targetIndex);
                            context.Add(new OpDecorate(variable, Decoration.Location, [targetIndex]));
                            return true;
                        }
                        return false;
                    case "SV_POSITION":
                        if (isFirstActiveShader && type == StreamVariableType.Output)
                            return AddBuiltin(variable, BuiltIn.Position);
                        if (executionModel == ExecutionModel.Fragment && type == StreamVariableType.Input)
                            return AddBuiltin(variable, BuiltIn.FragCoord);
                        return false;
                    // TODO: Check if first stage
                    case "SV_INSTANCEID":
                        if (type == StreamVariableType.Input)
                            return AddBuiltin(variable, BuiltIn.InstanceIndex);
                        return false;
                    case "SV_VERTEXID":
                        if (executionModel is ExecutionModel.Vertex && type == StreamVariableType.Input)
                            return AddBuiltin(variable, BuiltIn.VertexIndex);
                        return false;
                    case "SV_ISFRONTFACE":
                        if ((executionModel is ExecutionModel.Fragment && type == StreamVariableType.Input)
                            || (executionModel is ExecutionModel.Geometry && type == StreamVariableType.Output))
                            return AddBuiltin(variable, BuiltIn.FrontFacing);
                        throw new NotImplementedException($"Invalid use of System-value semantic {stream.Semantic} as {type} in stage {executionModel}");
                    case {} semantic when semantic.StartsWith("SV_"):
                        throw new NotImplementedException($"System-value Semantic not implemented: {semantic}");
                    default:
                        return false;
                }
            }

            foreach (var stream in streams)
            {
                var baseType = ((PointerType)stream.Value.Type).BaseType;

                if (stream.Value.UsedThisStage)
                    privateStreams.Add(stream.Value);

                if (stream.Value.Input)
                {
                    context.FluentAdd(new OpTypePointer(context.Bound++, StorageClass.Input, context.Types[baseType]), out var pointerType);
                    context.FluentAdd(new OpVariable(pointerType, context.Bound++, StorageClass.Input, null), out var variable);
                    context.AddName(variable, $"in_{stage}_{stream.Value.Name}");

                    if (!ProcessBuiltinsDecoration(variable.ResultId, StreamVariableType.Input, stream.Value))
                    {
                        if (stream.Value.InputLayoutLocation == null)
                            stream.Value.InputLayoutLocation = inputLayoutLocationCount++;
                        context.Add(new OpDecorate(variable, Decoration.Location, [stream.Value.InputLayoutLocation.Value]));
                        if (stream.Value.Semantic != null)
                            context.Add(new OpDecorateString(variable, Decoration.UserSemantic, stream.Value.Semantic));
                    }
                    
                    if (!baseType.GetElementType().IsFloating())
                        context.Add(new OpDecorate(variable, Decoration.Flat, []));

                    inputStreams.Add((stream.Value, variable.ResultId));
                }

                if (stream.Value.Output)
                {
                    context.FluentAdd(new OpTypePointer(context.Bound++, StorageClass.Output, context.Types[baseType]), out var pointerType);
                    context.FluentAdd(new OpVariable(pointerType, context.Bound++, StorageClass.Output, null), out var variable);
                    context.AddName(variable, $"out_{stage}_{stream.Value.Name}");

                    if (!ProcessBuiltinsDecoration(variable.ResultId, StreamVariableType.Output, stream.Value))
                    {
                        // TODO: this shouldn't be necessary if we allocated layout during first forward pass for any SV_ semantic
                        if (stream.Value.OutputLayoutLocation == null)
                        {
                            if (stream.Value.Semantic?.ToUpperInvariant().StartsWith("SV_") ?? false)
                                stream.Value.OutputLayoutLocation = outputLayoutLocationCount++;
                            else
                                throw new InvalidOperationException($"Can't find output layout location for variable [{stream.Value.Name}]");
                        }

                        context.Add(new OpDecorate(variable, Decoration.Location, [stream.Value.OutputLayoutLocation.Value]));
                        if (stream.Value.Semantic != null)
                            context.Add(new OpDecorateString(variable, Decoration.UserSemantic, stream.Value.Semantic));
                    }
                    
                    if (!baseType.GetElementType().IsFloating())
                        context.Add(new OpDecorate(variable, Decoration.Flat, []));

                    outputStreams.Add((stream.Value, variable.ResultId));
                }
            }

            var fields = new List<StructuredTypeMember>();
            foreach (var stream in privateStreams)
            {
                stream.StreamStructFieldIndex = fields.Count;
                fields.Add(new(stream.Name, stream.Type, default));
            }
            var streamsType = new StructType($"{stage}_STREAMS", fields);
            context.DeclareStructuredType(streamsType);

            // Create a static global streams variable
            context.FluentAdd(new OpVariable(context.GetOrRegister(new PointerType(streamsType, StorageClass.Private)), context.Bound++, StorageClass.Private, null), out var streamsVariable);
            context.AddName(streamsVariable.ResultId, $"streams{stage}");

            // Patch any OpStreams/OpAccessChain to use the new struct
            foreach (var method in liveAnalysis.ReferencedMethods)
            {
                if (method.Value.UsedThisStage && method.Value.HasStreamAccess)
                {
                    DuplicateMethodIfNecessary(buffer, context, method.Key, analysisResult, liveAnalysis);
                    PatchStreamsAccesses(buffer, context, method.Key, streamsType, streamsVariable.ResultId, analysisResult, liveAnalysis);
                }
            }

            var voidType = context.GetOrRegister(ScalarType.Void);

            // Add new entry point wrapper
            context.FluentAdd(new OpTypeFunctionSDSL(context.Bound++, voidType, []), out var newEntryPointFunctionType);
            buffer.FluentAdd(new OpFunction(voidType, context.Bound++, FunctionControlMask.None, newEntryPointFunctionType), out var newEntryPointFunction);
            buffer.Add(new OpLabel(context.Bound++));
            context.AddName(newEntryPointFunction, $"{entryPointName}_Wrapper");

            {
                // Variable initializers
                foreach (var variable in analysisResult.Variables)
                {
                    // Note: we check Private to make sure variable is actually used in the shader (otherwise it won't be emitted if not part of all used variables in OpEntryPoint)
                    if (variable.Value.UsedThisStage
                        && variable.Value.VariableMethodInitializerId is int methodInitializerId)
                    {
                        liveAnalysis.ExtraReferencedMethods.Add(methodInitializerId);

                        var variableValueType = ((PointerType)variable.Value.Type).BaseType;
                        buffer.FluentAdd(new OpFunctionCall(context.GetOrRegister(variableValueType), context.Bound++, methodInitializerId, []), out var methodInitializerCall);
                        buffer.Add(new OpStore(variable.Value.VariableId, methodInitializerCall.ResultId, null, []));
                    }
                }

                // Copy variables from input to streams struct
                foreach (var stream in inputStreams)
                {
                    var baseType = ((PointerType)stream.Info.Type).BaseType;
                    buffer.FluentAdd(new OpAccessChain(context.Types[stream.Info.Type], context.Bound++, streamsVariable.ResultId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id]), out var streamPointer);
                    buffer.FluentAdd(new OpLoad(context.Types[baseType], context.Bound++, stream.Id, null, []), out var loadedValue);
                    buffer.Add(new OpStore(streamPointer.ResultId, loadedValue.ResultId, null, []));
                }

                buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPointId, []));

                // Copy variables from streams struct to output
                foreach (var stream in outputStreams)
                {
                    var baseType = ((PointerType)stream.Info.Type).BaseType;
                    buffer.FluentAdd(new OpAccessChain(context.Types[stream.Info.Type], context.Bound++, streamsVariable.ResultId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id]), out var streamPointer);
                    buffer.FluentAdd(new OpLoad(context.Types[baseType], context.Bound++, streamPointer.ResultId, null, []), out var loadedValue);
                    buffer.Add(new OpStore(stream.Id, loadedValue.ResultId, null, []));
                }


                buffer.Add(new OpReturn());
                buffer.Add(new OpFunctionEnd());

                // Note: we overallocate and filter with UsedThisStage after
                Span<int> pvariables = stackalloc int[inputStreams.Count + outputStreams.Count + 1 + analysisResult.Variables.Count + analysisResult.CBuffers.Count + analysisResult.Resources.Count];
                int pvariableIndex = 0;
                foreach (var inputStream in inputStreams)
                    pvariables[pvariableIndex++] = inputStream.Id;
                foreach (var outputStream in outputStreams)
                    pvariables[pvariableIndex++] = outputStream.Id;
                pvariables[pvariableIndex++] = streamsVariable.ResultId;
                foreach (var variable in analysisResult.Variables)
                {
                    if (variable.Value.UsedThisStage)
                        pvariables[pvariableIndex++] = variable.Key;
                }
                foreach (var cbuffer in analysisResult.CBuffers)
                {
                    if (cbuffer.Value.UsedThisStage)
                        pvariables[pvariableIndex++] = cbuffer.Key;
                }
                foreach (var resource in analysisResult.Resources)
                {
                    if (resource.Value.UsedThisStage)
                        pvariables[pvariableIndex++] = resource.Key;
                }

                liveAnalysis.ExtraReferencedMethods.Add(newEntryPointFunction);
                context.Add(new OpEntryPoint(executionModel, newEntryPointFunction, $"{entryPointName}_Wrapper", [.. pvariables.Slice(0, pvariableIndex)]));
            }

            return newEntryPointFunction.ResultId;
        }

        void DuplicateMethodIfNecessary(NewSpirvBuffer buffer, SpirvContext context, int functionId, AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
        {
            var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);

            (var methodStart, var methodEnd) = FindMethodBounds(buffer, functionId);

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

        class StreamsTypeReplace(SymbolType streamsReplacement) : TypeRewriter
        {
            public override SymbolType Visit(StreamsType streamsType)
            {
                return streamsReplacement;
            }
        }

        class StreamsTypeSearch : TypeWalker
        {
            public bool Found { get; private set; }
            public override void Visit(StreamsType streamsType)
            {
                Found = true;
            }
        }

        void PatchStreamsAccesses(NewSpirvBuffer buffer, SpirvContext context, int functionId, StructType streamsStructType, int streamsVariableId, AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
        {
            var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);

            (var methodStart, var methodEnd) = FindMethodBounds(buffer, methodInfo.ThisStageMethodId ?? functionId);

            var streams = analysisResult.Streams;
            // true => implicit (streams.), false => specific variable
            var streamsInstructionIds = new Dictionary<int, bool>();

            var method = (OpFunction)buffer[methodStart];
            var methodType = (FunctionType)context.ReverseTypes[method.FunctionType];

            methodType = (FunctionType)new StreamsTypeReplace(streamsStructType).Visit(methodType);
            method.FunctionType = context.GetOrRegister(methodType);

            // Remap ids for streams type to actual struct type
            var remapIds = new Dictionary<int, int>
            {
                { context.GetOrRegister(new StreamsType()), context.GetOrRegister(streamsStructType) },
                { context.GetOrRegister(new PointerType(new StreamsType(), StorageClass.Private)), context.GetOrRegister(new PointerType(streamsStructType, StorageClass.Private)) },
                { context.GetOrRegister(new PointerType(new StreamsType(), StorageClass.Function)), context.GetOrRegister(new PointerType(streamsStructType, StorageClass.Function)) },
            };

            // TODO: remap method type!
            for (int index = methodStart; index < methodEnd; ++index)
            {
                var i = buffer[index];

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
                else if (i.Op == Op.OpFunctionCall && (OpFunctionCall)i is { } call)
                {
                    var calledMethodInfo = liveAnalysis.ReferencedMethods[call.Function];
                    // In case we copied the method, use the new ID
                    if (calledMethodInfo.ThisStageMethodId is int updatedMethodId)
                        call.Function = updatedMethodId;
                }

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
                (var methodStart, var methodEnd) = FindMethodBounds(buffer, functionId);
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

        public (int Start, int End) FindMethodBounds(NewSpirvBuffer buffer, int functionId)
        {
            int? start = null;
            for (var index = 0; index < buffer.Count; index++)
            {
                var instruction = buffer[index];
                if (instruction.Op is Op.OpFunction && ((OpFunction)instruction).ResultId == functionId)
                    start = index;
                if (instruction.Op is Op.OpFunctionEnd && start is int startIndex)
                    return (startIndex, index + 1);
            }
            throw new InvalidOperationException($"Could not find start of method {functionId}");
        }
    }
}
