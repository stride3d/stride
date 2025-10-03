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
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public class ShaderMixer(IExternalShaderLoader ShaderLoader)
{
    class MethodGroup
    {
        public List<(ShaderInfo Shader, int MethodId)> Methods { get; } = new();
    }

    public void MergeSDSL(string entryShaderName, out byte[] bytecode)
    {
        // TODO: support proper shader mixin source
        //var shaderMixin = new ShaderMixinSource { Mixins = { new ShaderClassCode(entryShaderName) } };

        var buffer = SpirvBuilder.GetOrLoadShader(ShaderLoader, entryShaderName);

        // Step: expand "for"
        // TODO

        // Step: build mixins: top level and (TODO) compose
        var inheritanceList = new List<string>();
        SpirvBuilder.BuildInheritanceList(ShaderLoader, buffer, inheritanceList);
        inheritanceList.Add(entryShaderName);

        var temp = new NewSpirvBuffer();
        var offset = 0;
        var nextOffset = 0;

        var table = new SymbolTable();

        foreach (var shaderName in inheritanceList)
        {
            var shader = SpirvBuilder.GetOrLoadShader(ShaderLoader, shaderName);
            offset += nextOffset;
            nextOffset = 0;
            shader.Header = shader.Header with { Bound = shader.Header.Bound + offset };
            foreach (var i in shader)
            {
                var i2 = new OpData(i.Data.Memory.Span);
                temp.Add(i2);

                if (i.Data.IdResult != null && i.Data.IdResult.Value > nextOffset)
                    nextOffset = i.Data.IdResult.Value;

                if (offset > 0)
                    OffsetIds(i2, offset);
            }
        }

        var shadersByName = new Dictionary<string, ShaderInfo>();
        var shaders = new List<ShaderInfo>();
        ShaderInfo? currentShader = null;

        var names = new Dictionary<int, string>();
        var importedShaders = new Dictionary<int, ShaderInfo>();
        var idRemapping = new Dictionary<int, int>();

        Dictionary<int, (ShaderInfo Shader, int MethodIndexInGroup, MethodGroup Group)> methodGroups = new();
        for (var index = 0; index < temp.Count; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (idRemapping.ContainsKey(nameInstruction.Target))
                    SetOpNop(i.Data.Memory.Span);
                else
                    names.Add(nameInstruction.Target, nameInstruction.Name);
            }
            else if (i.Data.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderInstruction)
            {
                currentShader = new ShaderInfo(shaders.Count);
                var shaderName = shaderInstruction.ShaderName;
                shadersByName.Add(shaderName, currentShader);
                shaders.Add(currentShader);
            }
            else if (i.Data.Op == Op.OpSDSLShaderEnd)
            {
                currentShader = null;
                importedShaders.Clear();
            }
            else if (i.Data.Op == Op.OpSDSLMixinInherit)
            {
                SetOpNop(i.Data.Memory.Span);
            }

            if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                var functionName = names[function.ResultId];
                currentShader!.Functions.Add(functionName, function.ResultId);
            }

            if (i.Data.Op == Op.OpVariable && (OpVariable)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
            {
                var variableName = names[variable.ResultId];
                currentShader!.Variables.Add(variableName, variable.ResultId);
            }

            if (i.Data.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)i is { } importShader)
            {
                importedShaders.Add(importShader.ResultId, shadersByName[importShader.ShaderName]);

                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLImportVariable && (OpSDSLImportVariable)i is { } importVariable)
            {
                var importedShader = importedShaders[importVariable.Shader];

                var importedVariable = importedShader.Variables[importVariable.VariableName];

                idRemapping.Add(importVariable.ResultId, importedVariable);

                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLImportFunction && (OpSDSLImportFunction)i is { } importFunction)
            {
                var importedShader = importedShaders[importFunction.Shader];
                var importedFunction = importedShader.Functions[importFunction.FunctionName];
                idRemapping.Add(importFunction.ResultId, importedFunction);

                SetOpNop(i.Data.Memory.Span);
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

        //Console.WriteLine("Done SDSL importing");
        //Spv.Dis(temp, true);

        // Step: merge mixins
        //       start from most-derived class and import on demand
        // Step: analyze streams and generate in/out variables

        new TypeDuplicateRemover().Apply(temp);

        //Console.WriteLine("Done type remapping");
        //Spv.Dis(temp, true);

        var context = new SpirvContext();
        context.Bound = offset + nextOffset + 1;
        //Spv.Dis(temp, true);
        ShaderClass.ProcessNameAndTypes(temp, out var names2, out var types);

        for (var index = 0; index < temp.Count; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                var functionName = names2[function.ResultId];
                var symbol = new Symbol(new(functionName, SymbolKind.Method), types[function.FunctionType], function.ResultId);
                table.CurrentFrame.Add(functionName, symbol);
            }
        }

        foreach (var type in types)
        {
            context.Types.Add(type.Value, type.Key);
            context.ReverseTypes.Add(type.Key, type.Value);
        }

        // Build method group info (override, etc.)
        for (var index = 0; index < temp.Count; index++)
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
                    // Check if it has a parent (and if yes, share the MethodGroup)
                    if (!methodGroups.TryGetValue(functionInfo.Parent, out var methodGroupEntry))
                        methodGroupEntry = (currentShader, 0, new());

                    methodGroupEntry.Shader = currentShader;
                    methodGroupEntry.MethodIndexInGroup = methodGroupEntry.Group.Methods.Count;
                    methodGroupEntry.Group.Methods.Add((Shader: currentShader, MethodId: function.ResultId));

                    methodGroups[function.ResultId] = methodGroupEntry;

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

        // Patch method calls (virtual calls & base calls)
        for (var index = 0; index < temp.Count; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                if (!methodGroups.TryGetValue(function.ResultId, out var methodGroupEntry))
                    throw new InvalidOperationException($"Can't find method group info for {names2[function.ResultId]}");

                currentShader = methodGroupEntry.Shader;
            }
            else if (i.Data.Op == Op.OpFunctionCall && (OpFunctionCall)i is { } functionCall)
            {
                if (!methodGroups.TryGetValue(functionCall.Function, out var methodGroupEntry))
                    throw new InvalidOperationException($"Can't find method group info for {names2[functionCall.Function]}");

                if (temp[index - 1].Op == Op.OpSDSLBase)
                {
                    // Is it a base call? if yes, find the direct parent
                    SetOpNop(temp[index - 1].Data.Memory.Span);

                    // Let's find the method in same group just before ours
                    bool baseMethodFound = false;
                    for (int j = methodGroupEntry.Group.Methods.Count - 1; j >= 0; --j)
                    {
                        if (methodGroupEntry.Group.Methods[j].Shader.ShaderIndex < currentShader.ShaderIndex)
                        {
                            functionCall.Function = methodGroupEntry.Group.Methods[j].MethodId;
                            baseMethodFound = true;
                            break;
                        }
                    }

                    if (!baseMethodFound)
                        throw new InvalidOperationException($"Can't find a base method for {names2[functionCall.Function]}");
                }
                else
                {
                    // If not, get the most derived implementation
                    functionCall.Function = methodGroupEntry.Group.Methods[^1].MethodId;
                }
            }
        }

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

    class ShaderInfo(int shaderIndex)
    {
        public int ShaderIndex { get; } = shaderIndex;

        public Dictionary<string, int> Functions { get; } = new();
        public Dictionary<string, int> Variables { get; } = new();
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