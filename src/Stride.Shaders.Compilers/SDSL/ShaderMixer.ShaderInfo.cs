using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Core;
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

    private void RemapInheritedIds(NewSpirvBuffer temp, int shaderStart, int shaderEnd, ShaderInfo shaderInfo, MixinNode mixinNode)
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
                if (importShader.Type == Specification.ImportType.Inherit)
                {
                    importedShaders.Add(importShader.ResultId, mixinNode.ShadersByName[importShader.ShaderName]);

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
                    if (importedShader.Functions.ContainsKey(importFunction.FunctionName))
                    {
                        var importedFunction = importedShader.Functions[importFunction.FunctionName];
                        idRemapping.Add(importFunction.ResultId, importedFunction);
                    }
                    else if (importedShader.Stage != null && importedShader.Stage.Functions.ContainsKey(importFunction.FunctionName))
                    {
                        var importedFunction = importedShader.Stage.Functions[importFunction.FunctionName];
                        idRemapping.Add(importFunction.ResultId, importedFunction);
                    }
                    else
                    {
                        // We have some cases when function is removed (i.e. stage/non-stage depending on mixin node), but import is still there.
                        // In this case, we map to 0 and make sure it's not referenced during next step.
                        idRemapping.Add(importFunction.ResultId, 0);
                    }

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
                {
                    if (to1 == 0)
                        throw new InvalidOperationException($"Tried to remap a non-existing id {op.Words[0]} at instruction {index}");
                    op.Words[0] = to1;
                }

                if ((op.Kind == OperandKind.PairLiteralIntegerIdRef
                     || op.Kind == OperandKind.PairIdRefIdRef)
                    && idRemapping.TryGetValue(op.Words[1], out var to2))
                {
                    if (to2 == 0)
                        throw new InvalidOperationException($"Tried to remap a non-existing id {op.Words[1]} at instruction {index}");
                    op.Words[1] = to2;
                }
            }
        }
    }
}