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

public class ShaderMixer(IExternalShaderLoader ShaderLoader)
{
    class MixinGroup
    {
        public List<string> InheritanceList { get; } = new();
    }

    public void MergeSDSL(string entryShaderName, out byte[] bytecode)
    {
        var temp = new NewSpirvBuffer();

        var context = new SpirvContext();
        var table = new SymbolTable();

        var shaderSource = EvaluateEffects(new ShaderClassSource(entryShaderName));

        var shaderSource2 = EvaluateInheritanceAndCompositions(shaderSource);

        // Root shader
        MergeSDSLMixin(new MixinResultGlobal(), null, context, table, temp, shaderSource2);

        context.Insert(0, new OpCapability(Capability.Shader));
        context.Insert(1, new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        context.Insert(2, new OpExtension("SPV_GOOGLE_hlsl_functionality1"));

        {
            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i].Op == Op.OpNop)
                    temp.RemoveAt(i--);
            }
            temp.Sort();
            Spv.Dis(temp, true);
        }

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

        Spv.Dis(temp, true);
        //File.WriteAllText("test.spvdis", source);
    }

    private void Merge(ShaderMixinSource mixinTree, ShaderSource source)
    {
        switch (source)
        {
            case ShaderClassSource classSource:
                mixinTree.Mixins.Add(classSource);
                break;
            case ShaderMixinSource mixinSource:
                foreach (var mixin in mixinSource.Mixins)
                {
                    mixinTree.Mixins.Add(mixin);
                }

                foreach (var composition in mixinSource.Compositions)
                {
                    if (mixinTree.Compositions.TryGetValue(composition.Key, out var mixinTreeComposition))
                        mixinTree.Compositions.Add(composition.Key, mixinTreeComposition = new ShaderMixinSource());
                    Merge(mixinTreeComposition, composition.Value);
                }
                
                break;
        }
    }

    private ShaderSource EvaluateEffects(ShaderSource source)
    {
        switch (source)
        {
            case ShaderClassSource classSource:
                if (classSource.GenericArguments != null && classSource.GenericArguments.Length > 0)
                    throw new NotImplementedException();

                var buffer = SpirvBuilder.GetOrLoadShader(ShaderLoader, classSource.ClassName);
                if (buffer[0].Op == Op.OpSDSLEffect)
                {
                    var mixinTree = new ShaderMixinSource();
                    foreach (var instruction in buffer)
                    {
                        if (instruction.Op == Op.OpSDSLMixin && (OpSDSLMixin)instruction is { } mixinInstruction)
                        {
                            var instSource = new ShaderClassSource(mixinInstruction.Mixin);
                            var evaluatedSource = EvaluateEffects(instSource);

                            Merge(mixinTree, evaluatedSource);
                        }
                        else if (instruction.Op == Op.OpSDSLMixinCompose && (OpSDSLMixinCompose)instruction is { } mixinComposeInstruction)
                        {
                            var instSource = new ShaderClassSource(mixinComposeInstruction.Mixin);
                            var evaluatedSource = EvaluateEffects(instSource);

                            MergeComposition(mixinTree, mixinComposeInstruction.Identifier, evaluatedSource);
                        }
                    }

                    return mixinTree;
                }

                return classSource;
            case ShaderMixinSource mixinSource:
                var result = new ShaderMixinSource();
                foreach (var mixin in mixinSource.Mixins)
                {
                    var evaluatedMixin = EvaluateEffects(mixin);
                    Merge(result, evaluatedMixin);
                }

                foreach (var composition in mixinSource.Compositions)
                {
                    var evaluatedMixin = EvaluateEffects(composition.Value);
                    MergeComposition(result, composition.Key, evaluatedMixin);
                }

                return result;
            default:
                throw new NotImplementedException();
        }
    }

    private void MergeComposition(ShaderMixinSource mixinTree, string compositionName, ShaderSource evaluatedSource)
    {
        if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
            mixinTree.Compositions.Add(compositionName, composition = new());

        Merge(composition, evaluatedSource);
    }

    class MethodGroup
    {
        public string Name;
        public int MethodIndexInGroup;
        public ShaderInfo Shader;
        public List<(ShaderInfo Shader, int MethodId)> Methods { get; } = new();
    }

    class MixinResultGlobal
    {
        public Dictionary<int, string> Names { get; } = new();
        public Dictionary<int, SymbolType> Types { get; } = new();
    }


    class MixinResult(MixinResult? parent)
    {
        public MixinResult? Parent { get; } = parent;

        public Dictionary<string, int> MethodGroupsByName { get; } = new();

        public Dictionary<int, MethodGroup> MethodGroups { get; } = new();

        public Dictionary<int, MixinResult> Compositions { get; } = new();
    }

    private ShaderMixinSource EvaluateInheritanceAndCompositions(ShaderSource shaderSource)
    {
        var mixinsToMerge = new List<ShaderClassSource>();

        var inheritanceList = new List<ShaderClassSource>();

        var shaderMixinSource = shaderSource switch
        {
            ShaderMixinSource mixinSource2 => mixinSource2,
            ShaderClassSource classSource => new ShaderMixinSource { Mixins = { classSource } },
        };

        foreach (var mixinToMerge in shaderMixinSource.Mixins)
        {
            if (mixinToMerge.GenericArguments != null && mixinToMerge.GenericArguments.Length > 0)
                throw new NotImplementedException();

            var buffer = SpirvBuilder.GetOrLoadShader(ShaderLoader, mixinToMerge.ClassName);
            SpirvBuilder.BuildInheritanceList(ShaderLoader, buffer, inheritanceList);
            if (!inheritanceList.Contains(mixinToMerge))
                inheritanceList.Add(mixinToMerge);
        }

        shaderMixinSource.Mixins.Clear();
        shaderMixinSource.Mixins.AddRange(inheritanceList);

        foreach (var shaderName in shaderMixinSource.Mixins)
        {
            var shader = SpirvBuilder.GetOrLoadShader(ShaderLoader, shaderName.ClassName);
            ShaderClass.ProcessNameAndTypes(shader, 0, shader.Count, out var names, out var types);

            foreach (var i in shader)
            {
                if (i.Op == Op.OpVariable && (OpVariable)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
                {
                    var variableType = types[variable.ResultType];
                    if (variableType is PointerType pointer && pointer.BaseType is ShaderSymbol shaderSymbol)
                    {
                        var variableName = names[variable.ResultId];
                        // Make sure we have a ShaderMixinSource
                        // If composition is not specified, use default class
                        if (!shaderMixinSource.Compositions.TryGetValue(variableName, out var compositionMixin))
                        {
                            compositionMixin = new ShaderMixinSource { Mixins = { new ShaderClassSource(shaderSymbol.Name) } };
                        }
                        compositionMixin = (ShaderMixinSource)EvaluateInheritanceAndCompositions(compositionMixin);
                        shaderMixinSource.Compositions[variableName] = compositionMixin;
                    }
                }
            }
        }

        return shaderMixinSource;
    }

    MixinResult MergeSDSLMixin(MixinResultGlobal global, MixinResult? parent, SpirvContext context, SymbolTable table, NewSpirvBuffer temp, ShaderMixinSource mixinSource)
    {
        var mixinResult = new MixinResult(parent);

        // TODO: support proper shader mixin source
        //var shaderMixin = new ShaderMixinSource { Mixins = { new ShaderClassCode(entryShaderName) } };

        // Step: expand "for"
        // TODO

        var offset = context.Bound;
        var nextOffset = 0;

        var shaders = new List<ShaderInfo>();
        var shadersByName = new Dictionary<string, ShaderInfo>();

        var mixinStart = temp.Count;
        foreach (var shaderClass in mixinSource.Mixins)
        {
            var shader = SpirvBuilder.GetOrLoadShader(ShaderLoader, shaderClass.ClassName);
            offset += nextOffset;
            nextOffset = 0;
            shader.Header = shader.Header with { Bound = shader.Header.Bound + offset };

            var shaderStart = temp.Count;

            // Copy instructions to single buffer
            foreach (var i in shader)
            {
                var i2 = new OpData(i.Data.Memory.Span);
                temp.Add(i2);

                if (i.Data.IdResult != null && i.Data.IdResult.Value > nextOffset)
                    nextOffset = i.Data.IdResult.Value;

                if (offset > 0)
                    OffsetIds(i2, offset);
            }

            var shaderInfo = new ShaderInfo(shaders.Count);
            PopulateShaderInfo(temp, shaderStart, temp.Count, shaderInfo);
            shadersByName.Add(shaderClass.ClassName, shaderInfo);
            shaders.Add(shaderInfo);

            RemapInheritedIds(temp, shaderStart, temp.Count, shaderInfo, shadersByName);
        }

        var mixinEnd = temp.Count;

        //Console.WriteLine("Done SDSL importing");
        //Spv.Dis(temp, true);

        new TypeDuplicateRemover().Apply(temp);

        //Console.WriteLine("Done type remapping");
        //Spv.Dis(temp, true);

        context.Bound = offset + nextOffset + 1;
        //Spv.Dis(temp, true);
        var names = global.Names;
        var types = global.Types;
        ShaderClass.ProcessNameAndTypes(temp, mixinStart, mixinEnd, names, types);

        // Add symbol for each method in current type (equivalent to implicit this pointer)
        for (var index = mixinStart; index < mixinEnd; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                var functionName = names[function.ResultId];
                var symbol = new Symbol(new(functionName, SymbolKind.Method), types[function.FunctionType], function.ResultId);
                table.CurrentFrame.Add(functionName, symbol);
            }
        }

        // Build type mappings
        foreach (var type in types)
        {
            if (!context.ReverseTypes.ContainsKey(type.Key))
            {
                context.Types.Add(type.Value, type.Key);
                context.ReverseTypes.Add(type.Key, type.Value);
            }
        }

        // Build method group info (override, etc.)
        ShaderInfo? currentShader = null;
        for (var index = mixinStart; index < mixinEnd; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderInstruction)
            {
                currentShader = shadersByName[shaderInstruction.ShaderName];
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLShaderEnd)
            {
                currentShader = null;
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                if (temp[index + 1].Op == Op.OpSDSLFunctionInfo &&
                    (OpSDSLFunctionInfo)temp[index + 1] is { } functionInfo)
                {
                    var functionName = names[function.ResultId];

                    // If it's a stage method, register at the root level
                    var methodMixinResult = mixinResult;
                    if ((functionInfo.Flags & FunctionFlagsMask.Stage) != 0)
                    {
                        while (methodMixinResult.Parent != null)
                            methodMixinResult = methodMixinResult.Parent;
                    }

                    // Check if it has a parent (and if yes, share the MethodGroup)
                    if (!methodMixinResult.MethodGroups.TryGetValue(functionInfo.Parent, out var methodGroup))
                        methodGroup = new MethodGroup { Name = functionName };

                    methodGroup.Shader = currentShader;
                    methodGroup.MethodIndexInGroup = methodGroup.Methods.Count;
                    methodGroup.Methods.Add((Shader: currentShader, MethodId: function.ResultId));

                    methodMixinResult.MethodGroups[function.ResultId] = methodGroup;

                    // Also add lookup by name
                    if (!methodMixinResult.MethodGroupsByName.TryGetValue(functionName, out var methodGroups))
                        methodMixinResult.MethodGroupsByName.Add(functionName, function.ResultId);

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

        // Compositions
        foreach (var shader in shaders)
        {
            foreach (var variable in shader.Variables)
            {
                if (variable.Value.Type is PointerType pointer && pointer.BaseType is ShaderSymbol shaderSymbol)
                {
                    var compositionMixin = mixinSource.Compositions[variable.Key];
                    var compositionResult = MergeSDSLMixin(global, mixinResult, context, table, temp, compositionMixin);
                    
                    mixinResult.Compositions.Add(variable.Value.Id, compositionResult);
                }
            }
        }


        // Patch method calls (virtual calls & base calls)
        var externalShaders = new HashSet<int>();
        var externalFunctions = new Dictionary<int, string>();
        for (var index = mixinStart; index < mixinEnd; index++)
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
                if (!mixinResult.MethodGroups.TryGetValue(function.ResultId, out var methodGroupEntry))
                    throw new InvalidOperationException($"Can't find method group info for {names[function.ResultId]}");

                currentShader = methodGroupEntry.Shader;
            }
            else if (i.Data.Op == Op.OpFunctionCall && (OpFunctionCall)i is { } functionCall)
            {
                /*var methodMixinResult = mixinResult;
                if ((functionInfo.Flags & FunctionFlagsMask.Stage) != 0)
                {
                    while (methodMixinResult.Parent != null)
                        methodMixinResult = methodMixinResult.Parent;
                }*/

                var methodGroups = mixinResult.MethodGroups;

                // Process member call (composition)
                if (temp[index - 1].Op == Op.OpSDSLCallTarget)
                {
                    var callTarget = (OpSDSLCallTarget)temp[index - 1];
                    var composition = mixinResult.Compositions[callTarget.Target];
                    methodGroups = composition.MethodGroups;

                    Spv.Dis(temp, false);

                    var functionName = externalFunctions[functionCall.Function];
                    var functionId = composition.MethodGroupsByName[functionName];

                    functionCall.Function = functionId;

                    SetOpNop(temp[index - 1].Data.Memory.Span);
                }

                if (!methodGroups.TryGetValue(functionCall.Function, out var methodGroupEntry))
                    throw new InvalidOperationException($"Can't find method group info for {names[functionCall.Function]}");

                // Process base call
                if (temp[index - 1].Op == Op.OpSDSLCallBase)
                {
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
                        throw new InvalidOperationException($"Can't find a base method for {names[functionCall.Function]}");

                    SetOpNop(temp[index - 1].Data.Memory.Span);
                }
                else
                {
                    // If not, get the most derived implementation
                    functionCall.Function = methodGroupEntry.Methods[^1].MethodId;
                }
            }
        }

        for (var index = mixinStart; index < mixinEnd; index++)
        {
            var i = temp[index];
            if (i.Op == Op.OpSDSLImportShader || i.Op == Op.OpSDSLImportFunction)
            {
                SetOpNop(i.Data.Memory.Span);
            }

            if (i.Op == Op.OpTypePointer)
            {

            }
        }

        return mixinResult;
    }

    private void PopulateShaderInfo(NewSpirvBuffer temp, int shaderStart, int shaderEnd, ShaderInfo shaderInfo)
    {
        ShaderClass.ProcessNameAndTypes(temp, shaderStart, shaderEnd, out var names, out var types);

        for (var index = shaderStart; index < shaderEnd; index++)
        {
            var i = temp[index];

            if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                shaderInfo.Names.Add(nameInstruction.Target, nameInstruction.Name);
            }
            else if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                var functionName = shaderInfo.Names[function.ResultId];
                shaderInfo!.Functions.Add(functionName, function.ResultId);
            }
            else if (i.Data.Op == Op.OpVariable && (OpVariable)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
            {
                var variableName = shaderInfo.Names[variable.ResultId];
                shaderInfo!.Variables.Add(variableName, (variable.ResultId, types[variable.ResultType]));
            }
        }
    }

    private void RemapInheritedIds(NewSpirvBuffer temp, int shaderStart, int shaderEnd, ShaderInfo shaderInfo, Dictionary<string, ShaderInfo> shadersByName)
    {
        var importedShaders = new Dictionary<int, ShaderInfo>();
        var idRemapping = new Dictionary<int, int>();
        for (var index = shaderStart; index < temp.Count; index++)
        {
            var i = temp[index];

            if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (idRemapping.ContainsKey(nameInstruction.Target))
                {
                    SetOpNop(i.Data.Memory.Span);
                    shaderInfo.Names.Remove(nameInstruction.Target);
                }
            }
            else if (i.Data.Op == Op.OpSDSLMixinInherit && (OpSDSLMixinInherit)i is { } mixinInherit)
            {
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)i is { } importShader)
            {
                if (importShader.Type == ImportType.Inherit)
                {
                    importedShaders.Add(importShader.ResultId, shadersByName[importShader.ShaderName]);

                    SetOpNop(i.Data.Memory.Span);
                }
            }
            else if (i.Data.Op == Op.OpSDSLImportVariable && (OpSDSLImportVariable)i is { } importVariable)
            {
                if (importedShaders.TryGetValue(importVariable.Shader, out var importedShader))
                {
                    var importedVariable = importedShader.Variables[importVariable.VariableName];

                    idRemapping.Add(importVariable.ResultId, importedVariable.Id);

                    SetOpNop(i.Data.Memory.Span);
                }
            }
            else if (i.Data.Op == Op.OpSDSLImportFunction && (OpSDSLImportFunction)i is { } importFunction)
            {
                if (importedShaders.TryGetValue(importFunction.Shader, out var importedShader))
                {
                    var importedFunction = importedShader.Functions[importFunction.FunctionName];
                    idRemapping.Add(importFunction.ResultId, importedFunction);

                    SetOpNop(i.Data.Memory.Span);
                }
            }

            foreach (var op in i.Data)
            {
                if ((op.Kind == OperandKind.IdRef
                     || op.Kind == OperandKind.IdResultType
                     || op.Kind == OperandKind.PairIdRefLiteralInteger
                     || op.Kind == OperandKind.PairIdRefIdRef)
                    && op.Words.Length > 0
                    && idRemapping.TryGetValue(op.Words[0], out var to1))
                    op.Words[0] = to1;
                if ((op.Kind == OperandKind.PairLiteralIntegerIdRef
                     || op.Kind == OperandKind.PairIdRefIdRef)
                    && idRemapping.TryGetValue(op.Words[1], out var to2))
                    op.Words[1] = to2;
            }
        }
    }

    class ShaderInfo(int shaderIndex)
    {
        public int ShaderIndex { get; } = shaderIndex;

        public Dictionary<int, string> Names { get; } = new();


        public Dictionary<string, int> Functions { get; } = new();
        public Dictionary<string, (int Id, SymbolType Type)> Variables { get; } = new();
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
}