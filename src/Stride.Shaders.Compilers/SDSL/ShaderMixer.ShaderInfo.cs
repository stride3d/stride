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
        public Dictionary<int, string> Names { get; } = new();
        public Dictionary<string, (int Id, FunctionType Type)> Functions { get; } = new();
        public Dictionary<string, (int Id, SymbolType Type)> Variables { get; } = new();

        public Dictionary<string, int> StructTypes { get; } = new();

        public (int Id, SymbolType Type) FindMember(string name)
        {
            if (Functions.TryGetValue(name, out var function))
                return (function.Id, function.Type);
            if (Variables.TryGetValue(name, out var variable))
                return (variable.Id, variable.Type);
            throw new KeyNotFoundException($"Member {name} was not found in shader {ShaderName}");
        }

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
                var functionType = (FunctionType)types[function.FunctionType];
                shaderInfo!.Functions.Add(functionName, (function.ResultId, functionType));
            }
            else if (i.Data.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
            {
                var variableName = shaderInfo.Names[variable.ResultId];
                var variableType = types[variable.ResultType];
                shaderInfo!.Variables.Add(variableName, (variable.ResultId, variableType));

                // Remove SPIR-V variables to other shaders (already stored in ShaderInfo and not valid SPIR-V)
                if (variableType is PointerType pointer && pointer.BaseType is (ShaderSymbol or ArrayType { BaseType: ShaderSymbol }))
                {
                    SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(variable.ResultId);
                }
            }
            // Remove SPIR-V about pointer types to other shaders (variable and types themselves are removed as well)
            else if (i.Data.Op == Op.OpTypePointer && (OpTypePointer)i is { } typePointer)
            {
                var pointedType = types[typePointer.Type];
                if (pointedType is ShaderSymbol || pointedType is ArrayType { BaseType: ShaderSymbol })
                {
                    SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(typePointer.ResultId);
                }
            }
            // Also remove arrays of shaders (used in composition arrays)
            else if (i.Data.Op == Op.OpTypeArray && (OpTypeArray)i is { } typeArray)
            {
                var innerType = types[typeArray.ElementType];
                if (innerType is ShaderSymbol)
                {
                    SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(typeArray.ResultId);
                }
            }
            else if (i.Data.Op == Op.OpTypeRuntimeArray && (OpTypeRuntimeArray)i is { } typeRuntimeArray)
            {
                var innerType = types[typeRuntimeArray.ElementType];
                if (innerType is ShaderSymbol)
                {
                    SetOpNop(i.Data.Memory.Span);
                    removedIds.Add(typeRuntimeArray.ResultId);
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
                // TODO: some common code to generate name, so that it doesn't deviate from ToClassName() called later when doing ShadersByName lookups
                var shaderName = importShader.ShaderName;
                if (importShader.Values.Elements.Length > 0)
                {
                    var genericArguments = new object[importShader.Values.Elements.Length];
                    for (int j = 0; j < genericArguments.Length; j++)
                    {
                        genericArguments[j] = SpirvBuilder.GetConstantValue(importShader.Values.Elements.Span[j], temp);
                    }
                    shaderName += $"<{string.Join(",", genericArguments)}>";
                }

                mixinNode.ExternalShaders.Add(importShader.ResultId, shaderName);
            }
            else if (i.Data.Op == Op.OpSDSLImportFunction && (OpSDSLImportFunction)i is { } importFunction)
            {
                if (mixinNode.ExternalShaders.ContainsKey(importFunction.Shader))
                {
                    mixinNode.ExternalFunctions.Add(importFunction.ResultId, (importFunction.Shader, importFunction.FunctionName));
                }
            }
            else if (i.Data.Op == Op.OpSDSLImportVariable && (OpSDSLImportVariable)i is { } importVariable)
            {
                if (mixinNode.ExternalShaders.ContainsKey(importVariable.Shader))
                {
                    mixinNode.ExternalVariables.Add(importVariable.ResultId, (importVariable.Shader, importVariable.VariableName));
                }
            }
        }
    }
}