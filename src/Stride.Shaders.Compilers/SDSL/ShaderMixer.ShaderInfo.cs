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

        public int StartInstruction { get; } = startInstruction;
        public int EndInstruction { get; } = endInstruction;
        public Dictionary<int, string> Names { get; } = new();
        public Dictionary<string, int> Functions { get; } = new();
        public Dictionary<string, (int Id, SymbolType Type)> Variables { get; } = new();
        public Dictionary<string, int> StructTypes { get; } = new();

        public override string ToString() => $"{ShaderName} ({(CompositionPath != null ? $" {CompositionPath} " : "")}{StartInstruction}..{EndInstruction})";
    }

    private void PopulateShaderInfo(NewSpirvBuffer temp, int shaderStart, int shaderEnd, ShaderInfo shaderInfo, MixinNode mixinNode)
    {
        ShaderClass.ProcessNameAndTypes(temp, shaderStart, shaderEnd, out var names, out var types);
        var removedIds = new HashSet<int>();
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
                var variableType = types[variable.ResultType];
                shaderInfo!.Variables.Add(variableName, (variable.ResultId, variableType));

                // Remove SPIR-V variables to other shaders (already stored in ShaderInfo and not valid SPIR-V)
                if (variableType is PointerType pointer && pointer.BaseType is ShaderSymbol shaderSymbol)
                {
                    SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(variable.ResultId);
                }
            }
            else if (i.Data.Op == Op.OpTypePointer && (OpTypePointer)i is { } typePointer)
            {
                // Remove SPIR-V about pointer types to other shaders (variable and types themselves are removed as well)
                var pointedType = types[typePointer.Type];
                if (pointedType is ShaderSymbol)
                {
                    SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(typePointer.ResultId);
                }
            }
            else if (i.Data.Op == Op.OpTypeStruct && (OpTypeStruct)i is { } typeStruct)
            {
                var structName = shaderInfo.Names[typeStruct];
                shaderInfo!.StructTypes.Add(structName, typeStruct.ResultId);
            }
        }

        // Second pass to remove OpName
        for (var index = shaderStart; index < shaderEnd; index++)
        {
            var i = temp[index];

            if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (removedIds.Contains(nameInstruction.Target))
                    SetOpNop(i.Data.Memory.Span);
            }
        }
    }

    private void BuildImportInfo(NewSpirvBuffer temp, int shaderStart, int shaderEnd, ShaderClassInstantiation classSource, ShaderInfo shaderInfo, MixinNode mixinNode)
    {
        var inheritedShaders = new HashSet<int>();
        for (var index = shaderStart; index < temp.Count; index++)
        {
            var i = temp[index];
            if (i.Data.Op == Op.OpSDSLMixinInherit && (OpSDSLMixinInherit)i is { } mixinInherit)
            {
                inheritedShaders.Add(mixinInherit.Shader);
                SetOpNop(i.Data.Memory.Span);
            }
        }

        for (var index = shaderStart; index < temp.Count; index++)
        {
            var i = temp[index];

            if (i.Data.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)i is { } importShader)
            {
                mixinNode.ExternalShaders.Add(importShader.ResultId, importShader.ShaderName);
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLImportFunction && (OpSDSLImportFunction)i is { } importFunction)
            {
                if (mixinNode.ExternalShaders.ContainsKey(importFunction.Shader))
                {
                    mixinNode.ExternalFunctions.Add(importFunction.ResultId, (importFunction.Shader, importFunction.FunctionName));
                    SetOpNop(i.Data.Memory.Span);
                }
            }
            else if (i.Data.Op == Op.OpSDSLImportVariable && (OpSDSLImportVariable)i is { } importVariable)
            {
                if (mixinNode.ExternalShaders.ContainsKey(importVariable.Shader))
                {
                    mixinNode.ExternalVariables.Add(importVariable.ResultId, (importVariable.Shader, importVariable.VariableName));
                    SetOpNop(i.Data.Memory.Span);
                }
            }
            // Removing OpName for OpSDSLImportShader and OpSDSLImportFunction (they are always located after, so no problem to do it in a single pass)
            else if (i.Data.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (mixinNode.ExternalShaders.ContainsKey(nameInstruction.Target)
                    || mixinNode.ExternalFunctions.ContainsKey(nameInstruction.Target)
                    || mixinNode.ExternalVariables.ContainsKey(nameInstruction.Target))
                {
                    SetOpNop(i.Data.Memory.Span);
                }
            }
        }
    }
}