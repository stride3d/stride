using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.SPIRV.Cross;
using Stride.Core.Extensions;
using Stride.Shaders.Compilers.Direct3D;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.PostProcessing;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer(IExternalShaderLoader shaderLoader)
{
    public IExternalShaderLoader ShaderLoader { get; } = shaderLoader;
    public void MergeSDSL(ShaderSource shaderSource, out byte[] bytecode, out EffectReflection effectReflection)
    {
        var temp = new NewSpirvBuffer();

        var context = new SpirvContext();
        var table = new SymbolTable(context) { ShaderLoader = ShaderLoader };

        var effectEvaluator = new EffectEvaluator(ShaderLoader);
        shaderSource = effectEvaluator.EvaluateEffects(shaderSource);

        var shaderSource2 = EvaluateInheritanceAndCompositions(shaderSource);

        // Root shader
        var globalContext = new MixinGlobalContext();
        var rootMixin = MergeMixinNode(globalContext, context, table, temp, shaderSource2);

        context.Insert(0, new OpCapability(Capability.Shader));
        context.Insert(1, new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        context.Insert(2, new OpExtension("SPV_GOOGLE_hlsl_functionality1"));

        // Process streams and remove unused code/cbuffer/variable/resources
        new InterfaceProcessor().Process(table, temp, context);

        // Merge cbuffers and rgroups
        // TODO: remove unused cbuffers (before merging them)
        MergeCBuffers(globalContext, context, temp);
        ComputeCBufferOffsets(globalContext, context, temp);

        // Try to give variables more sensible names
        // Note: since we mutate OpName and globalContext.Names, try to do that as late as possible because some code earlier use names to match variables/types
        RenameVariables(globalContext, context, temp);

        // Process reflection
        ProcessReflection(globalContext, context, temp, rootMixin);

        foreach (var inst in context)
            temp.Add(inst.Data);

        CleanupUnnecessaryInstructions(globalContext, temp);

        temp.Sort();

        // Final processing
        SpirvProcessor.Process(temp);


        temp.Sort();

        bytecode = temp.ToBytecode();

#if DEBUG
        File.WriteAllBytes("test.spv", bytecode);
        File.WriteAllText("test.spvdis", Spv.Dis(temp));
        Spv.Dis(temp, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
#endif

        effectReflection = globalContext.Reflection;
    }

    class MixinGlobalContext
    {
        public Dictionary<int, string> Names { get; } = [];
        public Dictionary<int, SymbolType> Types { get; } = [];

        public EffectReflection Reflection { get; } = new();

        public Dictionary<int, string> ExternalShaders { get; } = new();
        public Dictionary<int, (int ShaderId, string Name, int FunctionType)> ExternalFunctions { get; } = new();
        public Dictionary<int, (int ShaderId, string Name)> ExternalVariables { get; } = new();
    }

    class MixinNodeContext
    {
        public MixinNode? Result { get; }
    }

    struct LinkInfo
    {
        public string LinkName;
        public string ResourceGroup;
        public string LogicalGroup;
    }

    MixinNode MergeMixinNode(MixinGlobalContext globalContext, SpirvContext context, SymbolTable table, NewSpirvBuffer buffer, ShaderMixinInstantiation mixinSource, MixinNode? stage = null, string? currentCompositionPath = null)
    {
        // We emit OPSDSLEffect for any non-root composition
        if (currentCompositionPath != null)
            buffer.Add(new OpSDSLComposition(currentCompositionPath));

        var mixinNode = new MixinNode(stage, currentCompositionPath);
        var contextStart = context.GetBuffer().Count;

        // Merge all classes from mixinSource.Mixins in main buffer
        ProcessMixinClasses(globalContext, context, buffer, mixinSource, mixinNode);

        BuildTypesAndMethodGroups(globalContext, context, table, buffer, mixinNode);

        // Compositions (recursive)
        foreach (var shader in mixinNode.Shaders)
        {
            foreach (var variable in shader.Variables)
            {
                if (variable.Value.Type is PointerType pointer && pointer.BaseType is ShaderSymbol or ArrayType { BaseType: ShaderSymbol })
                {
                    var compositionMixins = mixinSource.Compositions[variable.Key];
                    var isCompositionArray = pointer.BaseType is ArrayType { BaseType: ShaderSymbol };

                    if (!isCompositionArray && compositionMixins.Length != 1)
                        throw new InvalidOperationException($"Composition variable {variable.Key} is not an array but had {compositionMixins.Length} entries");

                    var compositionResults = new MixinNode[compositionMixins.Length];
                    for (int i = 0; i < compositionMixins.Length; ++i)
                    {
                        var compositionPath = currentCompositionPath != null ? $"{currentCompositionPath}.{variable.Key}" : variable.Key;
                        if (isCompositionArray)
                            compositionPath += $"[{i}]";
                        compositionResults[i] = MergeMixinNode(globalContext, context, table, buffer, compositionMixins[i], mixinNode.IsRoot ? mixinNode : mixinNode.Stage, compositionPath);
                    }

                    if (isCompositionArray)
                        mixinNode.CompositionArrays.Add(variable.Value.Id, compositionResults);
                    else
                        mixinNode.Compositions.Add(variable.Value.Id, compositionResults[0]);
                }
            }
        }

        // Patch method calls (virtual calls & base calls)
        ProcessMemberAccessAndForeach(globalContext, context, buffer, mixinNode);

        if (currentCompositionPath != null)
            buffer.Add(new OpSDSLCompositionEnd());

        return mixinNode;
    }

    private void ProcessMixinClasses(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer, ShaderMixinInstantiation mixinSource, MixinNode mixinNode)
    {
        mixinNode.StartInstruction = buffer.Count;
        foreach (var shaderClass in mixinSource.Mixins)
        {
            var contextStart = context.GetBuffer().Count;

            var shaderInfo = MergeClassInBuffers(globalContext, context, buffer, mixinNode, shaderClass);

            mixinNode.ShadersByName.Add(shaderClass.ToClassName(), shaderInfo);
            mixinNode.Shaders.Add(shaderInfo);

            // Note: we process name, types and struct right away, as they might be needed by the next Shader
            ShaderClass.ProcessNameAndTypes(context.GetBuffer(), contextStart, context.GetBuffer().Count, globalContext.Names, globalContext.Types);
            PopulateShaderInfo(globalContext, context.GetBuffer(), contextStart, context.GetBuffer().Count, buffer, shaderInfo.StartInstruction, shaderInfo.EndInstruction, shaderInfo, mixinNode);
        }

        mixinNode.EndInstruction = buffer.Count;
    }

    private ShaderInfo MergeClassInBuffers(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer, MixinNode mixinNode, ShaderClassInstantiation shaderClass)
    {
        var isRootMixin = mixinNode.Stage == null;
        if (shaderClass.ImportStageOnly)
        {
            if (!isRootMixin)
                throw new InvalidOperationException("importing stage-only methods/variables is only possible at the root mixin");
        }

        var shader = shaderClass.Buffer;
        var offset = context.Bound;
        var resourceGroupOffset = context.ResourceGroupBound;

        // Remember when we started to add instructions in both context and main buffer
        var shaderStart = buffer.Count;
        var contextStart = context.GetBuffer().Count;
        var names = new Dictionary<int, string>();

        var forbiddenIds = new HashSet<int>();
        var remapIds = new Dictionary<int, int>();
        var removedIds = new HashSet<int>();

        bool isContext = true;

        // Note: FunctionType is only required when looking for stage function
        bool ProcessStageMemberOrType(int memberId, FunctionType? functionType, bool isStage)
        {
            var include = isStage switch
            {
                // Import stage members only if at root level
                true => isRootMixin,
                // Import non-stage members only if allowed, i.e. not a "stage-only inherit"
                // ("stage-only inherit" only happen when a class with stage members is inherited in a composition, and the stage-only version is added to the root mixin)
                false => !shaderClass.ImportStageOnly,
            };

            // If a stage member is skipped in a composition mixin, we want to remap to the version in the root mixin
            if (isStage && !isRootMixin)
            {
                var stageShader = mixinNode.Stage.ShadersByName[shaderClass.ToClassName()];
                var memberOrTypeName = names[memberId];
                var stageMemberOrTypeId = stageShader.StructTypes.TryGetValue(memberOrTypeName, out var structTypeId)
                    ? structTypeId
                    : stageShader.FindMember(memberOrTypeName, functionType).Id;
                remapIds.Add(offset + memberId, stageMemberOrTypeId);
                removedIds.Add(stageMemberOrTypeId);
            }
            // Otherwise, if not included, it means we need to forbid this IDs (which could only happen if referencing non-stage member from a stage method)
            else if (!include)
            {
                forbiddenIds.Add(offset + memberId);
            }

            return include;
        }

        var typeDuplicateInserter = new TypeDuplicateHelper(context.GetBuffer());

        var structTypes = new Dictionary<string, int>();

        // Copy instructions to main buffer
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];

            // Do we need to skip variable/functions? (depending on stage/non-stage)
            {
                var include = true;
                if (i.Op == Op.OpName)
                {
                    OpName nameInstruction = i;
                    names.Add(nameInstruction.Target, nameInstruction.Name);
                }
                if (i.Op == Op.OpFunction && (OpFunction)i is { } function && shader[index + 1].Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)shader[index + 1] is { } functionInfo)
                {
                    var isStage = (functionInfo.Flags & FunctionFlagsMask.Stage) != 0;
                    // Note: BuildTypesAndMethodGroups has not been called for this mixin so context.Types/ReverseTypes is not filled
                    //       However:
                    //        - FunctionType is only required when looking for stage function
                    //        - In that case, root stage mixin MergeMixinNode => BuildTypesAndMethodGroups would have been called for this function type
                    //        - function type is already deduplicated (in this loop)
                    //       So the lookup will work when it is necessary

                    FunctionType? functionType = default;
                    // First, assuming FunctionType is a duplicate from a previous shader, we could find the already existing type by applying offset and remapIds
                    if (remapIds.TryGetValue(function.FunctionType + offset, out var remappedFunctionTypeId)
                        // Then, we can find the actual type in context.ReverseTypes
                        && context.ReverseTypes.TryGetValue(remappedFunctionTypeId, out var functionType2))
                        functionType = (FunctionType)functionType2;

                    include = ProcessStageMemberOrType(function.ResultId, functionType, isStage);
                }
                if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variableInstruction)
                {
                    var isStage = (variableInstruction.Flags & VariableFlagsMask.Stage) != 0;
                    include = ProcessStageMemberOrType(variableInstruction.ResultId, null, isStage);
                }
                if (i.Op == Op.OpTypeStruct && (OpTypeStruct)i is { } typeStruct)
                {
                    include = ProcessStageMemberOrType(typeStruct.ResultId, null, true);
                }

                if (!include)
                {
                    // We store removed IDs for further OpName removals
                    if (i.Data.IdResult is int id)
                        removedIds.Add(offset + id);

                    // Special case for function: skip until function end
                    // (for other cases such as variable, skipping only current instruction is enough)
                    if (i.Op == Op.OpFunction)
                    {
                        // Skip until end of function
                        while (shader[++index].Op != Op.OpFunctionEnd)
                        {
                            // We store removed IDs for further OpName removals
                            if (shader[index].Data.IdResult is int id2)
                                removedIds.Add(offset + id2);
                        }
                    }

                    // Go to next instruction
                    continue;
                }
            }

            var i2 = new OpData(i.Data.Memory.Span);

            if (offset > 0)
                OffsetIds(i2, offset);

            if (i2.IdResult != null)
                context.Bound = Math.Max(context.Bound, i2.IdResult.Value + 1);

            // ResourceGroupId: adjust offsets too
            if (i2.Op == Op.OpDecorate && new OpDecorate(ref i2) is { Decoration: { Value: Decoration.ResourceGroupIdSDSL, Parameters: { } m } } resourceGroupIdDecorate)
            {
                // Somehow data doesn't get mutated inside i2 if we update resourceGroupIdDecorate.Decoration, so we reference buffer directly
                var n = new LiteralValue<int>(m.Span);
                n.Value += resourceGroupOffset;
                resourceGroupIdDecorate.Decoration = new(resourceGroupIdDecorate.Decoration.Value, n.Words);
                context.ResourceGroupBound = Math.Max(context.ResourceGroupBound, n.Value + 1);
                n.Dispose();
            }

            if (SpirvBuilder.ContainIds(forbiddenIds, i2))
                throw new InvalidOperationException($"Stage instruction {i.Data} references a non-stage ID");

            SpirvBuilder.RemapIds(remapIds, ref i2);

            // Detect when we switch from context to main buffer
            if (i2.Op == Op.OpSDSLShader)
            {
                isContext = false;
            }

            // Specific type instructions in context gets deduplicated before adding
            bool addToContext = false;
            if (
                // Types
                i2.Op == Op.OpTypeVoid
                || i2.Op == Op.OpTypeInt
                || i2.Op == Op.OpTypeFloat
                || i2.Op == Op.OpTypeBool
                || i2.Op == Op.OpTypeVector
                || i2.Op == Op.OpTypeMatrix
                || i2.Op == Op.OpTypeArray
                || i2.Op == Op.OpTypeRuntimeArray
                || i2.Op == Op.OpTypePointer
                || i2.Op == Op.OpTypeFunction
                || i2.Op == Op.OpTypeFunctionSDSL
                || i2.Op == Op.OpTypeImage
                || i2.Op == Op.OpTypeSampler
                || i2.Op == Op.OpTypeGenericSDSL
                || i2.Op == Op.OpSDSLImportShader
                || i2.Op == Op.OpSDSLImportVariable
                || i2.Op == Op.OpSDSLImportFunction
                || i2.Op == Op.OpSDSLImportStruct)
            {
                // We need to replace those right now (otherwise further types depending on this struct won't get properly translated)
                if (i2.Op == Op.OpSDSLImportStruct && new OpSDSLImportStruct(ref i2) is { } importStruct)
                {
                    var shaderName = globalContext.ExternalShaders[importStruct.Shader];
                    var shader2 = mixinNode.ShadersByName[shaderName];
                    if (!shader2.StructTypes.TryGetValue(importStruct.StructName, out var structId)
                        && (shader2.Stage == null || !shader2.Stage.StructTypes.TryGetValue(importStruct.StructName, out structId)))
                        throw new InvalidOperationException($"Struct {importStruct.StructName} not found in shader {shaderName}");
                    remapIds.Add(importStruct.ResultId, structId);
                    removedIds.Add(structId);
                }
                else
                {
                    // Check if type already exists in context (deduplicate them)
                    if (typeDuplicateInserter.CheckForDuplicates(i2, out var existingInstruction))
                    {
                        if (i2.IdResult is int id)
                        {
                            remapIds.Add(id, existingInstruction.IdResult.Value);
                            removedIds.Add(existingInstruction.IdResult.Value);
                        }
                    }
                    else
                    {
                        addToContext = true;
                    }
                }
            }
            // Does this belong in context or buffer?
            else if (isContext)
            {
                addToContext = true;
            }
            else
            {
                buffer.Add(i2);
            }

            // OpTypeStruct is the only type that can be defined by the shader.
            // In case it's deduplicated (i.e. used in two separate mixin nodes), we still want to have it in shaderInfo.StructTypes, so let's save it aside now.
            if (i2.Op == Op.OpTypeStruct && new OpTypeStruct(ref i2) is { } typeStruct2)
            {
                var structName = names[typeStruct2.ResultId - offset];
                if (!remapIds.TryGetValue(typeStruct2.ResultId, out var structId))
                    structId = typeStruct2.ResultId;
                structTypes.Add(structName, structId);
            }

            // Process OpSDSLImport
            ProcessImportInfo(globalContext, mixinNode, ref i2, context.GetBuffer());

            if (addToContext)
            {
                // OpName and such: check if associated instruction has not been removed
                if (i2.Op == Op.OpName || i2.Op == Op.OpDecorate || i2.Op == Op.OpDecorateString
                    || i2.Op == Op.OpMemberName || i2.Op == Op.OpMemberDecorate || i2.Op == Op.OpMemberDecorateString)
                {
                    // Target/Structure ID is always stored in first operand for all those instructions
                    var target = i2.Memory.Span[1];
                    if (removedIds.Contains(target))
                        addToContext = false;
                }

                if (addToContext)
                {
                    var i2Index = context.GetBuffer().Add(i2);
                }
            }
        }

        // Reprocess OpName/OpDecorate (they are defined before the OpType that was remapped, so we need to reprocess them)
        for (int index = contextStart; index < context.GetBuffer().Count; ++index)
        {
            var i = context.GetBuffer()[index];

            if (i.Op == Op.OpName
                || i.Op == Op.OpMemberName
                || i.Op == Op.OpDecorate
                || i.Op == Op.OpDecorateString
                || i.Op == Op.OpMemberDecorate
                || i.Op == Op.OpMemberDecorateString)
            {
                SpirvBuilder.RemapIds(remapIds, ref i.Data);

                var target = i.Data.Memory.Span[1];
                if (removedIds.Contains(target))
                    SetOpNop(i.Data.Memory.Span);
            }
        }

        // Link attribute: postfix with composition path
        if (mixinNode.CompositionPath != null)
        {
            foreach (var i in buffer)
            {
                if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.LinkSDSL, Parameters: { } m } } memberDecorate)
                {
                    var n = new LiteralValue<string>(m.Span);
                    n.Value = $"{n.Value}.{mixinNode.CompositionPath}";
                    memberDecorate.Decoration = new(memberDecorate.Decoration.Value, n.Words);
                    n.Dispose();
                }
            }
        }

        // Build ShaderInfo
        var shaderInfo = new ShaderInfo(mixinNode.Shaders.Count, shaderClass.ClassName, shaderStart, buffer.Count);
        foreach (var structType in structTypes)
            shaderInfo.StructTypes.Add(structType.Key, structType.Value);
        shaderInfo.CompositionPath = mixinNode.CompositionPath;
        if (mixinNode.Stage != null && mixinNode.Stage.ShadersByName.TryGetValue(shaderClass.ClassName, out var stageShaderInfo))
            shaderInfo.Stage = stageShaderInfo;

        return shaderInfo;
    }

    private static void BuildTypesAndMethodGroups(MixinGlobalContext globalContext, SpirvContext context, SymbolTable table, NewSpirvBuffer temp, MixinNode mixinNode)
    {
        // Setup types in context
        foreach (var type in globalContext.Types)
        {
            // Ignore ShaderSymbol which are not fully loaded (they are likely just OpSDSLImportShader)
            if (type.Value is ShaderSymbol && type.Value is not LoadedShaderSymbol)
                continue;
            if (!context.ReverseTypes.ContainsKey(type.Key))
            {
                context.Types.Add(type.Value, type.Key);
                context.ReverseTypes.Add(type.Key, type.Value);
            }
        }

        // Add symbol for each method in current type (equivalent to implicit this pointer)
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                var functionName = globalContext.Names[function.ResultId];
                var symbol = new Symbol(new(functionName, SymbolKind.Method), globalContext.Types[function.FunctionType], function.ResultId);
                table.CurrentFrame.Add(functionName, symbol);
            }
        }

        // Build method group info (override, etc.)
        ShaderInfo? currentShader = null;
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderInstruction)
            {
                //currentShader = mixinNode.ShadersByName[shaderInstruction.ShaderName];
                // TODO: better way to find ShaderInfo
                currentShader = mixinNode.Shaders.First(x => index >= x.StartInstruction && index < x.EndInstruction);
            }
            else if (i.Data.Op == Op.OpSDSLShaderEnd)
            {
                currentShader = null;
            }
            else if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                if (temp[index + 1].Op == Op.OpSDSLFunctionInfo &&
                    (OpSDSLFunctionInfo)temp[index + 1] is { } functionInfo) 
                {
                    var functionName = globalContext.Names[function.ResultId];
                    var functionType = (FunctionType)context.ReverseTypes[function.FunctionType];

                    var methodMixinGroup = mixinNode;
                    if (!mixinNode.IsRoot && (functionInfo.Flags & FunctionFlagsMask.Stage) != 0)
                        methodMixinGroup = methodMixinGroup.Stage;

                    // If OpSDSLFunctionInfo.Parent is coming from a OpSDSLImportFunction, find the real ID
                    if (functionInfo.Parent != 0)
                    {
                        if (globalContext.ExternalFunctions.TryGetValue(functionInfo.Parent, out var parentFunctionInfo))
                        {
                            var shaderName = globalContext.ExternalShaders[parentFunctionInfo.ShaderId];
                            var parentFunctionType = context.ReverseTypes[parentFunctionInfo.FunctionType];
                            functionInfo.Parent = mixinNode.ShadersByName[shaderName].Functions[parentFunctionInfo.Name].First(x => x.Type == parentFunctionType).Id;
                        }
                    }

                    // Check if it has a parent (and if yes, share the MethodGroup)
                    if (!methodMixinGroup.MethodGroups.TryGetValue(functionInfo.Parent, out var methodGroup))
                        methodGroup = new MethodGroup { Name = functionName, FunctionType = functionType };

                    methodGroup.Shader = currentShader;
                    methodGroup.Methods.Add((Shader: currentShader, MethodId: function.ResultId));

                    methodMixinGroup.MethodGroups[function.ResultId] = methodGroup;

                    // Also add lookup by name
                    if (!methodMixinGroup.MethodGroupsByName.TryGetValue((functionName, functionType), out var methodGroups))
                        methodMixinGroup.MethodGroupsByName.Add((functionName, functionType), function.ResultId);

                    // If abstract, let's erase the whole function
                    if ((functionInfo.Flags & FunctionFlagsMask.Abstract) != 0)
                    {
                        while (temp[index].Op != Op.OpFunctionEnd)
                        {
                            SetOpNop(temp[index++].Data.Memory.Span);
                        }

                        SetOpNop(temp[index].Data.Memory.Span);
                    }
                    else
                    {
                        // Remove the OpSDSLFunctionInfo
                        SetOpNop(temp[index + 1].Data.Memory.Span);
                    }
                }
            }
        }
    }

    private static void ExpandForeach(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer, MixinNode mixinNode, int index, OpForeachSDSL @foreach)
    {
        // Find matching ForeachEnd (taking into account nested foreach)
        var depth = 1;
        var endIndex = index;
        while (depth > 0 && ++endIndex < buffer.Count - 1)
        {
            if (buffer[endIndex].Op == Op.OpForeachSDSL)
                depth++;
            else if (buffer[endIndex].Op == Op.OpForeachEndSDSL)
                depth--;
        }
        endIndex++;

        if (depth > 0)
            throw new InvalidOperationException("Could not find end of foreach instruction");

        // Check the variable (both in current mixin node or in stage)
        // TODO: should we register Compositions by ID in the global context instead, to avoid having to check Stage all the time?)
        if (!mixinNode.CompositionArrays.TryGetValue(@foreach.Collection, out var compositions)
            && (mixinNode.Stage == null || !mixinNode.Stage.CompositionArrays.TryGetValue(@foreach.Collection, out compositions)))
            throw new InvalidOperationException($"Could not find compositions for expression [{@foreach.Collection}]");

        // Extract foreach buffer (with the foreach start/end)
        var foreachBuffer = buffer[index..endIndex];
        buffer.RemoveRange(index, endIndex - index, false);

        var foreachBufferCopy = new List<OpData>();
        // Note: Make sure we replace the OpForeachSDSL with a first OpNop, so that if a for() loop works fine and don't miss an instruction without having to do index--
        foreachBufferCopy.Add(new OpData(new OpNop().InstructionMemory));
        for (int j = 0; j < compositions.Length; ++j)
        {
            var idRemapping = new Dictionary<int, int>();

            // Setup variable for iterator access
            var accessChain = new OpAccessChain(0, context.Bound++, @foreach.Collection, [context.CompileConstant(j).Id]);
            foreachBufferCopy.Add(new(accessChain.InstructionMemory));
            idRemapping.Add(@foreach.ResultId, accessChain);

            // Do a first pass to find all IDs (OpBranch might point to OpLabel which are defined further)
            foreach (var i in foreachBuffer[1..^1])
            {
                if (i.IdResult is int result)
                {
                    // Also duplicate name (if any)
                    if (globalContext.Names.TryGetValue(result, out var name))
                        context.AddName(context.Bound, name);
                    idRemapping.Add(result, context.Bound++);
                }
            }
            // Build a buffer with all foreach instructions (with new ids)
            foreach (var i in foreachBuffer[1..^1]) // skip start/end
            {
                var i2 = new OpData(i.Memory.Span);
                // All result ids are remapped to new ids
                SpirvBuilder.RemapIds(idRemapping, ref i2);

                foreachBufferCopy.Add(i2);
            }
        }
        buffer.InsertRange(index, foreachBufferCopy.AsSpan());
        AdjustIndicesAfterAddingInstructions(mixinNode, index, foreachBufferCopy.Count - foreachBuffer.Count);

        foreach (var inst in foreachBuffer)
            inst.Dispose();
    }

    private static void AdjustIndicesAfterAddingInstructions(MixinNode mixinNode, int insertIndex, int insertCount)
    {
        if (mixinNode.StartInstruction > insertIndex)
            mixinNode.StartInstruction += insertCount;
        if (mixinNode.EndInstruction > insertIndex)
            mixinNode.EndInstruction += insertCount;
        foreach (var shader in mixinNode.Shaders)
        {
            if (shader.StartInstruction > insertIndex)
                shader.StartInstruction += insertCount;
            if (shader.EndInstruction > insertIndex)
                shader.EndInstruction += insertCount;
        }
    }

    private static void ProcessMemberAccessAndForeach(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer temp, MixinNode mixinNode)
    {
        var memberAccesses = new Dictionary<int, int>();
        var thisInstructions = new HashSet<int>();
        var baseInstructions = new HashSet<int>();
        var stageInstructions = new HashSet<int>();
        var compositionArrayAccesses = new Dictionary<int, MixinNode>();
        ShaderInfo? currentShader = null;
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = temp[index];
            currentShader = mixinNode.Shaders.Last(x => index >= x.StartInstruction);

            // Apply any OpMemberAccessSDSL remapping
            if (memberAccesses.Count > 0)
                SpirvBuilder.RemapIds(memberAccesses, ref i.Data);

            if (i.Data.Op == Op.OpForeachSDSL && (OpForeachSDSL)i is { } @foreach)
            {
                ExpandForeach(globalContext, context, temp, mixinNode, index, @foreach);
            }
            else if (i.Data.Op == Op.OpThisSDSL && (OpThisSDSL)i is { } thisInstruction)
            {
                thisInstructions.Add(thisInstruction.ResultId);
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpBaseSDSL && (OpBaseSDSL)i is { } baseInstruction)
            {
                baseInstructions.Add(baseInstruction.ResultId);
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpStageSDSL && (OpStageSDSL)i is { } stageInstruction)
            {
                stageInstructions.Add(stageInstruction.ResultId);
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpAccessChain && (OpAccessChain)i is { } accessChain)
            {
                if (mixinNode.CompositionArrays.TryGetValue(accessChain.BaseId, out var compositions)
                    || (mixinNode.Stage != null && mixinNode.Stage.CompositionArrays.TryGetValue(accessChain.BaseId, out compositions)))
                {
                    var compositionIndex = (int)SpirvBuilder.GetConstantValue(accessChain.Values.Elements.Span[0], context.GetBuffer());
                    compositionArrayAccesses.Add(accessChain.ResultId, compositions[compositionIndex]);

                    SetOpNop(i.Data.Memory.Span);
                }
            }
            else if (i.Data.Op == Op.OpMemberAccessSDSL && (OpMemberAccessSDSL)i is { } memberAccess)
            {
                // Find out the proper mixin node (the member instance)
                var isThis = thisInstructions.Contains(memberAccess.Instance);
                var isBase = baseInstructions.Contains(memberAccess.Instance);
                var isStage = stageInstructions.Contains(memberAccess.Instance);
                MixinNode instanceMixinGroup;
                if (isThis || isBase)
                    instanceMixinGroup = mixinNode;
                else if (isStage)
                    instanceMixinGroup = mixinNode.Stage ?? mixinNode;
                else
                {
                    if (!compositionArrayAccesses.TryGetValue(memberAccess.Instance, out instanceMixinGroup)
                        && !mixinNode.Compositions.TryGetValue(memberAccess.Instance, out instanceMixinGroup))
                        throw new InvalidOperationException();
                }

                if (globalContext.ExternalVariables.TryGetValue(memberAccess.Member, out var variable))
                {
                    var shaderName = globalContext.ExternalShaders[variable.ShaderId];

                    var shaderInfo = instanceMixinGroup.ShadersByName[shaderName];
                    if (!shaderInfo.Variables.TryGetValue(variable.Name, out var variableInfo))
                    {
                        // Try as a stage variable
                        if (instanceMixinGroup.Stage != null
                            && instanceMixinGroup.Stage.ShadersByName.TryGetValue(shaderName, out shaderInfo)
                            && shaderInfo.Variables.TryGetValue(variable.Name, out variableInfo))
                        {

                        }
                        else
                        {
                            throw new InvalidOperationException($"External variable {variable.Name} not found");
                        }
                    }
                    memberAccesses.Add(memberAccess.ResultId, variableInfo.Id);
                }
                else if (globalContext.Types[memberAccess.ResultType] is FunctionType functionType)
                {
                    // In case of functions, OpMemberAccessSDSL.Member could either be a OpFunction or a OpImportFunctionSDSL
                    var functionId = memberAccess.Member;
                    if (globalContext.ExternalFunctions.TryGetValue(memberAccess.Member, out var function))
                        // Process member call (composition)
                        functionId = instanceMixinGroup.MethodGroupsByName[(function.Name, functionType)];

                    bool foundInStage = false;
                    if (!instanceMixinGroup.MethodGroups.TryGetValue(functionId, out var methodGroupEntry))
                    {
                        // Try again as a stage method (only if not a base call)
                        if (instanceMixinGroup.Stage == null || !instanceMixinGroup.Stage.MethodGroups.TryGetValue(functionId, out methodGroupEntry))
                            throw new InvalidOperationException($"Can't find method group info for {globalContext.Names[functionId]}");
                        foundInStage = true;
                    }

                    // Process base call
                    if (isBase)
                    {
                        // We currently do not allow calling base stage method from a non-stage method
                        // (if we were to allow them later, we would need to tweak following detection code as ShaderIndex comparison is only valid for items within the same MixinNode)
                        if (foundInStage)
                            throw new InvalidOperationException($"Method {globalContext.Names[functionId]} was found but a base call can't be performed on a stage method from a non-stage method");

                        // Is it a base call? if yes, find the direct parent
                        // Let's find the method in same group just before ours
                        bool baseMethodFound = false;
                        for (int j = methodGroupEntry.Methods.Count - 1; j >= 0; --j)
                        {
                            if (methodGroupEntry.Methods[j].Shader.ShaderIndex < currentShader.ShaderIndex)
                            {
                                functionId = methodGroupEntry.Methods[j].MethodId;
                                baseMethodFound = true;
                                break;
                            }
                        }

                        if (!baseMethodFound)
                            throw new InvalidOperationException($"Can't find a base method for {globalContext.Names[functionId]}");
                    }
                    else
                    {
                        // If not, get the most derived implementation
                        functionId = methodGroupEntry.Methods[^1].MethodId;
                    }

                    memberAccesses.Add(memberAccess.ResultId, functionId);
                }
                else
                {
                    throw new InvalidOperationException($"Member {memberAccess.Member} not found");
                }

                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function && temp[index + 1].Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)temp[index + 1] is { } functionInfo)
            {
                if (!mixinNode.MethodGroups.TryGetValue(function.ResultId, out var methodGroupEntry))
                    throw new InvalidOperationException($"Can't find method group info for {globalContext.Names[function.ResultId]}");
            }
            else if (i.Data.Op == Op.OpFunctionEnd)
            {
                memberAccesses.Clear();
            }

            SpirvBuilder.RemapIds(memberAccesses, ref i.Data);
        }
    }

    private void RenameVariables(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer temp)
    {
        // Collect variables by names
        string? compositionPath = null;
        var shaderNameWithComposition = string.Empty;
        Dictionary<int, string> prefixes = new();
        foreach (var i in temp)
        {
            if (i.Op == Op.OpSDSLComposition && (OpSDSLComposition)i is { } composition)
            {
                compositionPath = composition.CompositionPath;
            }
            else if (i.Op == Op.OpSDSLCompositionEnd)
            {
                compositionPath = null;
            }
            else if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shader)
            {
                shaderNameWithComposition = compositionPath != null
                    ? $"{compositionPath}.{shader.ShaderName}"
                    : shader.ShaderName;
            }
            else if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { Storageclass: Specification.StorageClass.UniformConstant } variable)
            {
                // Note: we don't rename cbuffer as they have been merged and don't belong to a specific shader/composition anymore
                var type = globalContext.Types[variable.ResultType];
                if (type is not ConstantBufferSymbol)
                    prefixes[variable.ResultId] = shaderNameWithComposition;
            }
            else if (i.Op == Op.OpTypeStruct && (OpTypeStruct)i is { } structType)
            {
                prefixes[structType.ResultId] = shaderNameWithComposition;
            }
            else if (i.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                prefixes[function.ResultId] = shaderNameWithComposition;
            }
        }

        // Now, reprocess context with those names
        foreach (var i in context)
        {
            if (i.Op == Op.OpName && (OpName)i is { } name)
            {
                if (prefixes.TryGetValue(name.Target, out var prefix))
                {
                    var updatedName = $"{prefix}.{name.Name}";
                    name.Name = updatedName;

                    // Now, make sure it's all valid HLSL/GLSL characters (this will replace multiple invalid characters with a single underscore)
                    // Otherwise, EffectReflection RawName won't match
                    updatedName = SpirvBuilder.RemoveInvalidCharactersFromSymbol(updatedName);
                    globalContext.Names[name.Target] = updatedName;
                }
            }
        }
    }

    private static void ProcessReflection(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer, MixinNode mixinNode)
    {
        // First, figure out latest used bindings (assume they are filled in order)
        int srvSlot = 0;
        int samplerSlot = 0;
        int cbufferSlot = 0;
        foreach (var resourceBinding in globalContext.Reflection.ResourceBindings)
        {
            switch (resourceBinding)
            {
                case { Class: EffectParameterClass.ShaderResourceView }:
                    srvSlot = resourceBinding.SlotStart + resourceBinding.SlotCount;
                    break;
                case { Class: EffectParameterClass.Sampler }:
                    samplerSlot = resourceBinding.SlotStart + resourceBinding.SlotCount;
                    break;
                case { Class: EffectParameterClass.ConstantBuffer }:
                    cbufferSlot = resourceBinding.SlotStart + resourceBinding.SlotCount;
                    break;
            }
        }

        // TODO: do this once at root level and reuse for child mixin
        Dictionary<int, LinkInfo> linkInfos = new();
        var samplerStates = new Dictionary<int, Graphics.SamplerStateDescription>();
        foreach (var i in context)
        {
            // Fill linkInfos
            if (i.Op == Op.OpDecorateString && (OpDecorateString)i is
                {
                    Target: int t,
                    Decoration:
                    {
                        Value: Decoration.LinkSDSL or Decoration.ResourceGroupSDSL or Decoration.LogicalGroupSDSL,
                        Parameters: { } m
                    }
                } decoration)
            {
                using var n = new LiteralValue<string>(m.Span);
                ref var linkInfo = ref CollectionsMarshal.GetValueRefOrAddDefault(linkInfos, t, out _);
                if (decoration.Decoration.Value == Decoration.LinkSDSL)
                    linkInfo.LinkName = n.Value;
                else if (decoration.Decoration.Value == Decoration.ResourceGroupSDSL)
                    linkInfo.ResourceGroup = n.Value;
                else if (decoration.Decoration.Value == Decoration.LogicalGroupSDSL)
                    linkInfo.LogicalGroup = n.Value;
            }
            else if ((i.Op == Op.OpDecorate || i.Op == Op.OpDecorateString) && (OpDecorate)i is
                {
                    Decoration:
                    {
                        Value: Decoration.SamplerStateFilter or Decoration.SamplerStateAddressU or Decoration.SamplerStateAddressV or Decoration.SamplerStateAddressW
                            or Decoration.SamplerStateMipLODBias or Decoration.SamplerStateMaxAnisotropy or Decoration.SamplerStateComparisonFunc or Decoration.SamplerStateMinLOD or Decoration.SamplerStateMaxLOD,
                        Parameters: { } p
                    }
                } decorate)
            {
                ref var samplerState = ref CollectionsMarshal.GetValueRefOrAddDefault(samplerStates, decorate.Target, out var exists);
                if (!exists)
                    samplerState = Graphics.SamplerStateDescription.Default;
                switch (decorate.Decoration.Value)
                {
                    case Decoration.SamplerStateFilter:
                        samplerState.Filter = (Graphics.TextureFilter)p.Span[0];
                        break;
                    case Decoration.SamplerStateAddressU:
                        samplerState.AddressU = (Graphics.TextureAddressMode)p.Span[0];
                        break;
                    case Decoration.SamplerStateAddressV:
                        samplerState.AddressV = (Graphics.TextureAddressMode)p.Span[0];
                        break;
                    case Decoration.SamplerStateAddressW:
                        samplerState.AddressW = (Graphics.TextureAddressMode)p.Span[0];
                        break;
                    case Decoration.SamplerStateMipLODBias:
                        {
                            using var n = new LiteralValue<string>(p.Span);
                            samplerState.MipMapLevelOfDetailBias = float.Parse(n.Value);
                            break;
                        }
                    case Decoration.SamplerStateMaxAnisotropy:
                        samplerState.MaxAnisotropy = p.Span[0];
                        break;
                    case Decoration.SamplerStateComparisonFunc:
                        samplerState.CompareFunction = (Graphics.CompareFunction)p.Span[0];
                        break;
                    case Decoration.SamplerStateMinLOD:
                        {
                            using var n = new LiteralValue<string>(p.Span);
                            samplerState.MinMipLevel = float.Parse(n.Value);
                            break;
                        }
                    case Decoration.SamplerStateMaxLOD:
                        {
                            using var n = new LiteralValue<string>(p.Span);
                            samplerState.MaxMipLevel = float.Parse(n.Value);
                            break;
                        }
                }
            }
        }

        string currentShaderName = string.Empty;
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = buffer[index];

            if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shader)
            {
                currentShaderName = shader.ShaderName;
            }
            else if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable)
            {
                var type = context.ReverseTypes[variable.ResultType];
                if (type is PointerType pointerType)
                {
                    var name = globalContext.Names[variable.ResultId];
                    linkInfos.TryGetValue(variable.ResultId, out var linkInfo);
                    var linkName = linkInfo.LinkName ?? $"{TypeName.GetTypeNameWithoutGenerics(currentShaderName)}.{name}";
                    if (mixinNode.CompositionPath != null)
                        linkName = $"{linkName}.{mixinNode.CompositionPath}";

                    var effectResourceBinding = new EffectResourceBindingDescription
                    {
                        KeyInfo = new EffectParameterKeyInfo { KeyName = linkName },
                        ElementType = default,
                        RawName = name,
                        ResourceGroup = linkInfo.ResourceGroup,
                        //Stage = , // filed by ShaderCompiler
                        LogicalGroup = linkInfo.LogicalGroup,
                    };

                    if (pointerType.BaseType is TextureType t)
                    {
                        var slot = globalContext.Reflection.ResourceBindings.Count;
                        globalContext.Reflection.ResourceBindings.Add(effectResourceBinding with
                        {
                            Class = EffectParameterClass.ShaderResourceView,
                            Type = (t, t.Multisampled) switch
                            {
                                (Texture1DType, false) => EffectParameterType.Texture1D,
                                (Texture2DType, false) => EffectParameterType.Texture2D,
                                (Texture2DType, true) => EffectParameterType.Texture2DMultisampled,
                                (Texture3DType, false) => EffectParameterType.Texture3D,
                                (TextureCubeType, false) => EffectParameterType.TextureCube,
                            },
                            SlotStart = srvSlot,
                            SlotCount = 1,
                        });

                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationDescriptorSet(0)));
                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationBinding(srvSlot)));

                        srvSlot++;
                    }
                    else if (pointerType.BaseType is BufferType)
                    {
                        var slot = globalContext.Reflection.ResourceBindings.Count;
                        globalContext.Reflection.ResourceBindings.Add(effectResourceBinding with
                        {
                            Class = EffectParameterClass.ShaderResourceView,
                            Type = EffectParameterType.Buffer,
                            SlotStart = srvSlot,
                            SlotCount = 1,
                        });

                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationDescriptorSet(0)));
                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationBinding(srvSlot)));

                        srvSlot++;
                    }
                    else if (pointerType.BaseType is SamplerType)
                    {
                        globalContext.Reflection.ResourceBindings.Add(effectResourceBinding with
                        {
                            Class = EffectParameterClass.Sampler,
                            Type = EffectParameterType.Sampler,
                            SlotStart = samplerSlot,
                            SlotCount = 1,
                        });

                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationDescriptorSet(0)));
                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationBinding(samplerSlot)));

                        if (samplerStates.TryGetValue(variable.ResultId, out var samplerState))
                            globalContext.Reflection.SamplerStates.Add(new EffectSamplerStateBinding(linkName, samplerState));

                        samplerSlot++;
                    }
                    else if (pointerType.BaseType is ConstantBufferSymbol)
                    {
                        globalContext.Reflection.ResourceBindings.Add(effectResourceBinding with
                        {
                            Class = EffectParameterClass.ConstantBuffer,
                            Type = EffectParameterType.ConstantBuffer,
                            SlotStart = cbufferSlot,
                            SlotCount = 1,
                            // TODO: Special case, Stride EffectCompiler.CleanupReflection() expect a different format here (let's fix that later in Stride)
                            //       Anyway, since buffer is merged, KeyName with form ShaderName.VariableName doesn't make sense as it doesn't belong to a specific shader anymore
                            KeyInfo = new EffectParameterKeyInfo { KeyName = name },
                            ResourceGroup = name,
                        });

                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationDescriptorSet(0)));
                        context.Add(new OpDecorate(variable.ResultId, ParameterizedFlags.DecorationBinding(cbufferSlot)));

                        cbufferSlot++;
                    }
                }
            }
        }

        // Process compositions recursively
        foreach (var composition in mixinNode.Compositions)
        {
            ProcessReflection(globalContext, context, buffer, composition.Value);
        }
        foreach (var compositionArray in mixinNode.CompositionArrays)
        {
            foreach (var composition in compositionArray.Value)
            {
                ProcessReflection(globalContext, context, buffer, composition);
            }
        }
    }


    static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }

    public static void OffsetIds(OpData inst, int offset)
    {
        foreach (var o in inst)
        {
            if (o.Kind == OperandKind.IdRef
                || o.Kind == OperandKind.IdResult
                || o.Kind == OperandKind.IdResultType)
            {
                for (int i = 0; i < o.Words.Length; ++i)
                {
                    if (o.Words[i] != 0)
                        o.Words[i] += offset;
                }
            }
            else if (o.Kind == OperandKind.PairIdRefLiteralInteger
                     || o.Kind == OperandKind.PairLiteralIntegerIdRef
                     || o.Kind == OperandKind.PairIdRefIdRef)
            {
                for (int i = 0; i < o.Words.Length; i += 2)
                {
                    if (o.Kind == OperandKind.PairIdRefLiteralInteger || o.Kind == OperandKind.PairIdRefIdRef)
                    {
                        if (o.Words[i + 0] != 0)
                            o.Words[i + 0] += offset;
                    }

                    if (o.Kind == OperandKind.PairLiteralIntegerIdRef || o.Kind == OperandKind.PairIdRefIdRef)
                    {
                        if (o.Words[i + 1] != 0)
                            o.Words[i + 1] += offset;
                    }
                }
            }
        }
    }

    private static void CleanupUnnecessaryInstructions(MixinGlobalContext globalContext, NewSpirvBuffer temp)
    {
        for (int index = 0; index < temp.Count; index++)
        {
            var i = temp[index];

            // Transform OpVariableSDSL into OpVariable (we don't need extra info anymore)
            // Note: we ignore initializer as we store a method which is already processed during StreamAnalyzer (as opposed to a const for OpVariable)
            if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable)
                temp.Replace(index, new OpVariable(variable.ResultType, variable.ResultId, variable.Storageclass, null));

            // Transform OpTypeFunctionSDSL into OpTypeFunction (we don't need extra info anymore)
            if (i.Op == Op.OpTypeFunctionSDSL && (OpTypeFunctionSDSL)i is { } functionType)
            {
                Span<int> parameterTypes = stackalloc int[functionType.Values.Elements.Span.Length];
                for (int j = 0; j < functionType.Values.Elements.Span.Length; ++j)
                    parameterTypes[j] = functionType.Values.Elements.Span[j].Item1;
                temp.Replace(index, new OpTypeFunction(functionType.ResultId, functionType.ReturnType, [..parameterTypes]));
            }

            // Remove Nop
            if (i.Op == Op.OpNop)
                temp.RemoveAt(index--);
            // Also remove some other SDSL specific operators (that we keep late mostly for debug purposes)
            else if (i.Op == Op.OpSDSLShader
                || i.Op == Op.OpSDSLShaderEnd
                || i.Op == Op.OpSDSLComposition
                || i.Op == Op.OpSDSLCompositionEnd
                || i.Op == Op.OpSDSLMixinInherit
                || i.Op == Op.OpConstantStringSDSL
                || i.Op == Op.OpTypeGenericSDSL
                || i.Op == Op.OpSDSLImportShader
                || i.Op == Op.OpSDSLImportFunction
                || i.Op == Op.OpSDSLImportVariable)
                temp.RemoveAt(index--);
            else if ((i.Op == Op.OpDecorate || i.Op == Op.OpDecorateString) && ((OpDecorate)i).Decoration.Value is
                    Decoration.LinkIdSDSL or Decoration.LinkSDSL or Decoration.LogicalGroupSDSL or Decoration.ResourceGroupSDSL or Decoration.ResourceGroupIdSDSL
                    or Decoration.SamplerStateFilter or Decoration.SamplerStateAddressU or Decoration.SamplerStateAddressV or Decoration.SamplerStateAddressW
                    or Decoration.SamplerStateMipLODBias or Decoration.SamplerStateMaxAnisotropy or Decoration.SamplerStateComparisonFunc or Decoration.SamplerStateMinLOD or Decoration.SamplerStateMaxLOD)
                temp.RemoveAt(index--);
            else if ((i.Op == Op.OpMemberDecorate || i.Op == Op.OpMemberDecorateString) && ((OpMemberDecorate)i).Decoration.Value is Decoration.LinkIdSDSL or Decoration.LinkSDSL or Decoration.LogicalGroupSDSL or Decoration.ResourceGroupSDSL)
                temp.RemoveAt(index--);

            // Remove SPIR-V about pointer types to other shaders (variable and types themselves are removed as well)
            else if (i.Op == Op.OpTypePointer && (OpTypePointer)i is { } typePointer)
            {
                var pointedType = globalContext.Types[typePointer.Type];
                if (pointedType is ShaderSymbol || pointedType is ArrayType { BaseType: ShaderSymbol })
                    temp.RemoveAt(index--);
            }
            // Also remove arrays of shaders (used in composition arrays)
            else if (i.Op == Op.OpTypeArray && (OpTypeArray)i is { } typeArray)
            {
                var innerType = globalContext.Types[typeArray.ElementType];
                if (innerType is ShaderSymbol)
                    temp.RemoveAt(index--);
            }
            else if (i.Op == Op.OpTypeRuntimeArray && (OpTypeRuntimeArray)i is { } typeRuntimeArray)
            {
                var innerType = globalContext.Types[typeRuntimeArray.ElementType];
                if (innerType is ShaderSymbol)
                    temp.RemoveAt(index--);
            }
        }
    }
}