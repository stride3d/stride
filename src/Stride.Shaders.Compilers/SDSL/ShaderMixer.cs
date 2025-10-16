using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Stride.Shaders.Parsing.SDSL;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer(IExternalShaderLoader ShaderLoader)
{
    public void MergeSDSL(string entryShaderName, out byte[] bytecode)
    {
        var temp = new NewSpirvBuffer();

        var context = new SpirvContext();
        var table = new SymbolTable();

        var effectEvaluator = new EffectEvaluator(ShaderLoader);
        var shaderSource = effectEvaluator.EvaluateEffects(new ShaderClassSource(entryShaderName));

        var shaderSource2 = EvaluateInheritanceAndCompositions(shaderSource);

        // Root shader
        MergeMixinNode(new MixinGlobalContext(), context, table, temp, shaderSource2);

        context.Insert(0, new OpCapability(Capability.Shader));
        context.Insert(1, new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        context.Insert(2, new OpExtension("SPV_GOOGLE_hlsl_functionality1"));

        CleanupUnnecessaryInstructions(temp);
        temp.Sort();

        Spv.Dis(temp, Spv.DisassemblerFlags.NameAndId | Spv.DisassemblerFlags.InstructionIndex);

        new StreamAnalyzer().Process(table, temp, context);

        foreach (var inst in context.GetBuffer())
            temp.Add(inst.Data);

        new TypeDuplicateRemover().Apply(temp);
        for (int i = 0; i < temp.Count; i++)
        {
            if (temp[i].Op == Op.OpNop)
                temp.RemoveAt(i--);
        }

        temp.Sort();

        bytecode = temp.ToBytecode();

        //File.WriteAllBytes("test.spv", bytecode);

        Spv.Dis(temp);
        //File.WriteAllText("test.spvdis", source);
    }

    class MixinGlobalContext
    {
        public Dictionary<int, string> Names { get; } = new();
        public Dictionary<int, SymbolType> Types { get; } = new();
    }

    class MixinNodeContext
    {
        public MixinNode Result { get; }
    }


    MixinNode MergeMixinNode(MixinGlobalContext globalContext, SpirvContext context, SymbolTable table, NewSpirvBuffer buffer, ShaderMixinSource mixinSource, MixinNode? stage = null, string? currentCompositionPath = null)
    {
        if (currentCompositionPath != null)
            buffer.Add(new OpSDSLEffect(currentCompositionPath));

        var mixinNode = new MixinNode(stage, currentCompositionPath);

        // Step: expand "for"
        // TODO

        // Merge all classes from mixinSource.Mixins in main buffer
        ProcessMixinClasses(context, buffer, mixinSource, mixinNode);

        //Console.WriteLine("Done SDSL importing");
        //Spv.Dis(buffer, true);

        new TypeDuplicateRemover().Apply(buffer);

        //Console.WriteLine("Done type remapping");
        //Spv.Dis(buffer, true);

        // Build names and types mappings
        ShaderClass.ProcessNameAndTypes(buffer, mixinNode.StartInstruction, mixinNode.EndInstruction, globalContext.Names, globalContext.Types);

        BuildTypesAndMethodGroups(globalContext, context, table, buffer, mixinNode);

        // Compositions (recursive)
        foreach (var shader in mixinNode.Shaders)
        {
            foreach (var variable in shader.Variables)
            {
                if (variable.Value.Type is PointerType pointer && pointer.BaseType is ShaderSymbol shaderSymbol)
                {
                    var compositionMixin = mixinSource.Compositions[variable.Key];
                    var compositionPath = currentCompositionPath != null ? $"{currentCompositionPath}.{variable.Key}" : variable.Key;
                    var compositionResult = MergeMixinNode(globalContext, context, table, buffer, compositionMixin, mixinNode.IsRoot ? mixinNode : mixinNode.Stage, compositionPath);
                    
                    mixinNode.Compositions.Add(variable.Value.Id, compositionResult);
                }
            }
        }

        // Patch method calls (virtual calls & base calls)
        PatchMethodCalls(globalContext, buffer, mixinNode);

        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = buffer[index];
            if (i.Op == Op.OpSDSLImportShader || i.Op == Op.OpSDSLImportFunction)
            {
                SetOpNop(i.Data.Memory.Span);
            }
        }

        if (currentCompositionPath != null)
            buffer.Add(new OpSDSLEffectEnd());

        return mixinNode;
    }

    private void ProcessMixinClasses(SpirvContext context, NewSpirvBuffer temp, ShaderMixinSource mixinSource, MixinNode mixinNode)
    {
        var isRoot = mixinNode.Stage == null;
        var offset = context.Bound;
        var nextOffset = 0;

        var shaders = mixinNode.Shaders;
        var shadersByName = mixinNode.ShadersByName;

        mixinNode.StartInstruction = temp.Count;
        foreach (var shaderClass in mixinSource.Mixins)
        {
            var shader = SpirvBuilder.GetOrLoadShader(ShaderLoader, shaderClass.ClassName);
            offset += nextOffset;
            nextOffset = 0;
            shader.Header = shader.Header with { Bound = shader.Header.Bound + offset };

            var shaderStart = temp.Count;

            bool skipFunction = false;

            // Copy instructions to main buffer
            for (var index = 0; index < shader.Count; index++)
            {
                var i = shader[index];
                
                // Do we need to skip variable/functions? (depending on stage/non-stage)
                {
                    var include = true;
                    if (i.Op == Op.OpFunction && shader[index + 1].Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)shader[index + 1] is { } functionInfo)
                    {
                        var isStage = (functionInfo.Flags & FunctionFlagsMask.Stage) != 0;
                        include = isStage switch
                        {
                            // Import stage members only if root level
                            true => isRoot || shaderClass.ImportStageOnly,
                            // Import non-stage members only if import stage only is not specified
                            false => !shaderClass.ImportStageOnly,
                        };
                    }
                    if (i.Op == Op.OpVariable)
                    {
                        // TODO
                    }


                    if (!include)
                    {
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
            }

            // Build ShaderInfo
            var shaderInfo = new ShaderInfo(shaders.Count, shaderClass.ClassName, shaderStart, temp.Count);
            shaderInfo.CompositionPath = mixinNode.CompositionPath;
            if (mixinNode.Stage != null && mixinNode.Stage.ShadersByName.TryGetValue(shaderClass.ClassName, out var stageShaderInfo))
                shaderInfo.Stage = stageShaderInfo;

            PopulateShaderInfo(temp, shaderStart, temp.Count, shaderInfo, mixinNode);

            shadersByName.Add(shaderClass.ClassName, shaderInfo);
            shaders.Add(shaderInfo);

            // Remap ids from inherited class (OpSDSLImport*)
            RemapInheritedIds(temp, shaderStart, temp.Count, shaderInfo, mixinNode);
        }

        mixinNode.EndInstruction = temp.Count;
        context.Bound = offset + nextOffset + 1;
    }

    private static void BuildTypesAndMethodGroups(MixinGlobalContext globalContext, SpirvContext context, SymbolTable table, NewSpirvBuffer temp, MixinNode mixinNode)
    {
        // Setup types in context
        foreach (var type in globalContext.Types)
        {
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
                currentShader = mixinNode.ShadersByName[shaderInstruction.ShaderName];
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

    private static void PatchMethodCalls(MixinGlobalContext globalContext, NewSpirvBuffer temp, MixinNode mixinNode)
    {
        var externalShaders = new HashSet<int>();
        var externalFunctions = new Dictionary<int, string>();
        ShaderInfo? currentShader = null;
        for (var index = mixinNode.StartInstruction; index < mixinNode.EndInstruction; index++)
        {
            var i = temp[index];
            // Only import shaders should be left
            if (i.Data.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)i is { } importShader)
            {
                // Only external should be left
                if (importShader.Type == ImportType.External)
                {
                    externalShaders.Add(importShader.ResultId);
                    SetOpNop(i.Data.Memory.Span);
                }
            }
            else if (i.Data.Op == Op.OpSDSLImportFunction && (OpSDSLImportFunction)i is { } importFunction)
            {
                if (externalShaders.Contains(importFunction.Shader))
                {
                    externalFunctions.Add(importFunction.ResultId, importFunction.FunctionName);
                    SetOpNop(i.Data.Memory.Span);
                }
            }
            // Removing OpName for OpSDSLImportShader and OpSDSLImportFunction (they are always located after, so no problem to do it in a single pass)
            else if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (externalShaders.Contains(nameInstruction.Target) || externalFunctions.ContainsKey(nameInstruction.Target))
                {
                    SetOpNop(i.Data.Memory.Span);
                }
            }
            else if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                if (!mixinNode.MethodGroups.TryGetValue(function.ResultId, out var methodGroupEntry))
                    throw new InvalidOperationException($"Can't find method group info for {globalContext.Names[function.ResultId]}");

                currentShader = mixinNode.Shaders.Last(x => index >= x.StartInstruction);
            }
            else if (i.Data.Op == Op.OpFunctionCall && (OpFunctionCall)i is { } functionCall)
            {
                var methodMixinGroup = mixinNode;
                var isBaseCall = temp[index - 1].Op == Op.OpSDSLCallBase;

                // Process member call (composition)
                if (temp[index - 1].Op == Op.OpSDSLCallTarget)
                {
                    var callTarget = (OpSDSLCallTarget)temp[index - 1];
                    var composition = mixinNode.Compositions[callTarget.Target];
                    methodMixinGroup = composition;

                    Spv.Dis(temp, Spv.DisassemblerFlags.Id);

                    var functionName = externalFunctions[functionCall.Function];
                    var functionId = composition.MethodGroupsByName[functionName];

                    functionCall.Function = functionId;

                    SetOpNop(temp[index - 1].Data.Memory.Span);
                }

                Spv.Dis(temp, Spv.DisassemblerFlags.NameAndId | Spv.DisassemblerFlags.InstructionIndex);

                bool foundInStage = false;
                if (!methodMixinGroup.MethodGroups.TryGetValue(functionCall.Function, out var methodGroupEntry))
                {
                    // Try again as a stage method (only if not a base call)
                    if (methodMixinGroup.Stage == null || !methodMixinGroup.Stage.MethodGroups.TryGetValue(functionCall.Function, out methodGroupEntry))
                        throw new InvalidOperationException($"Can't find method group info for {globalContext.Names[functionCall.Function]}");
                    foundInStage = true;
                }

                // Process base call
                if (isBaseCall)
                {
                    // We currently do not allow calling base stage method from a non-stage method
                    // (if we were to allow them later, we would need to tweak following detection code as ShaderIndex comparison is only valid for items within the same MixinNode)
                    if (foundInStage)
                        throw new InvalidOperationException($"Method {globalContext.Names[functionCall.Function]} was found but a base call can't be performed on a stage method from a non-stage method");

                    // Is it a base call? if yes, find the direct parent
                    // Let's find the method in same group just before ours
                    bool baseMethodFound = false;
                    for (int j = methodGroupEntry.Methods.Count - 1; j >= 0; --j)
                    {
                        if (methodGroupEntry.Methods[j].Shader.ShaderIndex < currentShader.ShaderIndex)
                        {
                            functionCall.Function = methodGroupEntry.Methods[j].MethodId;
                            baseMethodFound = true;
                            break;
                        }
                    }

                    if (!baseMethodFound)
                        throw new InvalidOperationException($"Can't find a base method for {globalContext.Names[functionCall.Function]}");

                    SetOpNop(temp[index - 1].Data.Memory.Span);
                }
                else
                {
                    // If not, get the most derived implementation
                    functionCall.Function = methodGroupEntry.Methods[^1].MethodId;
                }
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
            // Remove Nop
            if (temp[i].Op == Op.OpNop)
                temp.RemoveAt(i--);
            // Also remove some other SDSL specific operators (that we keep late mostly for debug purposes)
            else if (temp[i].Op == Op.OpSDSLShader || temp[i].Op == Op.OpSDSLShaderEnd || temp[i].Op == Op.OpSDSLEffect || temp[i].Op == Op.OpSDSLEffectEnd)
                temp.RemoveAt(i--);
        }
    }
}