using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public class ShaderMixer(IExternalShaderLoader ShaderLoader)
{
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

        var shaders = new Dictionary<string, ShaderInfo>();
        ShaderInfo? currentShader = null;

        var names = new Dictionary<int, string>();
        var importedShaders = new Dictionary<int, ShaderInfo>();
        var idRemapping = new Dictionary<int, int>();
        foreach (var i in temp)
        {
            if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (idRemapping.ContainsKey(nameInstruction.Target))
                    SetOpNop(i.Data.Memory.Span);
                else
                    names.Add(nameInstruction.Target, nameInstruction.Name);
            }
            else if (i.Data.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderInstruction)
            {
                currentShader = new ShaderInfo();
                var shaderName = shaderInstruction.ShaderName;
                shaders.Add(shaderName, currentShader);
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLShaderEnd)
            {
                currentShader = null;
                importedShaders.Clear();
                SetOpNop(i.Data.Memory.Span);
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
                importedShaders.Add(importShader.ResultId, shaders[importShader.ShaderName]);

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

        var context = new SpirvContext(new());
        context.Bound = offset + nextOffset + 1;
        //Spv.Dis(temp, true);
        ShaderClass.ProcessNameAndTypes(temp, out var names2, out var types);

        Dictionary<int, int> methodOverrides = new Dictionary<int, int>();
        for (var index = 0; index < temp.Count; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                var functionName = names2[function.ResultId];
                if (!context.Module.Functions.TryGetValue(functionName, out var functions))
                    context.Module.Functions.Add(functionName, functions = new());
                functions.Add(new SpirvFunction(function.ResultId, functionName, (FunctionType)types[function.FunctionType]));

                if (temp[index + 1].Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)temp[index + 1] is {} functionInfo)
                {
                    if (functionInfo.ParentFunction != 0)
                    {
                        // TODO: better structure for more direct lookup by id?
                        // TODO: iterate to find real parent? or find it directly when computing shader?
                        methodOverrides[functionInfo.ParentFunction] = function.ResultId;
                    }

                    if ((functionInfo.Flags & FunctionFlagsMask.Abstract) != 0)
                    {
                        while (temp[index].Op != Op.OpFunctionEnd)
                        {
                            SetOpNop(temp[index++].Data.Memory.Span);
                        }
                        SetOpNop(temp[index].Data.Memory.Span);
                        // Let's go to next instruction since we erased current function
                        continue;
                    }
                }
                SetOpNop(temp[index + 1].Data.Memory.Span);
            }
        }

        foreach (var type in types)
        {
            context.Types.Add(type.Value, type.Key);
            context.ReverseTypes.Add(type.Key, type.Value);
        }

        // Patch virtual method calls
        foreach (var i in temp)
        {
            if (i.Data.Op == Op.OpFunctionCall && (OpFunctionCall)i is { } functionCall)
            {
                if (methodOverrides.TryGetValue(functionCall.Function, out var functionOverride))
                {
                    functionCall.Function = functionOverride;
                }
            }
        }

        context.Insert(0, new OpCapability(Capability.Shader));
        context.Insert(1, new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        context.Insert(2, new OpExtension("SPV_GOOGLE_hlsl_functionality1"));
        new StreamAnalyzer().Process(temp, context);

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

    class ShaderInfo
    {
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
                    o.Words[i] += offset;
            }
            else if (o.Kind == OperandKind.PairIdRefLiteralInteger
                     || o.Kind == OperandKind.PairLiteralIntegerIdRef
                     || o.Kind == OperandKind.PairIdRefIdRef)
            {
                for (int i = 0; i < o.Words.Length; i += 2)
                {
                    if (o.Kind == OperandKind.PairIdRefLiteralInteger || o.Kind == OperandKind.PairIdRefIdRef)
                        o.Words[i * 2 + 0] += offset;
                    if (o.Kind == OperandKind.PairLiteralIntegerIdRef || o.Kind == OperandKind.PairIdRefIdRef)
                        o.Words[i * 2 + 1] += offset;
                }
            }
        }
    }
}