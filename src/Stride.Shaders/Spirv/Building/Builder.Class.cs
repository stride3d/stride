using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public record class ShaderMixinInstantiation(List<ShaderClassInstantiation> Mixins, Dictionary<string, ShaderMixinInstantiation[]> Compositions);

public enum ResolveStep
{
    Compile,
    Mix,
}

public record class ShaderClassInstantiation(string ClassName, int[] GenericArguments, bool ImportStageOnly = false) : IEquatable<ShaderClassInstantiation>
{
    public NewSpirvBuffer Buffer { get; set; }

    public string ClassName { get; set; } = ClassName;

    public int[] GenericArguments { get; set; } = GenericArguments;

    public LoadedShaderSymbol Symbol { get; set; }

    public int Start { get; set; }
    public int End { get; set; }

    public int OffsetId { get; set; }

    public string ToClassName()
    {
        if ((GenericArguments == null || GenericArguments.Length == 0) && !ImportStageOnly)
            return ClassName;

        var result = new StringBuilder();
        result.Append(ClassName);
        if (GenericArguments != null && GenericArguments.Length > 0)
        {
            result.Append('<');
            result.Append(string.Join(",", GenericArguments));
            result.Append('>');
        }

        return result.ToString();
    }

    public override string ToString() => $"{(ImportStageOnly ? "stage " : string.Empty)}{ToClassName()} Symbol: {Symbol} Buffer: {(Buffer != null ? "set" : "empty")} Start: {Start} End: {End} OffsetId: {OffsetId}";

    public virtual bool Equals(ShaderClassInstantiation? shaderClassSource)
    {
        if (shaderClassSource is null) return false;
        if (ReferenceEquals(this, shaderClassSource)) return true;
        return
            string.Equals(ClassName, shaderClassSource.ClassName) &&
            GenericArguments.SequenceEqual(shaderClassSource.GenericArguments);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = ClassName?.GetHashCode() ?? 0;
            if (GenericArguments != null)
            {
                foreach (var current in GenericArguments)
                    hashCode = (hashCode * 397) ^ (current.GetHashCode());
            }

            return hashCode;
        }
    }
}

public partial class SpirvBuilder
{
    private static void BuildInheritanceListHelper(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, NewSpirvBuffer buffer, List<ShaderClassInstantiation> inheritanceList, ResolveStep resolveStep)
    {
        // Build shader name mapping
        var shaderMapping = new Dictionary<int, ShaderClassInstantiation>();
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)i is { } importShader)
            {
                var shaderClassSource = ConvertToShaderClassSource(buffer, 0, buffer.Count, importShader);

                shaderMapping[importShader.ResultId] = shaderClassSource;
            }
        }

        // Check inheritance
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpSDSLMixinInherit && (OpSDSLMixinInherit)i is { } inherit)
            {
                var shaderName = shaderMapping[inherit.Shader];
                BuildInheritanceList(shaderLoader, shaderName, inheritanceList, resolveStep, buffer);
            }
        }
    }

    public static ShaderClassInstantiation ConvertToShaderClassSource(NewSpirvBuffer buffer, int shaderStart, int shaderEnd, OpSDSLImportShader importShader)
    {
        return new ShaderClassInstantiation(importShader.ShaderName, importShader.Values.Elements.Memory.ToArray());
    }

    public static ShaderClassInstantiation BuildInheritanceList(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, List<ShaderClassInstantiation> inheritanceList, ResolveStep resolveStep, NewSpirvBuffer? parentBuffer = null)
    {
        // TODO: cache same instantiations within context?
        var index = inheritanceList.IndexOf(classSource);
        if (index == -1)
        {
            if (classSource.Buffer == null)
            {
                var shader = GetOrLoadShader(shaderLoader, classSource, resolveStep, parentBuffer);
                classSource.Buffer = shader;
            }

            // Note: since shader instantiation might mutate classSource, perform a search again
            index = inheritanceList.IndexOf(classSource);
            if (index == -1)
            {
                BuildInheritanceListHelper(shaderLoader, classSource, classSource.Buffer, inheritanceList, resolveStep);
                index = inheritanceList.Count;
                inheritanceList.Add(classSource);
            }
        }

        return inheritanceList[index];
    }

    public static object GetConstantValue(int constantId, params ReadOnlySpan<NewSpirvBuffer> buffers)
    {
        foreach (var buffer in buffers)
        {
            if (buffer.TryGetInstructionById(constantId, out var constant))
            {
                return GetConstantValue(constant.Data, buffers);
            }
        }

        throw new Exception("Cannot find constant instruction for id " + constantId);
    }

    public static object GetConstantValue(OpData data, params ReadOnlySpan<NewSpirvBuffer> buffers)
    {
        int typeId = data.Op switch
        {
            Op.OpConstant or Op.OpSpecConstant => data.Memory.Span[1],
            _ => throw new Exception("Unsupported context dependent number in instruction " + data.Op)
        };
        var operand = data.Get("value");
        foreach (var buffer in buffers)
        {
            if (buffer.TryGetInstructionById(typeId, out var typeInst))
            {
                if (typeInst.Op == Op.OpTypeInt)
                {
                    var type = (OpTypeInt)typeInst;
                    return type switch
                    {
                        { Width: <= 32, Signedness: 0 } => operand.ToLiteral<uint>(),
                        { Width: <= 32, Signedness: 1 } => operand.ToLiteral<int>(),
                        { Width: 64, Signedness: 0 } => operand.ToLiteral<ulong>(),
                        { Width: 64, Signedness: 1 } => operand.ToLiteral<long>(),
                        _ => throw new NotImplementedException("Unsupported int width " + type.Width),
                    };
                }
                else if (typeInst.Op == Op.OpTypeFloat)
                {
                    var type = new OpTypeFloat(typeInst);
                    return type switch
                    {
                        { Width: 16 } => operand.ToLiteral<Half>(),
                        { Width: 32 } => operand.ToLiteral<float>(),
                        { Width: 64 } => operand.ToLiteral<double>(),
                        _ => throw new NotImplementedException("Unsupported float width " + type.Width),
                    };
                }
                else
                    throw new NotImplementedException("Unsupported context dependent number with type " + typeInst.Op);
            }
        }
        throw new Exception("Cannot find type instruction for id " + typeId);
    }

    private static void InstantiateGenericShaderUsingGenericValues(NewSpirvBuffer shader, string className, string[] genericValues)
    {
        Console.WriteLine($"[Shader] Instantiating {className} with values {string.Join(",", genericValues)}");

        ShaderClass.ProcessNameAndTypes(shader, 0, shader.Count, out var names, out var types);

        var bound = shader.Header.Bound;

        var genericValueIndex = 0;
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is { } genericParameter)
            {
                var genericValue = genericValues[genericValueIndex++];
                var type = types[genericParameter.ResultType];
                switch (type)
                {
                    case ScalarType { TypeName: "int" }:
                        shader.Replace(index, new OpConstant<int>(genericParameter.ResultType, genericParameter.ResultId, int.Parse(genericValue)));
                        break;
                    case ScalarType { TypeName: "float" }:
                        shader.Replace(index, new OpConstant<float>(genericParameter.ResultType, genericParameter.ResultId, float.Parse(genericValue)));
                        break;
                    case ScalarType { TypeName: "bool" }:
                        if (bool.Parse(genericValue))
                            shader.Replace(index, new OpConstantTrue(genericParameter.ResultType, genericParameter.ResultId));
                        else
                            shader.Replace(index, new OpConstantFalse(genericParameter.ResultType, genericParameter.ResultId));
                        break;
                    case GenericLinkType:
                        shader.Replace(index, new OpConstantStringSDSL(genericParameter.ResultId, genericValue));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderDeclaration)
            {
                shaderDeclaration.ShaderName += $"<{string.Join(',', genericValues)}>";
            }
        }

        // In case we had to increase bound (new instructions), update header
        shader.Header = shader.Header with { Bound = bound };
    }

    private static void InstantiateGenericShaderUsingParentBuffer(NewSpirvBuffer shader, ShaderClassInstantiation classSource, ResolveStep resolveStep, NewSpirvBuffer instantiatingBuffer)
    {
        Console.WriteLine($"[Shader] Instantiating {classSource.ClassName} with parameters {string.Join(",", classSource.GenericArguments.Select(x => $"%{x}"))}");
        Console.WriteLine($"[Shader] Instantiating from buffer generics {instantiatingBuffer[0].Data}:");
        foreach (var i in instantiatingBuffer)
        {
            if (i.Data.IdResult is int id && classSource.GenericArguments.Contains(id))
            {
                Console.WriteLine($" - [{classSource.GenericArguments.IndexOf(id)}] %{id} => {i.Data}");
            }
        }

        // Map classSource.GenericArguments ids to OpSDSLGenericParameter.ResultId (in the order OpSDSLGenericParameter appears)
        Dictionary<int, List<int>> targets = new();

        // Collect OpSDSLGenericParameter
        List<int> generics = new();
        var genericArgumentIndex = 0;
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is { } genericParameter)
            {
                generics.Add(genericParameter.ResultId);
                if (!targets.TryGetValue(classSource.GenericArguments[genericArgumentIndex], out var genericParametersForThisArgument))
                    targets.Add(classSource.GenericArguments[genericArgumentIndex], genericParametersForThisArgument = new());
                genericParametersForThisArgument.Add(genericParameter.ResultId);
                genericArgumentIndex++;
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderDeclaration)
            {
                shaderDeclaration.ShaderName = classSource.ToClassName();
            }
        }

        // Try to resolve fully the new generic parameter values
        // Any parameter resolved will be stored in Dictionary<int, string> with the string version of the parameter value)
        var resolvedParameters = new Dictionary<int, string>();

        if (instantiatingBuffer != null)
        {
            for (var index = 0; index < instantiatingBuffer.Count; index++)
            {
                var i = instantiatingBuffer[index];
                if (i.Op == Op.OpConstant || i.Op == Op.OpConstantTrue || i.Op == Op.OpConstantFalse)
                {
                    if (targets.TryGetValue(i.Data.IdResult!.Value, out var parameters))
                    {
                        var value = i.Op switch
                        {
                            Op.OpConstant => GetConstantValue(i.Data, instantiatingBuffer),
                            Op.OpConstantTrue => bool.TrueString.ToLowerInvariant(),
                            Op.OpConstantFalse => bool.FalseString.ToLowerInvariant(),
                        };

                        // import constant in current shader
                        foreach (var parameter in parameters)
                        {
                            resolvedParameters.Add(parameter, value.ToString());
                            var i2 = new OpData(i.Data.Memory.Span);
                            i2.IdResult = parameter;
                            shader.Add(i2);
                        }
                    }
                }
                else if (i.Op == Op.OpConstantStringSDSL && (OpConstantStringSDSL)i is { } constantString)
                {
                    if (targets.TryGetValue(i.Data.IdResult!.Value, out var parameters))
                    {
                        var value = constantString.LiteralString;
                        // This will be used later for resolving LinkType generics
                        foreach (var parameter in parameters)
                            resolvedParameters.Add(parameter, value);
                    }
                }
                else if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is { } genericParameter)
                {
                    // Unresolved parameter, keep as is
                }
            }
        }

        // Try to resolve LinkType generics
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpMemberDecorateString
                && ((OpMemberDecorateString)i) is { Decoration: { Value: Decoration.LinkIdSDSL, Parameters: { } m } } linkDecorate)
            {
                using var n = new LiteralValue<int>(m.Span);
                if (resolvedParameters.TryGetValue(n.Value, out var resolvedValue))
                {
                    linkDecorate.Decoration = new ParameterizedFlag<Decoration>(Decoration.LinkSDSL, [.. resolvedValue.AsDisposableLiteralValue().Words]);
                }
            }
        }

        // Fully resolved?
        if (resolvedParameters.Count == generics.Count)
        {
            var parameters = string.Join(',', generics.Select(x => resolvedParameters[x]));
            var className = classSource.ClassName + "<" + parameters + ">";

            if (resolveStep == ResolveStep.Mix)
            {
                classSource.ClassName = className;
                classSource.GenericArguments = [];
            }

            for (var index = 0; index < shader.Count; index++)
            {
                var i = shader[index];
                if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderDeclaration)
                {
                    shaderDeclaration.ShaderName = className;
                }
            }
        }
        else if (resolveStep == ResolveStep.Mix)
        {
            throw new InvalidOperationException("During mix phase, shaders generics are expected to be fully resolved");
        }
    }

    public static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }

    public static bool ContainIds(HashSet<int> ids, OpData i)
    {
        foreach (var op in i)
        {
            if ((op.Kind == OperandKind.IdRef
                 || op.Kind == OperandKind.IdResult
                 || op.Kind == OperandKind.IdResultType
                 || op.Kind == OperandKind.PairIdRefLiteralInteger
                 || op.Kind == OperandKind.PairIdRefIdRef)
                && op.Words.Length > 0
                && ids.Contains(op.Words[0]))
            {
                return true;
            }

            if ((op.Kind == OperandKind.PairLiteralIntegerIdRef
                 || op.Kind == OperandKind.PairIdRefIdRef)
                && ids.Contains(op.Words[1]))
            {
                return true;
            }
        }

        return false;
    }

    public static void RemapIds(NewSpirvBuffer buffer, int shaderStart, int shaderEnd, Dictionary<int, int> idRemapping)
    {
        for (var index = shaderStart; index < buffer.Count; index++)
        {
            var i = buffer[index];
            RemapIds(idRemapping, i.Data);
        }
    }

    public static void RemapIds(Dictionary<int, int> idRemapping, OpData i)
    {
        foreach (var op in i)
        {
            if ((op.Kind == OperandKind.IdRef
                 || op.Kind == OperandKind.IdResult
                 || op.Kind == OperandKind.IdResultType
                 || op.Kind == OperandKind.PairIdRefLiteralInteger
                 || op.Kind == OperandKind.PairIdRefIdRef)
                && op.Words.Length > 0
                && idRemapping.TryGetValue(op.Words[0], out var to1))
            {
                op.Words[0] = to1;
            }

            if ((op.Kind == OperandKind.PairLiteralIntegerIdRef
                 || op.Kind == OperandKind.PairIdRefIdRef)
                && idRemapping.TryGetValue(op.Words[1], out var to2))
            {
                op.Words[1] = to2;
            }
        }
    }

    /// <summary>
    /// Gets or load a shader, with generic instantiation (if requested).
    /// </summary>
    /// <param name="shaderLoader"></param>
    /// <param name="classSource">The generics parameters should be in <see cref="parentBuffer"/>.</param>
    /// <param name="resolveStep"></param>
    /// <param name="parentBuffer"></param>
    /// <returns></returns>
    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, ResolveStep resolveStep, NewSpirvBuffer parentBuffer)
    {
        var shader = GetOrLoadShader(shaderLoader, classSource.ClassName, out var isFromCache);

        if (!isFromCache)
            Spv.Dis(shader, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);

        if (classSource.GenericArguments.Length > 0)
        {
            // Copy shader
            shader = CopyShader(shader);

            InstantiateGenericShaderUsingParentBuffer(shader, classSource, resolveStep, parentBuffer);
            Spv.Dis(shader, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        }

        return shader;
    }

    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string className, string[] genericValues)
    {
        var shader = GetOrLoadShader(shaderLoader, className, out var isFromCache);

        if (!isFromCache)
            Spv.Dis(shader, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);

        if (genericValues != null && genericValues.Length > 0)
        {
            // Copy shader
            shader = CopyShader(shader);

            InstantiateGenericShaderUsingGenericValues(shader, className, genericValues);
            Spv.Dis(shader, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        }

        return shader;
    }

    public static NewSpirvBuffer CopyShader(NewSpirvBuffer shader)
    {
        var copiedShader = new NewSpirvBuffer();
        foreach (var i in shader)
        {
            var i2 = new OpData(i.Data.Memory.Span);
            copiedShader.Add(i2);
        }
        shader = copiedShader;
        return shader;
    }

    public static List<int> CollectGenerics(NewSpirvBuffer shader)
    {
        // Collect OpSDSLGenericParameter
        List<int> generics = new();
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is { } genericParameter)
            {
                generics.Add(genericParameter.ResultId);
                SetOpNop(i.Data.Memory.Span);
            }
        }

        return generics;
    }

    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string className, out bool isFromCache)
    {
        Console.WriteLine($"[Shader] Requesting non-generic class {className}");

        if (!shaderLoader.LoadExternalBuffer(className, out var buffer, out isFromCache))
            throw new InvalidOperationException($"Could not load shader [{className}]");

        if (!isFromCache)
            Console.WriteLine($"[Shader] Loading non-generic class {className} for 1st time");

        return buffer;
    }
}
