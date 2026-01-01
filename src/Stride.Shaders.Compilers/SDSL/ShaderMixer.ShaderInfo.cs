using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Globalization;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    private class ShaderInfo(int shaderIndex, string shaderName, int startInstruction, int endInstruction)
    {
        /// <summary>
        /// Index of this <see cref="ShaderInfo"/> within this <see cref="MixinNode"/>.
        /// </summary>
        public int ShaderIndex { get; } = shaderIndex;

        public string ShaderName { get; } = shaderName;

        /// <summary>
        /// The <see cref="ShaderInfo"/> for the same shader at the top-level (for all the stage members, if any).
        /// </summary>
        public ShaderInfo? Stage { get; set; }

        /// <summary>
        /// Kept for debug purpose.
        /// </summary>
        public string? CompositionPath { get; set; }

        public int StartInstruction { get; internal set; } = startInstruction;
        public int EndInstruction { get; internal set; } = endInstruction;
        public Dictionary<string, List<(int Id, FunctionType Type)>> Functions { get; } = new();
        public Dictionary<string, (int Id, SymbolType Type)> Variables { get; } = new();

        public Dictionary<string, int> StructTypes { get; } = new();

        public (int Id, SymbolType Type) FindMember(string name, FunctionType? functionType = null)
        {
            if (Functions.TryGetValue(name, out var functions))
            {
                foreach (var function in functions)
                {
                    if (function.Type == functionType)
                        return (function.Id, function.Type);
                }
            }
            if (Variables.TryGetValue(name, out var variable))
                return (variable.Id, variable.Type);
            throw new KeyNotFoundException($"Member {name} was not found in shader {ShaderName}");
        }

        public override string ToString() => $"{ShaderName} ({(CompositionPath != null ? $" {CompositionPath} " : "")}{StartInstruction}..{EndInstruction})";
    }

    private void PopulateShaderInfo(MixinGlobalContext globalContext, SpirvContext context, int contextStart, int contextEnd, NewSpirvBuffer buffer, int shaderStart, int shaderEnd, ShaderInfo shaderInfo, MixinNode mixinNode)
    {
        var removedIds = new HashSet<int>();
        for (var index = shaderStart; index < shaderEnd; index++)
        {
            var i = buffer[index];

            if (i.Data.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                var functionName = context.Names[function.ResultId];
                var functionType = (FunctionType)context.ReverseTypes[function.FunctionType];
                if (!shaderInfo!.Functions.TryGetValue(functionName, out var functions))
                    shaderInfo.Functions.Add(functionName, functions = new());
                functions.Add((function.ResultId, functionType));
            }
            else if (i.Data.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
            {
                var variableName = context.Names[variable.ResultId];
                var variableType = context.ReverseTypes[variable.ResultType];
                shaderInfo!.Variables.Add(variableName, (variable.ResultId, variableType));

                // Remove SPIR-V variables to other shaders (already stored in ShaderInfo and not valid SPIR-V)
                if (variableType is PointerType pointer && pointer.BaseType is (ShaderSymbol or ArrayType { BaseType: ShaderSymbol }))
                {
                    SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(variable.ResultId);
                }
            }
        }

        // Second pass to remove OpName
        for (var index = contextStart; index < contextEnd; index++)
        {
            var i = context.GetBuffer()[index];

            if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (removedIds.Contains(nameInstruction.Target))
                    SetOpNop(i.Data.Memory.Span);
            }
        }
    }

    private void ProcessImportInfo(MixinGlobalContext globalContext, MixinNode mixinNode, ref OpData i, SpirvContext context)
    {
        if (i.Op == Op.OpSDSLImportShader && new OpSDSLImportShader(ref i) is { } importShader)
        {
            // TODO: some common code to generate name, so that it doesn't deviate from ToClassName() called later when doing ShadersByName lookups
            var shaderName = importShader.ShaderName;
            if (importShader.Values.Elements.Length > 0)
            {
                var genericArguments = new object[importShader.Values.Elements.Length];
                for (int j = 0; j < genericArguments.Length; j++)
                {
                    genericArguments[j] = context.GetConstantValue(importShader.Values.Elements.Span[j]);
                }
                shaderName += $"<{string.Join(",", genericArguments)}>";
            }

            globalContext.ExternalShaders.Add(importShader.ResultId, shaderName);
        }
        else if (i.Op == Op.OpSDSLImportFunction && new OpSDSLImportFunction(ref i) is { } importFunction)
        {
            if (globalContext.ExternalShaders.ContainsKey(importFunction.Shader))
            {
                globalContext.ExternalFunctions.Add(importFunction.ResultId, (importFunction.Shader, importFunction.FunctionName, importFunction.FunctionType));
            }
        }
        else if (i.Op == Op.OpSDSLImportVariable && new OpSDSLImportVariable(ref i) is { } importVariable)
        {
            if (globalContext.ExternalShaders.ContainsKey(importVariable.Shader))
            {
                globalContext.ExternalVariables.Add(importVariable.ResultId, (importVariable.Shader, importVariable.VariableName));
            }
        }
    }
}