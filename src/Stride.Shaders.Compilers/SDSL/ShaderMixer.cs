using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Stride.Core.Storage;
using static Stride.Shaders.Spirv.Specification;
using EntryPoint = Stride.Shaders.Core.EntryPoint;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer(IExternalShaderLoader shaderLoader)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ResourcesRegisterSeparate">For D3D11/12: t, b and s registers are separate (and should be kept as low as possible so we number them from 0 in each category).</param>
    public record struct Options(bool ResourcesRegisterSeparate);
    
    public IExternalShaderLoader ShaderLoader { get; } = shaderLoader;
    
    public void MergeSDSL(ShaderSource shaderSource, Options options, out Span<byte> bytecode, out EffectReflection effectReflection, out HashSourceCollection usedHashSources, out List<(string Name, int Id, ShaderStage Stage)> entryPoints)
    {
        var temp = new NewSpirvBuffer();

        var context = new SpirvContext();
        var shaderLoader = new CaptureLoadedShaders(ShaderLoader);
        var table = new SymbolTable(context) { ShaderLoader = shaderLoader };

        var effectEvaluator = new EffectEvaluator(shaderLoader);
        shaderSource = effectEvaluator.EvaluateEffects(shaderSource);

        var shaderSource2 = EvaluateInheritanceAndCompositions(shaderLoader, context, shaderSource);

        // Root shader
        var globalContext = new MixinGlobalContext();

        // Process name and types imported by constants due to generics instantiation
        ShaderClass.ProcessNameAndTypes(context);

        var rootMixin = MergeMixinNode(globalContext, context, table, temp, shaderSource2);
        
        context.Insert(0, new OpCapability(Capability.Shader));
        context.Insert(1, new OpCapability(Capability.SampledBuffer));
        context.Insert(2, new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        
        // Process streams and remove unused code/cbuffer/variable/resources
        var interfaceProcessor = new InterfaceProcessor
        {
            CodeInserted = (int index, int count) => AdjustIndicesAfterAppendInstructions(rootMixin, index, count)
        };
        (entryPoints, globalContext.Reflection.InputAttributes) = interfaceProcessor.Process(table, temp, context);
        
        // Process Link (add CompositionPath, generate missing ones, etc.)
        ProcessLinks(context, temp);

        // Any non-static variable is moved to a "Globals" default cbuffer
        // TODO: future language improvement:
        //       force cbuffer to be epxlicit? (and not need "static" anymore for mixin nodes member, which is weird)
        //       It's a breaking change and will require some changes to Stride shaders (esp. in post effects) 
        GenerateDefaultCBuffer(rootMixin, globalContext, context, temp);
        
        // Merge cbuffers and rgroups
        MergeCBuffers(globalContext, context, temp);
        ComputeCBufferReflection(globalContext, context, temp);

        // Try to give variables more sensible names
        // Note: since we mutate OpName and globalContext.Names, try to do that as late as possible because some code earlier use names to match variables/types
        RenameVariables(globalContext, context, temp);

        // Process reflection
        ProcessReflection(globalContext, context, temp, options);

        SimplifyNotSupportedConstantsInShader(context, temp);
        
        foreach (var inst in context)
            temp.Add(inst.Data);

        CleanupUnnecessaryInstructions(globalContext, context, temp);

        temp.Sort();

        bytecode = SpirvBytecode.CreateBytecodeFromBuffers(temp);

        effectReflection = globalContext.Reflection;
        usedHashSources = shaderLoader.Sources;
    }

    class MixinGlobalContext
    {
        public EffectReflection Reflection { get; } = new();

        public Dictionary<int, string> ExternalShaders { get; } = new();
        public Dictionary<int, (int ShaderId, string Name, int FunctionType)> ExternalFunctions { get; } = new();
        public Dictionary<int, (int ShaderId, string Name)> ExternalVariables { get; } = new();
    }

    class MixinNodeContext
    {
        public MixinNode? Result { get; }
    }

    MixinNode MergeMixinNode(MixinGlobalContext globalContext, SpirvContext context, SymbolTable table, NewSpirvBuffer buffer, ShaderMixinInstantiation mixinSource, MixinNode? stage = null, string? currentCompositionPath = null)
    {
        // We emit OPSDSLEffect for any non-root composition
        if (currentCompositionPath != null)
            buffer.Add(new OpSDSLComposition(currentCompositionPath));

        var mixinNode = new MixinNode(stage, currentCompositionPath);
        var contextStart = context.Count;

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
                        var localKey = variable.Key;
                        if (isCompositionArray)
                            localKey += $"[{i}]";
                        // TODO: Review: it seems like Stride compose variable the opposite way that we expect
                        //       Let's change it so that it becomes {currentCompositionPath}.{localKey}!
                        var compositionPath = currentCompositionPath != null ? $"{localKey}.{currentCompositionPath}" : localKey;
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
        var typeDuplicateInserter = new TypeDuplicateHelper(context);

        foreach (var shaderClass in mixinSource.Mixins)
        {
            var contextStart = context.Count;

            var shaderInfo = MergeClassInBuffers(globalContext, context, buffer, mixinNode, shaderClass, typeDuplicateInserter);

            mixinNode.ShadersByName.Add(shaderClass.ToClassNameWithGenerics(), shaderInfo);
            mixinNode.Shaders.Add(shaderInfo);

            // Note: we process name, types and struct right away, as they might be needed by the next Shader
            ShaderClass.ProcessNameAndTypes(context, contextStart, context.Count);
            PopulateShaderInfo(globalContext, context, contextStart, context.Count, buffer, shaderInfo.StartInstruction, shaderInfo.EndInstruction, shaderInfo, mixinNode);
        }

        mixinNode.EndInstruction = buffer.Count;
    }

    private static string GenerateLinkName(string shaderName, string variableName)
    {
        return $"{TypeName.GetTypeNameWithoutGenerics(shaderName)}.{variableName}";
    }

    private static string ComposeLinkName(string linkName, string? compositionPath = null)
    {
        if (compositionPath != null)
            linkName += $".{compositionPath}";
        return linkName;
    }
    
    // Append CompositionPath to "Link" for any non-stage variable
    // Also force-emit the missing "Link" decorations

    private ShaderInfo MergeClassInBuffers(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer, MixinNode mixinNode, ShaderClassInstantiation shaderClass, TypeDuplicateHelper typeDuplicateInserter)
    {
        var isRootMixin = mixinNode.Stage == null;
        if (shaderClass.ImportStageOnly)
        {
            if (!isRootMixin)
                throw new InvalidOperationException("importing stage-only methods/variables is only possible at the root mixin");
        }

        var shaderBuffers = shaderClass.Buffer.Value;
        var offset = context.Bound;
        var resourceGroupOffset = context.ResourceGroupBound;

        // Remember when we started to add instructions in both context and main buffer
        var shaderStart = buffer.Count;
        var contextStart = context.Count;
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
                var stageShader = mixinNode.Stage.ShadersByName[shaderClass.ToClassNameWithGenerics()];
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

        var structTypes = new Dictionary<string, int>();

        // Copy instructions to main buffer
        foreach (var shader in new[] { shaderBuffers.Context.GetBuffer(), shaderBuffers.Buffer })
        {
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
                if (i2.Op == Op.OpDecorate && new OpDecorate(ref i2) is { Decoration: Decoration.ResourceGroupIdSDSL, DecorationParameters: { } m } resourceGroupIdDecorate)
                {
                    // Somehow data doesn't get mutated inside i2 if we update resourceGroupIdDecorate.Decoration, so we reference buffer directly
                    resourceGroupIdDecorate.DecorationParameters = [m.Span[0] + resourceGroupOffset];
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
                if (TypeDuplicateHelper.OpCheckDuplicateForTypesAndImport(i2.Op))
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
                                remapIds.Add(id, existingInstruction.Data.IdResult.Value);
                                removedIds.Add(existingInstruction.Data.IdResult.Value);
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
                ProcessImportInfo(globalContext, mixinNode, ref i2, context);

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
                        typeDuplicateInserter.InsertInstruction(context.Count, i2);
                    }
                }
            }
        }

        // Reprocess OpName/OpDecorate (they are defined before the OpType that was remapped, so we need to reprocess them)
        for (int index = contextStart; index < context.Count; ++index)
        {
            var i = context[index];

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

        // Build ShaderInfo
        var shaderInfo = new ShaderInfo(mixinNode.Shaders.Count, shaderClass.ClassName, shaderStart, buffer.Count);
        shaderInfo.Symbol = shaderClass.Symbol;
        foreach (var structType in structTypes)
            shaderInfo.StructTypes.Add(structType.Key, structType.Value);
        shaderInfo.CompositionPath = mixinNode.CompositionPath;
        if (mixinNode.Stage != null && mixinNode.Stage.ShadersByName.TryGetValue(shaderClass.ClassName, out var stageShaderInfo))
            shaderInfo.Stage = stageShaderInfo;

        return shaderInfo;
    }

    private static void BuildTypesAndMethodGroups(MixinGlobalContext globalContext, SpirvContext context, SymbolTable table, NewSpirvBuffer temp, MixinNode mixinNode)
    {
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
                    var functionName = context.Names[function.ResultId];
                    var functionType = (FunctionType)context.ReverseTypes[function.FunctionType];
                    
                    // Add symbol for each method in current type (equivalent to implicit this pointer)
                    var symbol = new Symbol(new(functionName, SymbolKind.Method), context.ReverseTypes[function.FunctionType], function.ResultId, OwnerType: currentShader.Symbol);
                    table.CurrentFrame.Add(functionName, symbol);

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
                    methodGroup.Methods.Add((Shader: currentShader, MethodId: function.ResultId, Flags: functionInfo.Flags));

                    methodMixinGroup.MethodGroups[function.ResultId] = methodGroup;

                    // Also add lookup by name
                    if (!methodMixinGroup.MethodGroupsByName.TryGetValue(new(functionName, functionType), out var methodGroups))
                        methodMixinGroup.MethodGroupsByName.Add(new(functionName, functionType), function.ResultId);

                    // If abstract, let's erase the whole function
                    if ((functionInfo.Flags & FunctionFlagsMask.Abstract) != 0)
                    {
                        var removedIds = new HashSet<int>();
                        while (temp[index].Op != Op.OpFunctionEnd)
                        {
                            if (temp[index].Data.IdResult is {} idResult)
                                removedIds.Add(idResult);
                            SetOpNop(temp[index++].Data.Memory.Span);
                        }
                        context.RemoveNameAndDecorations(removedIds);

                        SetOpNop(temp[index].Data.Memory.Span);
                    }
                    else
                    {
                        // Remove the OpSDSLFunctionInfo
                        //SetOpNop(temp[index + 1].Data.Memory.Span);
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
        buffer.RemoveRange(index, foreachBuffer.Count, false);

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
                    if (context.Names.TryGetValue(result, out var name))
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
        
        // Clean OpName used by removed instructions (only for IdResult)
        var removedIds = new HashSet<int>();
        foreach (var i in foreachBuffer)
            if (i.IdResult is { } idResult)
                removedIds.Add(idResult);
        context.RemoveNameAndDecorations(removedIds);
        
        // Insert new code
        buffer.InsertRange(index, foreachBufferCopy.AsSpan());
        
        // Note: mixinNode is not added to rootMixin hierarchy yet
        //       Moreover, we are the last mixin (or one of our child is)
        //       So we need (and it's safe) to call this on mixinNode rather than root node
        AdjustIndicesAfterAppendInstructions(mixinNode, index, foreachBufferCopy.Count - foreachBuffer.Count);

        foreach (var inst in foreachBuffer)
            inst.Dispose();
    }

    // Note: Make sure to call it on propre node (i.e. either last mixin node (if just added) or root, otherwise it won't increment the MixinNode after current one 
    //       If added between two mixin, it will belong to the one before (as if appending)
    //       it also means adding before or at the start the first (root) mixin is forbidden
    private static void AdjustIndicesAfterAppendInstructions(MixinNode rootMixin, int insertIndex, int insertCount)
    {
        // Check bounds: we can't add before or at start of first mixin
        if (insertIndex <= rootMixin.StartInstruction)
            throw new ArgumentOutOfRangeException(nameof(insertIndex));
        
        // Nothing to shift
        if (insertCount == 0)
            return;

        AdjustIndicesAfterAppendInstructionsInner(rootMixin, insertIndex, insertCount);
        
        static void AdjustIndicesAfterAppendInstructionsInner(MixinNode mixinNode, int insertIndex, int insertCount)
        {
            if (mixinNode.StartInstruction > insertIndex)
                mixinNode.StartInstruction += insertCount;
            if (mixinNode.EndInstruction >= insertIndex)
                mixinNode.EndInstruction += insertCount;
            foreach (var shader in mixinNode.Shaders)
            {
                if (shader.StartInstruction > insertIndex)
                    shader.StartInstruction += insertCount;
                if (shader.EndInstruction >= insertIndex)
                    shader.EndInstruction += insertCount;
            }

            foreach (var composition in mixinNode.Compositions)
                AdjustIndicesAfterAppendInstructionsInner(composition.Value, insertIndex, insertCount);
            foreach (var compositions in mixinNode.CompositionArrays)
                foreach (var composition in compositions.Value)
                    AdjustIndicesAfterAppendInstructionsInner(composition, insertIndex, insertCount);
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
                    var compositionIndex = (int)context.GetConstantValue(accessChain.Values.Elements.Span[0]);
                    compositionArrayAccesses.Add(accessChain.ResultId, compositions[compositionIndex]);

                    SetOpNop(i.Data.Memory.Span);
                }
            }
            else if (i.Data.Op == Op.OpMemberAccessSDSL && (OpMemberAccessSDSL)i is { } memberAccess)
            {
                // Find out the proper mixin node (the member instance)
                var isThis = thisInstructions.Contains(memberAccess.Instance);
                var isBase = baseInstructions.Contains(memberAccess.Instance);
                MixinNode instanceMixinGroup;
                if (isThis || isBase)
                    instanceMixinGroup = mixinNode;
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
                        if (!(instanceMixinGroup.Stage != null
                            && instanceMixinGroup.Stage.ShadersByName.TryGetValue(shaderName, out shaderInfo)
                            && shaderInfo.Variables.TryGetValue(variable.Name, out variableInfo)))
                        {
                            throw new InvalidOperationException($"External variable {variable.Name} not found");
                        }
                    }
                    memberAccesses.Add(memberAccess.ResultId, variableInfo.Id);
                }
                else if (context.ReverseTypes[memberAccess.ResultType] is FunctionType functionType)
                {
                    // In case of functions, OpMemberAccessSDSL.Member could either be a OpFunction or a OpImportFunctionSDSL
                    var functionId = memberAccess.Member;
                    if (globalContext.ExternalFunctions.TryGetValue(memberAccess.Member, out var function))
                    {
                        // Process member call (composition)
                        if (!instanceMixinGroup.MethodGroupsByName.TryGetValue((function.Name, functionType), out functionId)
                            && (instanceMixinGroup.Stage == null || !instanceMixinGroup.Stage.MethodGroupsByName.TryGetValue((function.Name, functionType), out functionId)))
                            throw new InvalidOperationException($"Can't find function ID for {context.Names[functionId]}");
                    }

                    bool foundInStage = false;
                    if (!instanceMixinGroup.MethodGroups.TryGetValue(functionId, out var methodGroupEntry))
                    {
                        // Try again as a stage method (only if not a base call)
                        if (instanceMixinGroup.Stage == null || !instanceMixinGroup.Stage.MethodGroups.TryGetValue(functionId, out methodGroupEntry))
                            throw new InvalidOperationException($"Can't find method group info for {context.Names[functionId]}");
                        foundInStage = true;
                    }

                    // Default: most derived implementation
                    var selectedMethod = methodGroupEntry.Methods[^1];

                    // Process base call
                    if (isBase)
                    {
                        // We currently do not allow calling base stage method from a non-stage method
                        // (if we were to allow them later, we would need to tweak following detection code as ShaderIndex comparison is only valid for items within the same MixinNode)
                        if (foundInStage)
                            throw new InvalidOperationException($"Method {context.Names[functionId]} was found but a base call can't be performed on a stage method from a non-stage method");

                        // Is it a base call? if yes, find the direct parent
                        // Let's find the method in same group just before ours
                        bool baseMethodFound = false;
                        for (int j = methodGroupEntry.Methods.Count - 1; j >= 0; --j)
                        {
                            if (methodGroupEntry.Methods[j].Shader.ShaderIndex < currentShader.ShaderIndex)
                            {
                                selectedMethod = methodGroupEntry.Methods[j];
                                baseMethodFound = true;
                                break;
                            }
                        }

                        if (!baseMethodFound)
                            throw new InvalidOperationException($"Can't find a base method for {context.Names[functionId]}");
                    }

                    if ((selectedMethod.Flags & FunctionFlagsMask.Abstract) != 0)
                        throw new InvalidOperationException($"Trying to call an abstract method {selectedMethod.Shader.ShaderName}.{context.Names[functionId]}");
                    functionId = selectedMethod.MethodId;

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
                    throw new InvalidOperationException($"Can't find method group info for {context.Names[function.ResultId]}");
            }
            else if (i.Data.Op == Op.OpFunctionEnd)
            {
                memberAccesses.Clear();
            }

            SpirvBuilder.RemapIds(memberAccesses, ref i.Data);
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
                || o.Kind == OperandKind.IdResultType
                || o.Kind == OperandKind.IdScope
                || o.Kind == OperandKind.IdMemorySemantics
                || o.Kind == OperandKind.PairIdRefIdRef)
            {
                for (int i = 0; i < o.Words.Length; ++i)
                {
                    if (o.Words[i] != 0)
                        o.Words[i] += offset;
                }
            }
            else if (o.Kind == OperandKind.PairIdRefLiteralInteger
                     || o.Kind == OperandKind.PairLiteralIntegerIdRef)
            {
                for (int i = 0; i < o.Words.Length; i += 2)
                {
                    if (o.Kind == OperandKind.PairIdRefLiteralInteger)
                    {
                        if (o.Words[i + 0] != 0)
                            o.Words[i + 0] += offset;
                    }

                    if (o.Kind == OperandKind.PairLiteralIntegerIdRef)
                    {
                        if (o.Words[i + 1] != 0)
                            o.Words[i + 1] += offset;
                    }
                }
            }
        }
    }
    
    private void SimplifyNotSupportedConstantsInShader(SpirvContext context, NewSpirvBuffer temp)
    {
        foreach (var i in context)
        {
            if (i.Op == Op.OpSpecConstantOp && (OpSpecConstantOp)i is { } specConstantOp)
            {
                if (!ExpressionExtensions.ShaderSpecConstantOpSupportedOps.Contains((Op)specConstantOp.Opcode))
                {
                    // Simplify the constant
                    context.TryGetConstantValue(i, out _, out _, true);
                }
            }
        }
    }

    private static void RemoveInstructionWhere(NewSpirvBuffer buffer, Func<OpDataIndex, bool> match)
    {
        int insertIndex = 0;
        for (int sourceIndex = 0; sourceIndex < buffer.Count; sourceIndex++)
        {
            var i = buffer[sourceIndex];
            var remove = match(i);

            if (!remove)
            {
                if (insertIndex++ != sourceIndex)
                    // Note: we're not using Dispose() since we simply move it
                    buffer.Replace(insertIndex - 1, buffer[sourceIndex].Data, false);
            }
            else
            {
                buffer[sourceIndex].Data.Dispose();
            }
        }
        
        // Remove leftover instructions (they have been either disposed or moved so no need to dispose them)
        buffer.RemoveRange(insertIndex, buffer.Count - insertIndex, false);
    }

    private static void CleanupUnnecessaryInstructions(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer temp)
    {
        // Remove in a single pass (we do in-place without RemoveAt otherwise it would be up to O(n^2) complexity)
        RemoveInstructionWhere(temp, i =>
        {
            // Remove Nop
            if (i.Op == Op.OpNop)
                return true;
            // Also remove some other SDSL specific operators (that we keep late mostly for debug purposes)
            if (i.Op == Op.OpSDSLShader
                || i.Op == Op.OpSDSLShaderEnd
                || i.Op == Op.OpSDSLComposition
                || i.Op == Op.OpSDSLCompositionEnd
                || i.Op == Op.OpSDSLMixinInherit
                || i.Op == Op.OpConstantStringSDSL
                || i.Op == Op.OpTypeGenericSDSL
                || i.Op == Op.OpSDSLImportShader
                || i.Op == Op.OpSDSLImportFunction
                || i.Op == Op.OpSDSLImportVariable
                || i.Op == Op.OpSDSLFunctionInfo)
                return true;
            if ((i.Op == Op.OpDecorate || i.Op == Op.OpDecorateString) && ((OpDecorate)i).Decoration is
                    Decoration.FunctionParameterDefaultValueSDSL
                    or Decoration.ShaderConstantSDSL
                    or Decoration.LinkIdSDSL or Decoration.LinkSDSL or Decoration.LogicalGroupSDSL or Decoration.ResourceGroupSDSL or Decoration.ResourceGroupIdSDSL
                    or Decoration.SamplerStateFilter or Decoration.SamplerStateAddressU or Decoration.SamplerStateAddressV or Decoration.SamplerStateAddressW
                    or Decoration.SamplerStateMipLODBias or Decoration.SamplerStateMaxAnisotropy or Decoration.SamplerStateComparisonFunc or Decoration.SamplerStateMinLOD or Decoration.SamplerStateMaxLOD)
                return true;
            if ((i.Op == Op.OpMemberDecorate || i.Op == Op.OpMemberDecorateString) && ((OpMemberDecorate)i).Decoration is Decoration.LinkIdSDSL or Decoration.LinkSDSL or Decoration.LogicalGroupSDSL or Decoration.ResourceGroupSDSL)
                return true;

            // Remove SPIR-V about pointer types to other shaders (variable and types themselves are removed as well)
            if (i.Op == Op.OpTypePointer && (OpTypePointer)i is { } typePointer)
            {
                var pointedType = context.ReverseTypes[typePointer.Type];
                if (pointedType is ShaderSymbol || pointedType is ArrayType { BaseType: ShaderSymbol })
                    return true;
            }
            // Also remove arrays of shaders (used in composition arrays)
            else if (i.Op == Op.OpTypeArray && (OpTypeArray)i is { } typeArray)
            {
                var innerType = context.ReverseTypes[typeArray.ElementType];
                if (innerType is ShaderSymbol)
                    return true;
            }
            else if (i.Op == Op.OpTypeRuntimeArray && (OpTypeRuntimeArray)i is { } typeRuntimeArray)
            {
                var innerType = context.ReverseTypes[typeRuntimeArray.ElementType];
                if (innerType is ShaderSymbol)
                    return true;
            }
            return false;
        });
        
        var ids = new HashSet<int>();
        foreach (var i in temp)
        {
            // Transform OpVariableSDSL into OpVariable (we don't need extra info anymore)
            // Note: we ignore initializer as we store a method which is already processed during InterfaceProcessor (as opposed to a const for OpVariable)
            if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable)
                temp.Replace(i.Index, new OpVariable(variable.ResultType, variable.ResultId, variable.Storageclass, null));

            // Transform OpTypeFunctionSDSL into OpTypeFunction (we don't need extra info anymore)
            if (i.Op == Op.OpTypeFunctionSDSL && (OpTypeFunctionSDSL)i is { } functionType)
            {
                Span<int> parameterTypes = stackalloc int[functionType.Values.Elements.Span.Length];
                for (int j = 0; j < functionType.Values.Elements.Span.Length; ++j)
                    parameterTypes[j] = functionType.Values.Elements.Span[j].Item1;
                temp.Replace(i.Index, new OpTypeFunction(functionType.ResultId, functionType.ReturnType, [..parameterTypes]));
            }
            
            // Collect IDs (except for OpName/OpDecorate/OpDecorateString metadata)
            if (i.Op != Op.OpName && i.Op != Op.OpDecorate && i.Op != Op.OpDecorateString)
                SpirvBuilder.CollectIds(i.Data, ids);
        }

        // Remove unnecessary OpName/OpDecorate/OpDecorateString
        // Note: we should issue a warning and make sure those are deleted as we process stuff?
        RemoveInstructionWhere(temp, i =>
        {
            if (i.Op == Op.OpName && (OpName)i is {} nameInstruction)
            {
                if (!ids.Contains(nameInstruction.Target))
                    return true;
            }
            if (i.Op == Op.OpDecorate && (OpDecorate)i is {} decorate)
            {
                if (!ids.Contains(decorate.Target))
                    return true;
            }
            if (i.Op == Op.OpDecorate && (OpDecorateString)i is {} decorateString)
            {
                if (!ids.Contains(decorateString.Target))
                    return true;
            }

            return false;
        });
    }
}

public class CaptureLoadedShaders(IExternalShaderLoader inner) : IExternalShaderLoader
{
    /// <summary>
    /// Cache per file.
    /// </summary>
    /// <remarks>Expects hash to be stored.</remarks>
    public IShaderCache FileCache => inner.FileCache;
    /// <summary>
    /// Cache per generic instantiation.
    /// </summary>
    /// <remarks>Hashes are not needed.</remarks>
    public IShaderCache GenericCache => inner.GenericCache;

    public HashSourceCollection Sources { get; } = new();
    
    public bool Exists(string name) => inner.Exists(name);
    
    public bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
        =>  inner.LoadExternalFileContent(name, out filename, out code, out hash);

    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, out ShaderBuffers bytecode, out ObjectId hash, out bool isFromCache)
    {
        var result = inner.LoadExternalBuffer(name, defines, out bytecode, out hash, out isFromCache);
        if (!Sources.ContainsKey(name))
            Sources.Add(name, hash);
        return result;
    }

    public bool LoadExternalBuffer(string name, string code, ReadOnlySpan<ShaderMacro> defines, out ShaderBuffers bytecode, out ObjectId hash, out bool isFromCache)
        => inner.LoadExternalBuffer(name, code, defines, out bytecode, out hash, out isFromCache);
}