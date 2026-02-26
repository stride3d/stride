using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using Stride.Shaders.Spirv.Processing.Interfaces.Transformation;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Generation;

internal static class EntryPointWrapperGenerator
{
    public static InterfaceProcessor.EntryPointInfo GenerateWrapper(SpirvContext context,
        SpirvBuffer buffer,
        Symbol entryPoint,
        ExecutionModel executionModel,
        AnalysisResult analysisResult,
        LiveAnalysis liveAnalysis,
        StageStreamLayout streamLayout,
        Symbol? patchConstantEntryPoint)
    {
        var entryPointFunctionType = (FunctionType)entryPoint.Type;
        var voidType = context.GetOrRegister(ScalarType.Void);

        // Add new entry point wrapper
        var newEntryPointFunctionType = context.GetOrRegister(new FunctionType(ScalarType.Void, []));
        var newEntryPointFunction = buffer.Add(new OpFunction(voidType, context.Bound++, FunctionControlMask.None, newEntryPointFunctionType));
        buffer.Add(new OpLabel(context.Bound++));
        var variableInsertIndex = buffer.Count;
        var entryPointName = $"{entryPoint.Id.Name}_Wrapper";
        context.AddName(newEntryPointFunction, entryPointName);

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
        var entryPointExtraVariables = new List<int>();

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
            if (!BuiltinProcessor.ProcessBuiltinsDecoration(context, executionModel, variableId, StreamVariableType.Input, semantic, ref type))
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
        if (streamLayout.ArrayInputSize != null)
        {
            // Copy variables to Input[X] which is first method parameter of main()
            // Pattern is a loop over index i looking like:
            //  inputs[i].Position = gl_Position[i];
            //  inputs[i].Normal = in_GS_normals[i];
            var inputsVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(new ArrayType(streamLayout.InputType, streamLayout.ArrayInputSize.Value), Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
            context.AddName(inputsVariable, "inputs");

            int ConvertInputsArray()
            {
                Span<int> inputLoadValues = stackalloc int[streamLayout.InputType.Members.Count];
                for (var inputIndex = 0; inputIndex < streamLayout.InputStreams.Count; inputIndex++)
                {
                    var stream = streamLayout.InputStreams[inputIndex];
                    var loadedValue = buffer.Add(new OpLoad(context.GetOrRegister(new ArrayType(stream.Info.Type, streamLayout.ArrayInputSize.Value)), context.Bound++, stream.Id, null, []));
                    inputLoadValues[inputIndex] = loadedValue.ResultId;
                }

                Span<int> inputFieldValues = stackalloc int[streamLayout.InputType.Members.Count];
                Span<int> inputValues = stackalloc int[streamLayout.ArrayInputSize.Value];
                for (int arrayIndex = 0; arrayIndex < streamLayout.ArrayInputSize; ++arrayIndex)
                {
                    for (var inputIndex = 0; inputIndex < streamLayout.InputStreams.Count; inputIndex++)
                    {
                        var stream = streamLayout.InputStreams[inputIndex];
                        inputFieldValues[inputIndex] = buffer.Add(new OpCompositeExtract(context.Types[stream.Info.Type], context.Bound++, inputLoadValues[inputIndex], [arrayIndex])).ResultId;
                        inputFieldValues[inputIndex] = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.InterfaceType, stream.Info.Type, inputFieldValues[inputIndex]);
                    }

                    inputValues[arrayIndex] = buffer.Add(new OpCompositeConstruct(context.GetOrRegister(streamLayout.InputType), context.Bound++, [.. inputFieldValues])).ResultId;
                }

                var inputsData1 = buffer.Add(new OpCompositeConstruct(context.GetOrRegister(new ArrayType(streamLayout.InputType, streamLayout.ArrayInputSize.Value)), context.Bound++, [.. inputValues])).ResultId;
                return inputsData1;
            }

            var inputsData = ConvertInputsArray();

            buffer.Add(new OpStore(inputsVariable, inputsData, null, []));

            var entryPointTypeId = context.GetOrRegister(entryPoint.Type);
            if (executionModel == ExecutionModel.TessellationControl || executionModel == ExecutionModel.TessellationEvaluation)
            {
                var arraySize = executionModel == ExecutionModel.TessellationControl
                    ? streamLayout.ArrayOutputSize ?? throw new InvalidOperationException("Can't figure array output size for tessellation shader")
                    : streamLayout.ArrayInputSize.Value;
                bool hullTessellationOutputsGenerated = false;
                int GenerateHullTessellationOutputs()
                {
                    if (hullTessellationOutputsGenerated)
                        throw new InvalidOperationException("Hull OutputPatch can only be used in once place (constant patch)");
                    hullTessellationOutputsGenerated = true;
                    var outputsVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(new ArrayType(streamLayout.OutputType, arraySize), Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
                    context.AddName(outputsVariable, "outputs");

                    for (int arrayIndex = 0; arrayIndex < arraySize; ++arrayIndex)
                    {
                        for (var outputIndex = 0; outputIndex < streamLayout.OutputStreams.Count; outputIndex++)
                        {
                            var stream = streamLayout.OutputStreams[outputIndex];
                            var outputsVariablePtr = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Function)),
                                context.Bound++, outputsVariable,
                                [context.CompileConstant(arrayIndex).Id, context.CompileConstant(outputIndex).Id])).ResultId;
                            var outputSourcePtr = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Output)),
                                context.Bound++, stream.Id,
                                [context.CompileConstant(arrayIndex).Id])).ResultId;
                            var outputsSourceValue = buffer.Add(new OpLoad(context.GetOrRegister(stream.Info.Type), context.Bound++, outputSourcePtr, null, [])).ResultId;
                            outputsSourceValue = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.Info.Type, stream.InterfaceType, outputsSourceValue);
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
                                    arguments[i] = inputsVariable;
                                    break;
                                }
                            // Hull outputs
                            case PatchType { Kind: PatchTypeKindSDSL.Output } outputPatchType when executionModel == ExecutionModel.TessellationControl:
                                {
                                    arguments[i] = GenerateHullTessellationOutputs();
                                    break;
                                }
                            case StreamsType t when t.Kind is StreamsKindSDSL.Constants && parameterModifiers is ParameterModifiers.None or ParameterModifiers.In:
                                {
                                    // Parameter is "HS_CONSTANTS constants"
                                    var constantVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(streamLayout.ConstantsType, Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
                                    arguments[i] = constantVariable;
                                    // Copy back values from semantic/builtin variables to Constants struct
                                    foreach (var stream in streamLayout.PatchInputStreams)
                                    {
                                        var inputPtr = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Function)), context.Bound++, constantVariable, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                                        var inputResult = buffer.Add(new OpLoad(context.GetOrRegister(stream.Info.Type), context.Bound++, stream.Id, null, [])).ResultId;
                                        inputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.InterfaceType, stream.Info.Type, inputResult);
                                        buffer.Add(new OpStore(inputPtr, inputResult, null, []));
                                    }
                                    break;
                                }
                            case StreamsType t when t.Kind is StreamsKindSDSL.Output or StreamsKindSDSL.Constants && parameterModifiers == ParameterModifiers.Out:
                                {
                                    // Parameter is "out HS_OUTPUT output" or "out HS_CONSTANTS constants"
                                    var structType = t.Kind switch
                                    {
                                        StreamsKindSDSL.Output => streamLayout.OutputType,
                                        StreamsKindSDSL.Constants => streamLayout.ConstantsType,
                                    };
                                    var outVariable = buffer.Insert(variableInsertIndex++, new OpVariable(context.GetOrRegister(new PointerType(structType, Specification.StorageClass.Function)), context.Bound++, Specification.StorageClass.Function, null)).ResultId;
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
                            case StreamsType { Kind: StreamsKindSDSL.Output } when parameterModifiers == ParameterModifiers.Out:
                                {
                                    // Parameter is "out HS_OUTPUT output"
                                    var outputVariable = arguments[i];
                                    // Load as value
                                    outputVariable = buffer.Add(new OpLoad(context.GetOrRegister(streamLayout.OutputType), context.Bound++, outputVariable, null, [])).ResultId;
                                    // Do we need to index into array? if yes, get index (gl_invocationID)
                                    int? invocationIdValue = streamLayout.ArrayOutputSize != null ? GetOrDeclareBuiltInValue(ScalarType.UInt, "SV_OutputControlPointID") : null;
                                    // Copy back values from Output struct to semantic/builtin variables
                                    for (var outputIndex = 0; outputIndex < streamLayout.OutputStreams.Count; outputIndex++)
                                    {
                                        var stream = streamLayout.OutputStreams[outputIndex];
                                        var outputResult = buffer.Add(new OpCompositeExtract(context.GetOrRegister(stream.Info.Type), context.Bound++, outputVariable, [outputIndex])).ResultId;
                                        outputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.Info.Type, stream.InterfaceType, outputResult);
                                        var outputTargetPtr = streamLayout.ArrayOutputSize != null
                                            ? buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Output)),
                                                context.Bound++, stream.Id,
                                                [invocationIdValue.Value])).ResultId
                                            : stream.Id;
                                        buffer.Add(new OpStore(outputTargetPtr, outputResult, null, []));
                                    }
                                    break;
                                }
                            case StreamsType { Kind: StreamsKindSDSL.Constants } when parameterModifiers == ParameterModifiers.Out:
                                {
                                    // Parameter is "out HS_OUTPUT output"
                                    var outputVariable = arguments[i];
                                    // Load as value
                                    outputVariable = buffer.Add(new OpLoad(context.GetOrRegister(streamLayout.ConstantsType ?? throw new InvalidOperationException()), context.Bound++, outputVariable, null, [])).ResultId;
                                    // Copy back values from Output struct to semantic/builtin variables
                                    foreach (var stream in streamLayout.PatchOutputStreams)
                                    {
                                        var outputResult = buffer.Add(new OpCompositeExtract(context.GetOrRegister(stream.Info.Type), context.Bound++, outputVariable, [stream.Info.StreamStructFieldIndex])).ResultId;
                                        outputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.Info.Type, stream.InterfaceType, outputResult);
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
                // TODO: Check it's really the 2nd parameter
                SpirvBuilder.FunctionRemoveParameter(context, buffer, entryPoint, 1);

                // Extract and remove execution mode (line, point, triangleadj, etc.)
                var executionMode = entryPointFunctionType.ParameterTypes[0].Modifiers;
                if (executionMode == ParameterModifiers.None)
                    throw new InvalidOperationException("Execution mode primitive is missing for first parameter of geometry shader");
                entryPointFunctionType.ParameterTypes[0] = entryPointFunctionType.ParameterTypes[0] with { Modifiers = ParameterModifiers.None };
                entryPointFunctionType.ParameterTypes.RemoveAt(1);

                context.ReplaceType(entryPointFunctionType, entryPointTypeId);
                context.Add(new OpExecutionMode(entryPoint.IdRef, executionMode switch
                {
                    ParameterModifiers.Point => ExecutionMode.InputPoints,
                    ParameterModifiers.Line => ExecutionMode.InputLines,
                    ParameterModifiers.LineAdjacency => ExecutionMode.InputLinesAdjacency,
                    ParameterModifiers.Triangle => ExecutionMode.Triangles,
                    ParameterModifiers.TriangleAdjacency => ExecutionMode.InputTrianglesAdjacency,
                }, []));

                arguments[0] = inputsVariable;

                // Call main(inputs) without 2nd argument
                buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPoint.IdRef, [arguments[0], .. arguments[2..]]));
            }
        }
        else
        {
            // We assume a void returning function and Input/Output is all handled with streams
            // Note: we could in the future support having Input/Output in the function signature, just like we do for HS/DS/GS

            // Copy variables from input to streams struct
            foreach (var stream in streamLayout.InputStreams)
            {
                var streamPointer = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Private)), context.Bound++, streamLayout.StreamsVariableId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                var inputResult = buffer.Add(new OpLoad(context.Types[stream.Info.Type], context.Bound++, stream.Id, null, [])).ResultId;
                inputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.InterfaceType, stream.Info.Type, inputResult);
                buffer.Add(new OpStore(streamPointer, inputResult, null, []));
            }

            // Call main()
            buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPoint.IdRef, new(arguments)));

            // Copy variables from streams struct to output
            foreach (var stream in streamLayout.OutputStreams)
            {
                var baseType = stream.Info.Type;
                var streamPointer = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Private)), context.Bound++, streamLayout.StreamsVariableId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                var outputResult = buffer.Add(new OpLoad(context.Types[baseType], context.Bound++, streamPointer, null, [])).ResultId;
                outputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.Info.Type, stream.InterfaceType, outputResult);
                buffer.Add(new OpStore(stream.Id, outputResult, null, []));
            }
        }

        buffer.Add(new OpReturn());
        buffer.Add(new OpFunctionEnd());

        // Note: we overallocate and filter with UsedThisStage after
        Span<int> entryPointInterfaceVariables = stackalloc int[streamLayout.InputStreams.Count + streamLayout.OutputStreams.Count + streamLayout.PatchInputStreams.Count + streamLayout.PatchOutputStreams.Count + 1 + analysisResult.Variables.Count + analysisResult.CBuffers.Count + analysisResult.Resources.Count + entryPointExtraVariables.Count];
        int pvariableIndex = 0;
        foreach (var inputStream in streamLayout.InputStreams)
            entryPointInterfaceVariables[pvariableIndex++] = inputStream.Id;
        foreach (var outputStream in streamLayout.OutputStreams)
            entryPointInterfaceVariables[pvariableIndex++] = outputStream.Id;
        foreach (var inputStream in streamLayout.PatchInputStreams)
            entryPointInterfaceVariables[pvariableIndex++] = inputStream.Id;
        foreach (var outputStream in streamLayout.PatchOutputStreams)
            entryPointInterfaceVariables[pvariableIndex++] = outputStream.Id;
        entryPointInterfaceVariables[pvariableIndex++] = streamLayout.StreamsVariableId;
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

        return new InterfaceProcessor.EntryPointInfo(entryPointName, newEntryPointFunction.ResultId, executionModel, [.. entryPointInterfaceVariables.Slice(0, pvariableIndex)]) { ArrayInputSize = streamLayout.ArrayInputSize };
    }
}
