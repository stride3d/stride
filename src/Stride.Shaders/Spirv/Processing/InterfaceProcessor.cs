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
using Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Analysis;
using Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Cleanup;
using Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Transformation;

namespace Stride.Shaders.Spirv.Processing
{
    /// <summary>
    /// Help to process streams and simplify the interface (resources, methods, cbuffer) of the shader.
    /// </summary>
    public class InterfaceProcessor
    {
        public Action<int, int>? CodeInserted { get; set; }

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

            var analysisResult = StreamAnalyzer.Analyze(buffer, context);
            VariableMerger.MergeSameSemanticVariables(table, context, buffer, analysisResult);
            var streams = analysisResult.Streams;

            var liveAnalysis = new LiveAnalysis();
            ReadWriteAnalyzer.AnalyzeStreamReadWrites(buffer, context, entryPointPSOrCS.IdRef, analysisResult, liveAnalysis);

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
                DeadCodeRemover.ResetUsedThisStage(analysisResult, liveAnalysis);

                VariableMerger.PropagateStreamsFromPreviousStage(streams);

                foreach (var entryPoint in new[] { (ExecutionModel.TessellationControl, entryPointHS), (ExecutionModel.TessellationEvaluation, entryPointDS), (ExecutionModel.Geometry, entryPointGS) })
                {
                    if (entryPoint.Item2 != null)
                    {
                        ReadWriteAnalyzer.AnalyzeStreamReadWrites(buffer, context, entryPoint.Item2.IdRef, analysisResult, liveAnalysis);

                        // Find patch constant entry point and process it as well
                        var patchConstantEntryPoint = entryPoint.Item1 == ExecutionModel.TessellationControl ? ResolveHullPatchConstantEntryPoint(table, context, entryPoint.Item2) : null;
                        if (patchConstantEntryPoint != null)
                            ReadWriteAnalyzer.AnalyzeStreamReadWrites(buffer, context, patchConstantEntryPoint.IdRef, analysisResult, liveAnalysis);
                    
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
                        DeadCodeRemover.ResetUsedThisStage(analysisResult, liveAnalysis);
                    
                        VariableMerger.PropagateStreamsFromPreviousStage(streams);
                    
                        if (entryPointVS == null)
                            throw new InvalidOperationException($"{nameof(InterfaceProcessor)}: If a {stage} shader is specified, a vertex shader is needed too");
                    }
                }
                
                if (entryPointVS != null)
                {
                    ReadWriteAnalyzer.AnalyzeStreamReadWrites(buffer, context, entryPointVS.IdRef, analysisResult, liveAnalysis);

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
                            var semantic = SemanticAnalyzer.ParseSemantic(stream.Value.Semantic);
                            inputAttributes.Add(new ShaderInputAttributeDescription { Location = inputLayoutLocation, SemanticName = semantic.Name, SemanticIndex = semantic.Index });
                        }
                    }
                }
            }

            // This will remove a lot of unused methods, resources and variables
            // (while following proper rules to preserve rgroup, cbuffer, logical groups, etc.)
            DeadCodeRemover.RemoveUnreferencedCode(buffer, context, analysisResult, streams, liveAnalysis);

            return new(entryPoints, inputAttributes);
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
                    MethodDuplicator.DuplicateMethodIfNecessary(buffer, context, method.Key, analysisResult, liveAnalysis, CodeInserted);
                    StreamAccessPatcher.PatchStreamsAccesses(table, buffer, context, method.Key, streamsType, inputType, outputType, constantsType, streamsVariable.ResultId, analysisResult, liveAnalysis);
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
    }
}
