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
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using Stride.Shaders.Spirv.Processing.Interfaces.Analysis;
using Stride.Shaders.Spirv.Processing.Interfaces.Cleanup;
using Stride.Shaders.Spirv.Processing.Interfaces.Transformation;
using Stride.Shaders.Spirv.Processing.Interfaces.Generation;

namespace Stride.Shaders.Spirv.Processing.Interfaces
{
    /// <summary>
    /// Help to process streams and simplify the interface (resources, methods, cbuffer) of the shader.
    /// </summary>
    public class InterfaceProcessor
    {
        public Action<int, int>? CodeInserted { get; set; }

        public class EntryPointInfo
        {
            internal EntryPointInfo(string name, int id, ExecutionModel model, List<int> interfaceVariables)
            {
                Name = name;
                Id = id;
                Model = model;
                InterfaceVariables = interfaceVariables;
                Stage = model switch
                {
                    ExecutionModel.Vertex => ShaderStage.Vertex,
                    ExecutionModel.TessellationControl => ShaderStage.Hull,
                    ExecutionModel.TessellationEvaluation => ShaderStage.Domain,
                    ExecutionModel.Geometry => ShaderStage.Geometry,
                    ExecutionModel.Fragment => ShaderStage.Pixel,
                    ExecutionModel.GLCompute => ShaderStage.Compute,
                    _ => throw new NotImplementedException()
                };
            }

            public string Name { get; }
            public int Id { get; }
            public ShaderStage Stage { get; }
            internal ExecutionModel Model { get; }
            internal List<int> InterfaceVariables { get; }
            internal int? ArrayInputSize { get; init; }
        }

        public record Result(List<EntryPointInfo> EntryPoints, List<ShaderInputAttributeDescription> InputAttributes);

        Symbol? ResolveEntryPoint(SymbolTable table, string name)
        {
            table.TryResolveSymbol(name, out var entryPoint);
            return entryPoint?.Type switch
            {
                FunctionGroupType => entryPoint.GroupMembers[^1],
                _ => entryPoint
            };
        }

        public Result Process(SymbolTable table, SpirvBuffer buffer, SpirvContext context)
        {
            // OpEntryPoint emission is deferred to allow fixups (e.g. adding dummy DS inputs for HS-internal outputs)
            var entryPoints = new List<EntryPointInfo>();

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
                entryPoints.Add(GenerateStreamWrapper(table, buffer, context, ExecutionModel.GLCompute, entryPointCS, analysisResult, liveAnalysis, false));
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

                // SDSL convention: `override stage void PSMain() { }` at the leaf of the mixin chain (with no
                // further override) means "no pixel shader needed" — drop the Fragment entry point entirely.
                // Detect this by inspecting the resolved PSMain body in SPIR-V: if it contains only structural
                // instructions, the source body is empty and the convention applies. Anything else (clip/discard
                // → OpKill, function calls, stores, atomics, etc.) keeps the PS.
                var (psStart, psEnd) = SpirvBuilder.FindMethodBounds(buffer, entryPointPS.IdRef);
                bool psBodyIsEmpty = true;
                for (int idx = psStart; idx < psEnd; idx++)
                {
                    var op = buffer[idx].Op;
                    if (op is not (Op.OpFunction or Op.OpFunctionParameter or Op.OpLabel or Op.OpReturn or Op.OpReturnValue or Op.OpFunctionEnd))
                    {
                        psBodyIsEmpty = false;
                        break;
                    }
                }

                if (!psBodyIsEmpty)
                {
                    var psEntry = GenerateStreamWrapper(table, buffer, context, ExecutionModel.Fragment, entryPointPS, analysisResult, liveAnalysis, false);
                    entryPoints.Add(psEntry);

                    buffer.Add(new OpExecutionMode(psEntry.Id, ExecutionMode.OriginUpperLeft, []));

                    // Vulkan spec VUID-FragDepth-FragDepth-04216: a shader that writes the FragDepth
                    // builtin must declare the DepthReplacing execution mode. NVIDIA/AMD drivers
                    // tolerate the omission; strict validators (spirv-val, Lavapipe) reject it.
                    if (streams.Any(s => s.Value.Write && s.Value.Output
                        && s.Value.Semantic is { } sem
                        && sem.Equals("SV_DEPTH", StringComparison.OrdinalIgnoreCase)))
                    {
                        buffer.Add(new OpExecutionMode(psEntry.Id, ExecutionMode.DepthReplacing, []));
                    }
                }
                else
                {
                    // No PS wrapper generated — undo UsedAnyStage for methods marked during PS analysis
                    // (otherwise they survive dead code removal with unpatched OpStreamsSDSL instructions)
                    // Safe to clear UsedAnyStage since PS is always the first stage analyzed
                    foreach (var method in liveAnalysis.ReferencedMethods)
                    {
                        method.Value.UsedThisStage = false;
                        method.Value.UsedAnyStage = false;
                    }
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

                // Remember if a stage output SV_Position already (it should be the first active stage before pixel shader)
                bool requirePosition = true;

                // Reminder: we process stage in reverse GPU execution order
                foreach (var entryPoint in new[] { (ExecutionModel.Geometry, entryPointGS), (ExecutionModel.TessellationEvaluation, entryPointDS), (ExecutionModel.TessellationControl, entryPointHS) })
                {
                    if (entryPoint.Item2 != null)
                    {
                        ReadWriteAnalyzer.AnalyzeStreamReadWrites(buffer, context, entryPoint.Item2.IdRef, analysisResult, liveAnalysis);

                        // Find patch constant entry point and process
                        var patchConstantEntryPoint = entryPoint.Item1 == ExecutionModel.TessellationControl ? ResolveHullPatchConstantEntryPoint(table, context, entryPoint.Item2) : null;
                        if (patchConstantEntryPoint != null)
                            ReadWriteAnalyzer.AnalyzeStreamReadWrites(buffer, context, patchConstantEntryPoint.IdRef, analysisResult, liveAnalysis);

                        // If specific semantic are written to (i.e. SV_Position), they are expected at the end of vertex shader
                        foreach (var stream in streams)
                        {
                            if (stream.Value.Semantic is { } semantic)
                            {
                                if (semantic.ToUpperInvariant().StartsWith("SV_POSITION") && requirePosition)
                                {
                                    stream.Value.Output = true;
                                    requirePosition = false;
                                }

                                if (entryPoint.Item1 == ExecutionModel.TessellationControl
                                    && (semantic.ToUpperInvariant().StartsWith("SV_TESSFACTOR") || semantic.ToUpperInvariant().StartsWith("SV_INSIDETESSFACTOR")))
                                    stream.Value.Output = true;
                            }
                        }

                        var entry = GenerateStreamWrapper(table, buffer, context, entryPoint.Item1, entryPoint.Item2, analysisResult, liveAnalysis, false);
                        entryPoints.Add(entry);

                        // After HS processing, add dummy DS inputs for HS-internal outputs to avoid Vulkan interface mismatch warning
                        if (entryPoint.Item1 == ExecutionModel.TessellationControl)
                        {
                            var dsEntryPoint = entryPoints.FirstOrDefault(ep => ep.Model == ExecutionModel.TessellationEvaluation);
                            if (dsEntryPoint?.InterfaceVariables != null)
                            {
                                foreach (var stream in streams)
                                {
                                    if (stream.Value.InternalPatchConstantOutput && stream.Value.OutputLayoutLocation is { } location)
                                    {
                                        // Create a dummy Input variable in DS with the same Location to match the HS output
                                        var dummyInputId = context.Bound++;
                                        var variableType = stream.Value.Type;
                                        var inputPointerType = new PointerType(
                                            !stream.Value.Patch && dsEntryPoint.ArrayInputSize is { } arrayInputSize ? new ArrayType(variableType, arrayInputSize) : variableType,
                                            Specification.StorageClass.Input);
                                        context.Add(new OpVariable(context.GetOrRegister(inputPointerType), dummyInputId, Specification.StorageClass.Input, null));
                                        context.Add(new OpDecorate(dummyInputId, Decoration.Location, [location]));
                                        if (variableType is ScalarType or VectorType or MatrixType && !variableType.GetElementType().IsFloating())
                                            context.Add(new OpDecorate(dummyInputId, Decoration.Flat, []));
                                        dsEntryPoint.InterfaceVariables.Add(dummyInputId);
                                    }
                                }
                            }
                        }

                        // Reset cbuffer/resource/methods used for next stage
                        DeadCodeRemover.ResetUsedThisStage(analysisResult, liveAnalysis);

                        VariableMerger.PropagateStreamsFromPreviousStage(streams);

                        if (entryPointVS == null)
                            throw new InvalidOperationException($"{nameof(InterfaceProcessor)}: If a {entry.Stage} shader is specified, a vertex shader is needed too");
                    }
                }

                if (entryPointVS != null)
                {
                    ReadWriteAnalyzer.AnalyzeStreamReadWrites(buffer, context, entryPointVS.IdRef, analysisResult, liveAnalysis);

                    // If specific semantic are written to (i.e. SV_Position), they are expected at the end of vertex shader
                    foreach (var stream in streams)
                    {
                        if (stream.Value.Semantic is { } semantic)
                        {
                            if (semantic.ToUpperInvariant().StartsWith("SV_POSITION") && requirePosition)
                            {
                                stream.Value.Output = true;
                                requirePosition = false;
                            }
                        }
                    }

                    entryPoints.Add(GenerateStreamWrapper(table, buffer, context, ExecutionModel.Vertex, entryPointVS, analysisResult, liveAnalysis, true));

                    // Process shader input attributes
                    foreach (var stream in streams)
                    {
                        // Note: built-ins won't have a inputLayoutLocation so they will be skipped
                        if (stream.Value.Input && stream.Value.InputLayoutLocation is { } inputLayoutLocation)
                        {
                            if (stream.Value.Semantic == null)
                            {
                                var readBy = stream.Value.ReadByMethod is { } m ? $" (read by '{m}')" : "";
                                throw new InvalidOperationException(
                                    $"Stream '{stream.Value.Name}'{readBy} is expected as a vertex shader input and is never written by any shader stage before being read. " +
                                    $"Without a semantic it cannot be sourced from a vertex buffer, so it has to be produced by a shader: " +
                                    $"write to streams.{stream.Value.Name} before reading it, or give it a semantic if it really is a vertex buffer input.");
                            }
                            var semantic = SemanticAnalyzer.ParseSemantic(stream.Value.Semantic);
                            inputAttributes.Add(new ShaderInputAttributeDescription { Location = inputLayoutLocation, SemanticName = semantic.Name, SemanticIndex = semantic.Index });
                        }
                    }
                }
            }

            // Emit all OpEntryPoints (deferred to allow fixups like adding dummy DS inputs for HS-internal outputs)
            foreach (var ep in entryPoints)
                context.Add(new OpEntryPoint(ep.Model, ep.Id, ep.Name, [.. ep.InterfaceVariables]));

            // Entry points had their geometry stream parameter removed as their wrapper was
            // generated; any other method carrying one has to lose it too.
            RemoveGeometryStreamParameters(buffer, context);

            // This will remove a lot of unused methods, resources and variables
            // (while following proper rules to preserve rgroup, cbuffer, logical groups, etc.)
            DeadCodeRemover.RemoveUnreferencedCode(buffer, context, analysisResult, liveAnalysis);

            return new(entryPoints, inputAttributes);
        }


        /// <summary>
        /// Drops geometry stream output parameters from every method that still has one, and the
        /// matching argument from every call to them.
        /// <para>
        /// A <c>TriangleStream&lt;Output&gt;</c> parameter carries no data - appending goes through
        /// OpEmitVertexSDSL and the stage's output variables - but it cannot be dropped earlier:
        /// EntryPointWrapperGenerator reads the output topology off it to emit the OutputPoints /
        /// OutputLineStrip / OutputTriangleStrip execution mode. So it survives until here, where
        /// the entry point has already been stripped of it and everything else still has to be.
        /// </para>
        /// <para>
        /// The function type is rewritten through GetOrRegister rather than in place: a method and
        /// the entry point calling it share one OpTypeFunction when their signatures match, and
        /// mutating it for one silently rewrites the other - which is how the entry point's own
        /// removal left such a method with more parameters than its type declared.
        /// </para>
        /// </summary>
        private static void RemoveGeometryStreamParameters(SpirvBuffer buffer, SpirvContext context)
        {
            // Which parameter index each function loses.
            var removedParameters = new Dictionary<int, int>();

            var currentFunction = 0;
            var currentFunctionIndex = 0;
            var parameterIndex = 0;
            for (var index = 0; index < buffer.Count; index++)
            {
                var i = buffer[index];
                if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
                {
                    currentFunction = function.ResultId;
                    currentFunctionIndex = index;
                    parameterIndex = 0;
                }
                else if (i.Data.Op == Op.OpFunctionParameter && (OpFunctionParameter)i is { } parameter)
                {
                    if (context.ReverseTypes.TryGetValue(parameter.ResultType, out var parameterType)
                        && parameterType is PointerType { BaseType: GeometryStreamType })
                    {
                        removedParameters[currentFunction] = parameterIndex;
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);

                        // Drop it from the function's own type too, unless a shared type already
                        // lost it when the entry point sharing that signature was rewritten.
                        var declaringFunction = (OpFunction)buffer[currentFunctionIndex];
                        if (context.ReverseTypes.TryGetValue(declaringFunction.FunctionType, out var declaredType)
                            && declaredType is FunctionType functionType
                            && parameterIndex < functionType.ParameterTypes.Count)
                        {
                            var remainingParameters = new List<FunctionParameter>(functionType.ParameterTypes);
                            remainingParameters.RemoveAt(parameterIndex);
                            declaringFunction.FunctionType = context.GetOrRegister(functionType with { ParameterTypes = remainingParameters });
                        }
                    }
                    parameterIndex++;
                }
            }

            if (removedParameters.Count == 0)
                return;

            // Rebuild the calls: an argument cannot be dropped in place, the instruction is shorter.
            for (var index = 0; index < buffer.Count; index++)
            {
                if (buffer[index].Data.Op != Op.OpFunctionCall)
                    continue;

                var call = (OpFunctionCall)buffer[index];
                if (!removedParameters.TryGetValue(call.Function, out var removedIndex))
                    continue;

                var arguments = call.Arguments.Elements.Span;
                if (removedIndex >= arguments.Length)
                    continue;

                Span<int> remaining = new int[arguments.Length - 1];
                arguments[..removedIndex].CopyTo(remaining);
                arguments[(removedIndex + 1)..].CopyTo(remaining[removedIndex..]);

                var (resultType, resultId, function) = (call.ResultType, call.ResultId, call.Function);
                buffer.RemoveRange(index, 1);
                buffer.Insert(index, new OpFunctionCall(resultType, resultId, function, new(remaining)));
            }
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

        private EntryPointInfo GenerateStreamWrapper(SymbolTable table, SpirvBuffer buffer, SpirvContext context, ExecutionModel executionModel, Symbol entryPoint, AnalysisResult analysisResult, LiveAnalysis liveAnalysis, bool isFirstActiveShader)
        {
            var streams = analysisResult.Streams;

            var stage = ExecutionModelToStageId(executionModel);

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

            // Generate stream variables
            GenerateStreamVariables(context, executionModel, streams, arrayInputSize, arrayOutputSize, out var inputStreams, out var outputStreams, out var patchInputStreams, out var patchOutputStreams);

            // Generate streams struct types (i.e. VS_STREAMS VS_INPUT and VS_OUTPUT)
            GenerateStreamStructTypes(context, executionModel, streams, inputStreams, outputStreams, out var inputType, out var outputType, out var streamsType, out var constantsType);

            // Create a static global streams variable
            var streamsVariable = context.Add(new OpVariable(context.GetOrRegister(new PointerType(streamsType, Specification.StorageClass.Private)), context.Bound++, Specification.StorageClass.Private, null));
            context.AddName(streamsVariable.ResultId, $"streams{stage}");

            var streamLayout = new StageStreamLayout(inputStreams, outputStreams, patchInputStreams, patchOutputStreams, inputType, outputType, streamsType, constantsType, arrayInputSize, arrayOutputSize, streamsVariable.ResultId);

            // Find patch constant entry point
            var patchConstantEntryPoint = executionModel == ExecutionModel.TessellationControl ? ResolveHullPatchConstantEntryPoint(table, context, entryPoint) : null;

            // Generate entry point wrapper
            var entryPointInfo = EntryPointWrapperGenerator.GenerateWrapper(context,
                buffer, entryPoint, executionModel, analysisResult,
                liveAnalysis, streamLayout, patchConstantEntryPoint);

            // Patch any OpStreams/OpAccessChain to use the new struct
            foreach (var method in liveAnalysis.ReferencedMethods)
            {
                if (method.Value.UsedThisStage && method.Value.HasStreamAccess)
                {
                    MethodDuplicator.DuplicateMethodIfNecessary(buffer, context, method.Key, analysisResult, liveAnalysis, CodeInserted);
                    StreamAccessPatcher.PatchStreamsAccesses(table, buffer, context, method.Key, streamsType, inputType, outputType, constantsType, streamsVariable.ResultId, analysisResult, liveAnalysis, CodeInserted);
                }
            }

            // Move OpExecutionMode on new wrapper
            foreach (var i in context)
            {
                if (i.Op == Op.OpExecutionMode && (OpExecutionMode)i is { } executionMode)
                {
                    if (executionMode.EntryPoint == entryPoint.IdRef)
                        executionMode.EntryPoint = entryPointInfo.Id;
                }
            }

            return entryPointInfo;
        }

        private static string ExecutionModelToStageId(ExecutionModel executionModel)
        {
            return executionModel switch
            {
                ExecutionModel.Vertex => "VS",
                ExecutionModel.TessellationControl => "HS",
                ExecutionModel.TessellationEvaluation => "DS",
                ExecutionModel.Geometry => "GS",
                ExecutionModel.Fragment => "PS",
                ExecutionModel.GLCompute => "CS",
                _ => throw new NotImplementedException()
            };
        }

        private static void GenerateStreamStructTypes(SpirvContext context, ExecutionModel executionModel, Dictionary<int, StreamVariableInfo> streams, List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> inputStreams, List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> outputStreams, out StructType inputType, out StructType outputType, out StructType streamsType, out StructType? constantsType)
        {
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

            // Build input/output types
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

            var stage = ExecutionModelToStageId(executionModel);

            inputType = new StructType($"{stage}_INPUT", inputFields);
            outputType = new StructType($"{stage}_OUTPUT", outputFields);
            streamsType = new StructType($"{stage}_STREAMS", streamFields);
            bool hasConstants = executionModel is ExecutionModel.TessellationControl or ExecutionModel.TessellationEvaluation;
            constantsType = hasConstants ? new StructType($"{stage}_CONSTANTS", constantFields) : null;
            context.DeclareStructuredType(inputType, context.Bound++);
            context.DeclareStructuredType(outputType, context.Bound++);
            context.DeclareStructuredType(streamsType, context.Bound++);
            if (hasConstants)
                context.DeclareStructuredType(constantsType!, context.Bound++);
        }

        private static void GenerateStreamVariables(SpirvContext context, ExecutionModel executionModel, Dictionary<int, StreamVariableInfo> streams, int? arrayInputSize, int? arrayOutputSize, out List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> inputStreams, out List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> outputStreams, out List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> patchInputStreams, out List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> patchOutputStreams)
        {
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
            inputStreams = [];
            outputStreams = [];
            patchInputStreams = [];
            patchOutputStreams = [];

            var stage = ExecutionModelToStageId(executionModel);
            foreach (var stream in streams)
            {
                int RequiredLocations(SymbolType type)
                {
                    return type switch
                    {
                        ScalarType or VectorType => 1,
                        MatrixType m => m.Columns,
                        ArrayType a => a.Size,
                        _ => throw new NotSupportedException($"Unsupported type for location counting: {type}"),
                    };
                }

                if (stream.Value.Input)
                {
                    var variableId = context.Bound++;
                    var variableType = stream.Value.Type;
                    if (!ProcessBuiltinsDecoration(variableId, StreamVariableType.Input, stream.Value.Semantic, ref variableType))
                    {
                        if (stream.Value.InputLayoutLocation == null)
                        {
                            stream.Value.InputLayoutLocation = inputLayoutLocationCount;
                            inputLayoutLocationCount += RequiredLocations(variableType);
                        }
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

                    // Flat is required for non-floating-point interpolation, but not valid on vertex shader inputs
                    if (executionModel != ExecutionModel.Vertex
                        && stream.Value.Type is ScalarType or VectorType or MatrixType && !stream.Value.Type.GetElementType().IsFloating())
                        context.Add(new OpDecorate(variable, Decoration.Flat, []));

                    if (stream.Value.Patch)
                        context.Add(new OpDecorate(variable, Decoration.Patch, []));

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
                            stream.Value.OutputLayoutLocation = outputLayoutLocationCount;
                            outputLayoutLocationCount += RequiredLocations(variableType);
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

                    // Flat is required for non-floating-point interpolation, but not valid on fragment shader outputs
                    if (executionModel != ExecutionModel.Fragment
                        && stream.Value.Type is ScalarType or VectorType or MatrixType && !stream.Value.Type.GetElementType().IsFloating())
                        context.Add(new OpDecorate(variable, Decoration.Flat, []));

                    if (stream.Value.Patch)
                        context.Add(new OpDecorate(variable, Decoration.Patch, []));

                    stream.Value.OutputId = variable.ResultId;
                    (stream.Value.Patch ? patchOutputStreams : outputStreams).Add((stream.Value, variable.ResultId, variableType));
                }
            }

            bool ProcessBuiltinsDecoration(int variable, StreamVariableType type, string? semantic, ref SymbolType symbolType) =>
                BuiltinProcessor.ProcessBuiltinsDecoration(context, executionModel, variable, type, semantic, ref symbolType);
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
