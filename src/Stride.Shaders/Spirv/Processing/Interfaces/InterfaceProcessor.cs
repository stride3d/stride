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
            DeadCodeRemover.RemoveUnreferencedCode(buffer, context, analysisResult, liveAnalysis);

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

            var stage = ExecutionModelToStageId(executionModel);



            bool AddBuiltin(int variable, BuiltIn builtin) => BuiltinProcessor.AddBuiltin(context, variable, builtin);

            bool AddLocation(int variable, string location) => BuiltinProcessor.AddLocation(context, variable, location);

            int ConvertInterfaceVariable(SymbolType sourceType, SymbolType castType, int value) =>
                BuiltinProcessor.ConvertInterfaceVariable(buffer, context, sourceType, castType, value);

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

            // Generate entry point wrapper
            var (newEntryPointFunctionResultId, entryPointName) = EntryPointWrapperGenerator.GenerateWrapper(context,
                buffer, entryPoint, executionModel, analysisResult,
                liveAnalysis, inputStreams, outputStreams, patchInputStreams,
                patchOutputStreams, inputType, outputType, streamsType,
                constantsType, arrayInputSize, arrayOutputSize, streamsVariable.ResultId,
                patchConstantEntryPoint);

            // Move OpExecutionMode on new wrapper
            foreach (var i in context)
            {
                if (i.Op == Op.OpExecutionMode && (OpExecutionMode)i is { } executionMode)
                {
                    if (executionMode.EntryPoint == entryPoint.IdRef)
                        executionMode.EntryPoint = newEntryPointFunctionResultId;
                }
            }

            return (newEntryPointFunctionResultId, entryPointName);
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
                context.DeclareStructuredType(constantsType, context.Bound++);
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
