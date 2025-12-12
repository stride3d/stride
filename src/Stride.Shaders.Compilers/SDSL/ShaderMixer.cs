using CommunityToolkit.HighPerformance;
using Silk.NET.SPIRV.Cross;
using Stride.Core.Extensions;
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
        var table = new SymbolTable { ShaderLoader = ShaderLoader };

        var effectEvaluator = new EffectEvaluator(ShaderLoader);
        shaderSource = effectEvaluator.EvaluateEffects(shaderSource);

        var shaderSource2 = EvaluateInheritanceAndCompositions(shaderSource);

        // Root shader
        var globalContext = new MixinGlobalContext();
        var rootMixin = MergeMixinNode(globalContext, context, table, temp, shaderSource2);

        context.Insert(0, new OpCapability(Capability.Shader));
        context.Insert(1, new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        context.Insert(2, new OpExtension("SPV_GOOGLE_hlsl_functionality1"));

        Spv.Dis(temp, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);

        new StreamAnalyzer().Process(table, temp, context);

        // Merge cbuffers and rgroups
        // TODO: remove unused cbuffers (before merging them)
        Spv.Dis(temp, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        MergeCBuffers(globalContext, context, temp);
        Spv.Dis(temp, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        ComputeCBufferOffsets(globalContext, context, temp);

        // Process reflection
        ProcessReflection(globalContext, context, temp, rootMixin);

        temp.Sort();

        CleanupUnnecessaryInstructions(temp);

        foreach (var inst in context)
            temp.Add(inst.Data);

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

    private void MergeCBuffers(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer)
    {
        // If multiple cbuffer with same name Test, they will be renamed Test.0 Test.1 etc.
        string GetCBufferFinalName(string cbufferName)
        {
            var dotIndex = cbufferName.IndexOf('.');
            if (dotIndex != -1)
                return cbufferName.Substring(0, dotIndex);

            return cbufferName;
        }

        // OpSDSLEffect is emitted for any non-root composition
        var compositionNodes = buffer
            .Where(x => x.Op == Op.OpSDSLEffect)
            .Select(x => (StartIndex: x.Index, CompositionPath: ((OpSDSLEffect)x).EffectName))
            .ToList();

        var shaders = buffer
            .Where(x => x.Op == Op.OpSDSLShader)
            .Select(x => (StartIndex: x.Index, ShaderName: ((OpSDSLShader)x).ShaderName))
            .ToList();

        var cbuffersByNames = buffer
            .Where(x => x.Op == Op.OpVariableSDSL)
            .Select(x => (Index: x.Index, Variable: (OpVariableSDSL)x))
            // Note: MemberIndexOffset is simply a shift in Members index, not something like a byte offset
            .Select(x => (
                Variable: x.Variable,
                CompositionPath: compositionNodes.LastOrDefault(mixinNode => x.Index >= mixinNode.StartIndex).CompositionPath,
                ShaderName: shaders.LastOrDefault(shader => x.Index >= shader.StartIndex).ShaderName,
                StructTypePtrId: x.Variable.ResultType,
                StructType: context.ReverseTypes[x.Variable.ResultType] is PointerType p && p.StorageClass == Specification.StorageClass.Uniform && p.BaseType is StructuredType s ? s : null,
                MemberIndexOffset: 0))
            // TODO: Check Decoration.Block?
            .Where(x => x.StructType != null)
            .GroupBy(x => GetCBufferFinalName(globalContext.Names[x.Variable.ResultId]));

        var cbufferStructTypes = cbuffersByNames.SelectMany(x => x).Select(x => context.Types[x.StructType]).ToHashSet();

        Dictionary<(int StructType, int Member), string> links = new();
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.LinkSDSL, Parameters: { } m } } memberDecorate)
            {
                if (cbufferStructTypes.Contains(memberDecorate.StructType))
                {
                    using var n = new LiteralValue<string>(m.Span);
                    links.Add((memberDecorate.StructType, memberDecorate.Member), n.Value);
                    SetOpNop(i.Data.Memory.Span);
                }
            }
        }

        void DecorateLinks(Span<(OpVariableSDSL Variable, string CompositionPath, string ShaderName, int StructTypePtrId, StructuredType? StructType, int MemberIndexOffset)> cbuffersSpan, int cbufferStructId)
        {
            int mergedMemberIndex = 0;
            foreach (ref var cbuffer in cbuffersSpan)
            {
                var compositionPath = cbuffer.CompositionPath;

                for (int memberIndex = 0; memberIndex < cbuffer.StructType.Members.Count; memberIndex++, mergedMemberIndex++)
                {
                    var member = cbuffer.StructType.Members[memberIndex];

                    var link = $"{TypeName.GetTypeNameWithoutGenerics(cbuffer.ShaderName)}.{member.Name}";
                    if (!compositionPath.IsNullOrEmpty())
                        link = $"{link}.{compositionPath}";

                    // Check if there is already a decoration (i.e. from an explicit "Link")
                    if (links.TryGetValue((context.Types[cbuffer.StructType], memberIndex), out var linkValue))
                        link = linkValue;

                    context.Add(new OpMemberDecorateString(cbufferStructId, mergedMemberIndex, ParameterizedFlags.DecorationLinkSDSL(link)));
                }
            }
        }

        foreach (var cbuffersEntry in cbuffersByNames)
        {
            var cbuffers = cbuffersEntry.ToList();
            var cbuffersSpan = CollectionsMarshal.AsSpan(cbuffers);

            if (cbuffersEntry.Count() == 1)
            {
                DecorateLinks(cbuffersSpan, context.Types[cbuffersEntry.First().StructType]);
            }
            // More than 1 cbuffers with same name
            else
            {
                int offset = 0;
                // TODO: Analyze and skip cbuffers parts which are unused
                foreach (ref var cbuffer in cbuffersSpan)
                {
                    cbuffer.MemberIndexOffset = offset;
                    offset += cbuffer.StructType.Members.Count;
                }
                var variables = cbuffers.ToDictionary(x => x.Variable.ResultId, x => x);
                var structTypes = cbuffers.Select(x => x.StructType);

                var mergedCbufferStruct = new ConstantBufferSymbol(cbuffersEntry.Key, structTypes.SelectMany(x => x.Members).ToList());
                var mergedCbufferStructId = context.DeclareCBuffer(mergedCbufferStruct);
                var mergedCbufferPtrStructId = context.GetOrRegister(new PointerType(mergedCbufferStruct, Specification.StorageClass.Uniform));

                DecorateLinks(cbuffersSpan, mergedCbufferStructId);

                // Remap member ids
                foreach (var i in buffer)
                {
                    if (i.Op == Op.OpAccessChain && (OpAccessChain)i is { } accessChain)
                    {
                        if (variables.TryGetValue(accessChain.BaseId, out var cbuffer) && cbuffer.MemberIndexOffset > 0)
                        {
                            // According to spec, this must be a OpConstant (and we only create them with int)
                            var indexes = accessChain.Values.Elements.Span;
                            var constantId = indexes[0];
                            var index = cbuffer.MemberIndexOffset + (int)SpirvBuilder.GetConstantValue(constantId, buffer);
                            indexes[0] = context.CompileConstant(index).Id;

                            // Regenerate buffer (since we modify accessChain.Values, it doesn't get rebuilt automatically)
                            accessChain.UpdateInstructionMemory();
                        }
                    }
                    // Out of safety, check for any OpLoad/OpStore on the variables (forbidden, only OpAccessChain)
                    else if (i.Op == Op.OpLoad && (OpLoad)i is { } load)
                    {
                        if (variables.TryGetValue(load.Pointer, out var cbuffer))
                            throw new NotSupportedException("Can't OpLoad with cbuffer");
                    }
                    else if (i.Op == Op.OpStore && (OpStore)i is { } store)
                    {
                        if (variables.TryGetValue(store.Pointer, out var cbuffer))
                            throw new NotSupportedException("Can't OpLoad with cbuffer");
                    }
                }

                // Update first variable to use new type
                cbuffersSpan[0].Variable.ResultType = mergedCbufferPtrStructId;
                // Update name
                globalContext.Names[cbuffersSpan[0].Variable.ResultId] = cbuffersEntry.Key;
                foreach (var i in buffer)
                {
                    if (i.Op == Op.OpName && (OpName)i is { } name)
                    {
                        // Ensure cbuffer variable name is correct (it might still have a pending number such as Test.0 if there was multiple buffers with same name)
                        if (cbuffersSpan[0].Variable.ResultId == name.Target)
                            name.Name = cbuffersEntry.Key;
                        // Remove any other OpName (after remapping they would all point to the merged variable)
                        foreach (var cbuffer in cbuffersSpan[1..])
                        {
                            if (cbuffer.Variable.ResultId == name.Target)
                                SetOpNop(i.Data.Memory.Span);
                        }
                    }
                }

                var idRemapping = new Dictionary<int, int>();
                foreach (ref var cbuffer in cbuffersSpan.Slice(1))
                {
                    // Update all cbuffers access to be replaced with first variable (unified cbuffer)
                    idRemapping.Add(cbuffer.Variable.ResultId, cbuffersSpan[0].Variable.ResultId);
                    // Remove other cbuffer variables
                    SetOpNop(cbuffer.Variable.InstructionMemory.Span);
                    // TODO: Do we want to remove unecessary types?
                    //       Maybe we don't care as they are not used anymore, they will be ignored.
                    //       Also, if we do so, maybe we could do it as part of a global pass at the end rather than now?
                }

                SpirvBuilder.RemapIds(buffer, 0, buffer.Count, idRemapping);
            }
        }
    }

    private void ComputeCBufferOffsets(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer)
    {
        var cbuffers = buffer
            .Where(x => x.Op == Op.OpVariableSDSL)
            .Select(x => (OpVariableSDSL)x)
            // Note: MemberIndexOffset is simply a shift in Members index, not something like a byte offset
            .Select(x => (
                Variable: x,
                StructTypePtrId: x.ResultType,
                StructType: context.ReverseTypes[x.ResultType] is PointerType p && p.StorageClass == Specification.StorageClass.Uniform && p.BaseType is StructuredType s ? s : null,
                MemberIndexOffset: 0))
            .Where(x => x.StructType != null)
            .ToList();

        EffectTypeDescription ConvertStructType(SpirvContext context, StructType s)
        {
            var structId = context.Types[s];

            var hasOffsetDecorations = false;
            foreach (var i in context.GetBuffer())
            {
                if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.Offset } } memberDecorate && memberDecorate.StructType == structId)
                {
                    hasOffsetDecorations = true;
                }
            }

            var members = new EffectTypeMemberDescription[s.Members.Count];
            var offset = 0;
            for (int i = 0; i < s.Members.Count; ++i)
            {
                members[i] = new EffectTypeMemberDescription
                {
                    Name = s.Members[i].Name,
                    Type = ConvertType(context, s.Members[i].Type, s.Members[i].TypeModifier),
                    Offset = offset,
                };

                var memberSize = SpirvBuilder.ComputeCBufferOffset(s.Members[i].Type, s.Members[i].TypeModifier, ref offset);

                // Note: we assume if already added by another cbuffer using this type, the offsets were computed the same way
                if (!hasOffsetDecorations)
                    context.Add(new OpMemberDecorate(context.Types[s], i, ParameterizedFlags.DecorationOffset(offset)));

                offset += memberSize;
            }
            return new EffectTypeDescription { Class = EffectParameterClass.Struct, RowCount = 1, ColumnCount = 1, Name = s.Name, Members = members, ElementSize = offset };
        }


        EffectTypeDescription ConvertType(SpirvContext context, SymbolType symbolType, TypeModifier typeModifier)
        {
            return symbolType switch
            {
                ScalarType { TypeName: "int" } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Int, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                ScalarType { TypeName: "float" } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Float, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                ArrayType a => ConvertArrayType(context, a, typeModifier),
                StructType s => ConvertStructType(context, s),
                // TODO: should we use RowCount instead? (need to update Stride)
                VectorType v => ConvertType(context, v.BaseType, typeModifier) with { Class = EffectParameterClass.Vector, ColumnCount = v.Size },
                MatrixType m when typeModifier == TypeModifier.ColumnMajor || typeModifier == TypeModifier.None
                    => ConvertType(context, m.BaseType, typeModifier) with { Class = EffectParameterClass.MatrixColumns, RowCount = m.Rows, ColumnCount = m.Columns },
                MatrixType m when typeModifier == TypeModifier.RowMajor
                    => ConvertType(context, m.BaseType, typeModifier) with { Class = EffectParameterClass.MatrixRows, RowCount = m.Rows, ColumnCount = m.Columns },
            };

            EffectTypeDescription ConvertArrayType(SpirvContext context, ArrayType a, TypeModifier typeModifier)
            {
                var typeId = context.Types[a];
                var elementType = ConvertType(context, a.BaseType, typeModifier);

                var hasStrideDecoration = false;
                foreach (var i in context.GetBuffer())
                {
                    if (i.Op == Op.OpDecorate && (OpDecorate)i is { Decoration: { Value: Decoration.ArrayStride } } arrayStrideDecoration && arrayStrideDecoration.Target == typeId)
                    {
                        hasStrideDecoration = true;
                    }
                }

                var arrayStride = (elementType.ElementSize + 15) / 16 * 16;
                context.Add(new OpDecorate(typeId, ParameterizedFlags.DecorationArrayStride(arrayStride)));

                return elementType with { Elements = a.Size };
            }
        }

        // Scan LinkSDSL decorations
        Dictionary<(int StructType, int Member), string> links = new();
        foreach (var i in context.GetBuffer())
        {
            if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.LinkSDSL, Parameters: { } m } } memberDecorate)
            {
                using var n = new LiteralValue<string>(m.Span);
                links.Add((memberDecorate.StructType, memberDecorate.Member), n.Value);
            }
        }

        foreach (var cbuffer in cbuffers)
        {
            int constantBufferOffset = 0;
            var cb = cbuffer.StructType;
            var structTypeId = context.Types[cb];
            
            var memberInfos = new EffectValueDescription[cb.Members.Count];
            for (var index = 0; index < cb.Members.Count; index++)
            {
                // Properly compute size and offset according to DirectX rules
                var member = cb.Members[index];
                var memberSize = SpirvBuilder.ComputeCBufferOffset(member.Type, member.TypeModifier, ref constantBufferOffset);

                context.Add(new OpMemberDecorate(context.Types[cbuffer.StructType], index, ParameterizedFlags.DecorationOffset(constantBufferOffset)));

                if (!links.TryGetValue((structTypeId, index), out var linkName))
                    throw new InvalidOperationException($"Could not find cbuffer member link info; it should have been generated during {MergeCBuffers}");

                memberInfos[index] = new EffectValueDescription
                {
                    Type = ConvertType(context, member.Type, member.TypeModifier),
                    RawName = member.Name,
                    KeyInfo = new EffectParameterKeyInfo { KeyName = linkName },
                    Offset = constantBufferOffset,
                    Size = memberSize,
                };

                // Adjust offset for next item
                constantBufferOffset += memberSize;
            }

            globalContext.Reflection.ConstantBuffers.Add(new EffectConstantBufferDescription
            {
                Name = globalContext.Names[cbuffer.Variable.ResultId],
                // Round buffer size to next multiple of 16 bytes
                Size = (constantBufferOffset + 15) / 16 * 16,

                Type = ConstantBufferType.ConstantBuffer,
                Members = memberInfos,
            });
        }
    }

    class MixinGlobalContext
    {
        public Dictionary<int, string> Names { get; } = [];
        public Dictionary<int, SymbolType> Types { get; } = [];

        public EffectReflection Reflection { get; } = new();
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
            buffer.Add(new OpSDSLEffect(currentCompositionPath));

        var mixinNode = new MixinNode(stage, currentCompositionPath);

        // Step: expand "for"
        // TODO

        // Merge all classes from mixinSource.Mixins in main buffer
        ProcessMixinClasses(context, buffer, mixinSource, mixinNode);

        // Import struct types
        ImportStructTypes(globalContext, buffer, mixinNode);

        new TypeDuplicateRemover().Apply(buffer);

        // Build names and types mappings
        ShaderClass.ProcessNameAndTypes(buffer, mixinNode.StartInstruction, mixinNode.EndInstruction, globalContext.Names, globalContext.Types);

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
            buffer.Add(new OpSDSLEffectEnd());

        return mixinNode;
    }

    private void ProcessMixinClasses(SpirvContext context, NewSpirvBuffer temp, ShaderMixinInstantiation mixinSource, MixinNode mixinNode)
    {
        var isRootMixin = mixinNode.Stage == null;
        var stage = mixinNode.Stage;
        var offset = context.Bound;
        var nextOffset = 0;

        var shaders = mixinNode.Shaders;

        mixinNode.StartInstruction = temp.Count;
        foreach (var shaderClass in mixinSource.Mixins)
        {
            if (shaderClass.ImportStageOnly)
            {
                if (!isRootMixin)
                    throw new InvalidOperationException("importing stage-only methods/variables is only possible at the root mixin");
            }

            var shader = shaderClass.Buffer;
            offset += nextOffset;
            nextOffset = 0;
            shader.Header = shader.Header with { Bound = shader.Header.Bound + offset };

            var shaderStart = temp.Count;

            bool skipFunction = false;

            var forbiddenIds = new HashSet<int>();
            var remapIds = new Dictionary<int, int>();
            var names = new Dictionary<int, string>();
            var removedIds = new HashSet<int>();

            bool ProcessStageMember(int memberId, bool isStage)
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
                    var stageShader = stage.ShadersByName[shaderClass.ToClassName()];
                    var memberName = names[memberId];
                    var stageMember = stageShader.FindMember(memberName);
                    remapIds.Add(offset + memberId, stageMember.Id);
                }
                // Otherwise, if not included, it means we need to forbid this IDs (which could only happen if referencing non-stage member from a stage method)
                else if (!include)
                {
                    forbiddenIds.Add(offset + memberId);
                }

                return include;
            }

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
                        include = ProcessStageMember(function.ResultId, isStage);
                    }
                    if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variableInstruction)
                    {
                        var isStage = (variableInstruction.Flags & VariableFlagsMask.Stage) != 0;
                        include = ProcessStageMember(variableInstruction.ResultId, isStage);
                    }

                    if (!include)
                    {
                        if (i.Data.IdResult is int id)
                            removedIds.Add(offset + id);

                        // Special case for function: skip until function end
                        // (for other cases such as variable, skipping only current instruction is enough)
                        if (i.Op == Op.OpFunction)
                        {
                            // Skip until end of function
                            while (shader[++index].Op != Op.OpFunctionEnd)
                            {
                            }
                        }

                        // Go to next instruction
                        continue;
                    }
                }

                var i2 = new OpData(i.Data.Memory.Span);
                temp.Add(i2);

                if (i.Data.IdResult != null && i.Data.IdResult.Value > nextOffset)
                    nextOffset = i.Data.IdResult.Value;

                if (offset > 0)
                    OffsetIds(i2, offset);

                if (SpirvBuilder.ContainIds(forbiddenIds, i2))
                    throw new InvalidOperationException($"Stage instruction {i.Data} references a non-stage ID");

                SpirvBuilder.RemapIds(remapIds, i2);
            }

            for (var index = shaderStart; index < temp.Count; index++)
            {
                // Second pass: remove OpName/OpMember referencing to removed IDs
                var i = temp[index];
                if (i.Op == Op.OpName && (OpName)i is { } name)
                {
                    if (removedIds.Contains(name.Target))
                        SetOpNop(i.Data.Memory.Span);
                }
                else if (i.Op == Op.OpMemberName || i.Op == Op.OpMemberDecorate || i.Op == Op.OpMemberDecorateString)
                {
                    // Structure ID is always stored in first operand
                    var target = i.Data.Memory.Span[1];
                    if (removedIds.Contains(target))
                        SetOpNop(i.Data.Memory.Span);
                }
            }

            // Link attribute: postfix with composition path
            if (mixinNode.CompositionPath != null)
            {
                foreach (var i in temp)
                {
                    if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.LinkSDSL, Parameters: { } m } } memberDecorate)
                    {
                        var n = new LiteralValue<string>(m.Span);
                        n.Value = $"{n.Value}.{mixinNode.CompositionPath}";
                        n.Dispose();
                    }
                }
            }

            shaderClass.Start = shaderStart;
            shaderClass.End = temp.Count;
            shaderClass.OffsetId = offset;

            // Build ShaderInfo
            var shaderInfo = new ShaderInfo(shaders.Count, shaderClass.ClassName, shaderStart, temp.Count);
            shaderInfo.CompositionPath = mixinNode.CompositionPath;
            if (mixinNode.Stage != null && mixinNode.Stage.ShadersByName.TryGetValue(shaderClass.ClassName, out var stageShaderInfo))
                shaderInfo.Stage = stageShaderInfo;

            PopulateShaderInfo(temp, shaderStart, temp.Count, shaderInfo, mixinNode);

            mixinNode.ShadersByName.Add(shaderClass.ToClassName(), shaderInfo);
            shaders.Add(shaderInfo);

            BuildImportInfo(temp, shaderStart, temp.Count, shaderClass, shaderInfo, mixinNode);
        }

        mixinNode.EndInstruction = temp.Count;
        context.Bound = offset + nextOffset + 1;
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

                    var methodMixinGroup = mixinNode;
                    if (!mixinNode.IsRoot && (functionInfo.Flags & FunctionFlagsMask.Stage) != 0)
                        methodMixinGroup = methodMixinGroup.Stage;

                    // If OpSDSLFunctionInfo.Parent is coming from a OpSDSLImportFunction, find the real ID
                    if (functionInfo.Parent != 0)
                    {
                        if (mixinNode.ExternalFunctions.TryGetValue(functionInfo.Parent, out var parentFunctionInfo))
                        {
                            var shaderName = mixinNode.ExternalShaders[parentFunctionInfo.ShaderId];
                            functionInfo.Parent = mixinNode.ShadersByName[shaderName].Functions[parentFunctionInfo.Name].Id;
                        }
                    }

                    // Check if it has a parent (and if yes, share the MethodGroup)
                    if (!methodMixinGroup.MethodGroups.TryGetValue(functionInfo.Parent, out var methodGroup))
                        methodGroup = new MethodGroup { Name = functionName };

                    methodGroup.Shader = currentShader;
                    methodGroup.Methods.Add((Shader: currentShader, MethodId: function.ResultId));

                    methodMixinGroup.MethodGroups[function.ResultId] = methodGroup;

                    // Also add lookup by name
                    if (!methodMixinGroup.MethodGroupsByName.TryGetValue(functionName, out var methodGroups))
                        methodMixinGroup.MethodGroupsByName.Add(functionName, function.ResultId);

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

    private void ImportStructTypes(MixinGlobalContext globalContext, NewSpirvBuffer buffer, MixinNode mixinNode)
    {
        var idRemapping = new Dictionary<int, int>();
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = buffer[index];

            if (i.Data.Op == Op.OpSDSLImportStruct && (OpSDSLImportStruct)i is { } importStruct)
            {
                var shaderName = mixinNode.ExternalShaders[importStruct.Shader];
                var shader = mixinNode.ShadersByName[shaderName];
                var structId = shader.StructTypes[importStruct.StructName];
                idRemapping.Add(importStruct.ResultId, structId);
                SetOpNop(i.Data.Memory.Span);
            }
        }

        // Remove associated OpName
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = buffer[index];

            if (i.Data.Op == Op.OpName && (OpName)i is { } name)
            {
                if (idRemapping.ContainsKey(name.Target))
                {
                    SetOpNop(i.Data.Memory.Span);
                }
            }
        }

        SpirvBuilder.RemapIds(buffer, mixinNode.StartInstruction, mixinNode.EndInstruction, idRemapping);
    }

    private static void ExpandForeach(SpirvContext context, NewSpirvBuffer buffer, MixinNode mixinNode, int index, OpForeachSDSL @foreach)
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

        // Check the variable
        if (!mixinNode.CompositionArrays.TryGetValue(@foreach.Collection, out var compositions))
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

            // Build a buffer with all foreach instructions (with new ids)
            foreach (var i2 in foreachBuffer[1..^1]) // skip start/end
            {
                var i3 = new OpData(i2.Memory.Span);
                // All result ids are remapped to new ids
                if (i3.IdResult is int result)
                    idRemapping.Add(result, context.Bound++);
                SpirvBuilder.RemapIds(idRemapping, i3);

                foreachBufferCopy.Add(i3);
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
                SpirvBuilder.RemapIds(memberAccesses, i.Data);

            if (i.Data.Op == Op.OpForeachSDSL && (OpForeachSDSL)i is { } @foreach)
            {
                ExpandForeach(context, temp, mixinNode, index, @foreach);
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
                if (mixinNode.CompositionArrays.TryGetValue(accessChain.BaseId, out var compositions))
                {
                    var compositionIndex = (int)SpirvBuilder.GetConstantValue(accessChain.Values.Elements.Span[0], context.GetBuffer(), temp);
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

                if (mixinNode.ExternalVariables.TryGetValue(memberAccess.Member, out var variable))
                {
                    var shaderName = mixinNode.ExternalShaders[variable.ShaderId];

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
                else if (globalContext.Types[memberAccess.ResultType] is FunctionType)
                {
                    // In case of functions, OpMemberAccessSDSL.Member could either be a OpFunction or a OpImportFunctionSDSL
                    var functionId = memberAccess.Member;
                    if (mixinNode.ExternalFunctions.TryGetValue(memberAccess.Member, out var function))
                        // Process member call (composition)
                        functionId = instanceMixinGroup.MethodGroupsByName[function.Name];

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
            else if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                if (!mixinNode.MethodGroups.TryGetValue(function.ResultId, out var methodGroupEntry))
                    throw new InvalidOperationException($"Can't find method group info for {globalContext.Names[function.ResultId]}");
            }
            else if (i.Data.Op == Op.OpFunctionEnd)
            {
                memberAccesses.Clear();
            }

            SpirvBuilder.RemapIds(memberAccesses, i.Data);
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

        Dictionary<int, LinkInfo> linkInfos = new();
        string currentShaderName = string.Empty;
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = buffer[index];

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
            else if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shader)
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

                    if (pointerType.BaseType is TextureType)
                    {
                        var slot = globalContext.Reflection.ResourceBindings.Count;
                        globalContext.Reflection.ResourceBindings.Add(effectResourceBinding with
                        {
                            Class = EffectParameterClass.ShaderResourceView,
                            Type = EffectParameterType.Texture,
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

                        cbufferSlot++;
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
                        if (o.Words[i * 2 + 0] != 0)
                            o.Words[i * 2 + 0] += offset;
                    }

                    if (o.Kind == OperandKind.PairLiteralIntegerIdRef || o.Kind == OperandKind.PairIdRefIdRef)
                    {
                        if (o.Words[i * 2 + 1] != 0)
                            o.Words[i * 2 + 1] += offset;
                    }
                }
            }
        }
    }

    private static void CleanupUnnecessaryInstructions(NewSpirvBuffer temp)
    {
        for (int i = 0; i < temp.Count; i++)
        {
            // Transform OpVariableSDSL into OpVariable (we don't need extra info anymore)
            if (temp[i].Op == Op.OpVariableSDSL && (OpVariableSDSL)temp[i] is { } variable)
                temp.Replace(i, new OpVariable(variable.ResultType, variable.ResultId, variable.Storageclass, variable.Initializer));

            // Remove Nop
            if (temp[i].Op == Op.OpNop)
                temp.RemoveAt(i--);
            // Also remove some other SDSL specific operators (that we keep late mostly for debug purposes)
            else if (temp[i].Op == Op.OpSDSLShader
                || temp[i].Op == Op.OpSDSLShaderEnd
                || temp[i].Op == Op.OpSDSLEffect
                || temp[i].Op == Op.OpSDSLEffectEnd
                || temp[i].Op == Op.OpConstantStringSDSL
                || temp[i].Op == Op.OpTypeGenericLinkSDSL
                || temp[i].Op == Op.OpSDSLImportShader
                || temp[i].Op == Op.OpSDSLImportFunction
                || temp[i].Op == Op.OpSDSLImportVariable)
                temp.RemoveAt(i--);
            else if (temp[i].Op == Op.OpDecorateString && ((OpDecorateString)temp[i]).Decoration.Value == Decoration.LinkSDSL)
                temp.RemoveAt(i--);
            else if (temp[i].Op == Op.OpMemberDecorateString && ((OpMemberDecorateString)temp[i]).Decoration.Value == Decoration.LinkSDSL)
                temp.RemoveAt(i--);
        }
    }
}