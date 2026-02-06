using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Generation;

internal static class EntryPointWrapperGenerator
{
    public static (int ResultId, string Name) GenerateWrapper(
        NewSpirvBuffer buffer,
        SpirvContext context,
        Symbol entryPoint,
        ExecutionModel executionModel,
        string stage,
        AnalysisResult analysisResult,
        LiveAnalysis liveAnalysis,
        List<(StreamVariableInfo Info, int InterfaceId, SymbolType InterfaceType)> inputStreams,
        List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> outputStreams,
        List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> patchInputStreams,
        List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> patchOutputStreams,
        List<StructuredTypeMember> inputFields,
        List<StructuredTypeMember> outputFields,
        StructType inputType,
        StructType outputType,
        StructType streamsType,
        StructType? constantsType,
        int? arrayInputSize,
        int? arrayOutputSize,
        int streamsVariableId,
        Symbol? patchConstantEntryPoint,
        List<int> entryPointExtraVariables)
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
                        inputFieldValues[inputIndex] = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.InterfaceType, stream.Info.Type, inputFieldValues[inputIndex]);
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
                                    inputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.InterfaceType, stream.Info.Type, inputResult);
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
                                    outputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.Info.Type, stream.InterfaceType, outputResult);
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
                var streamPointer = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Private)), context.Bound++, streamsVariableId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                var inputResult = buffer.Add(new OpLoad(context.Types[stream.Info.Type], context.Bound++, stream.InterfaceId, null, [])).ResultId;
                inputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.InterfaceType, stream.Info.Type, inputResult);
                buffer.Add(new OpStore(streamPointer, inputResult, null, []));
            }

            // Call main()
            buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPoint.IdRef, new(arguments)));

            // Copy variables from streams struct to output
            foreach (var stream in outputStreams)
            {
                var baseType = stream.Info.Type;
                var streamPointer = buffer.Add(new OpAccessChain(context.GetOrRegister(new PointerType(stream.Info.Type, Specification.StorageClass.Private)), context.Bound++, streamsVariableId, [context.CompileConstant(stream.Info.StreamStructFieldIndex).Id])).ResultId;
                var outputResult = buffer.Add(new OpLoad(context.Types[baseType], context.Bound++, streamPointer, null, [])).ResultId;
                outputResult = BuiltinProcessor.ConvertInterfaceVariable(buffer, context, stream.Info.Type, stream.InterfaceType, outputResult);
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
        entryPointInterfaceVariables[pvariableIndex++] = streamsVariableId;
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

        return (newEntryPointFunction.ResultId, entryPointName);
    }
}
