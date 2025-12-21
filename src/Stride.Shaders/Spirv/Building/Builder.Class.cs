using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    public string ToClassName()
    {
        if ((GenericArguments == null || GenericArguments.Length == 0) && !ImportStageOnly)
            return ClassName;

        var result = new StringBuilder();
        result.Append(ClassName);
        if (GenericArguments != null && GenericArguments.Length > 0)
        {
            result.Append('<');
            result.Append(string.Join(",", GenericArguments.Select(x => $"%{x}")));
            result.Append('>');
        }

        return result.ToString();
    }

    public override string ToString() => $"{(ImportStageOnly ? "stage " : string.Empty)}{ToClassName()} Symbol: {Symbol} Buffer: {(Buffer != null ? "set" : "empty")}";

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
    private static void BuildInheritanceListHelper(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, ReadOnlySpan<ShaderMacro> macros, NewSpirvBuffer buffer, List<ShaderClassInstantiation> inheritanceList, ResolveStep resolveStep)
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
                BuildInheritanceList(shaderLoader, shaderName, macros, inheritanceList, resolveStep, buffer);
            }
        }
    }

    public static ShaderClassInstantiation ConvertToShaderClassSource(NewSpirvBuffer buffer, int shaderStart, int shaderEnd, OpSDSLImportShader importShader)
    {
        return new ShaderClassInstantiation(importShader.ShaderName, importShader.Values.Elements.Memory.ToArray());
    }

    public static ShaderClassInstantiation BuildInheritanceList(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, ReadOnlySpan<ShaderMacro> macros, List<ShaderClassInstantiation> inheritanceList, ResolveStep resolveStep, NewSpirvBuffer? parentBuffer = null)
    {
        // TODO: cache same instantiations within context?
        var index = inheritanceList.IndexOf(classSource);
        if (index == -1)
        {
            if (classSource.Buffer == null)
            {
                var shader = GetOrLoadShader(shaderLoader, classSource, macros, resolveStep, parentBuffer);
                classSource.Buffer = shader;
            }

            // Note: since shader instantiation might mutate classSource, perform a search again
            index = inheritanceList.IndexOf(classSource);
            if (index == -1)
            {
                BuildInheritanceListHelper(shaderLoader, classSource, macros, classSource.Buffer, inheritanceList, resolveStep);
                index = inheritanceList.Count;
                inheritanceList.Add(classSource);
            }
        }

        return inheritanceList[index];
    }

    public static bool TryGetInstructionById(int constantId, out OpDataIndex instruction, params ReadOnlySpan<NewSpirvBuffer> buffers)
    {
        foreach (var buffer in buffers)
        {
            if (buffer.TryGetInstructionById(constantId, out instruction))
                return true;
        }

        instruction = default;
        return false;
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

    public static bool TryGetConstantValue(int constantId, out object value, params ReadOnlySpan<NewSpirvBuffer> buffers)
    {
        foreach (var buffer in buffers)
        {
            if (buffer.TryGetInstructionById(constantId, out var constant))
            {
                return TryGetConstantValue(constant.Data, out value, buffers);
            }
        }

        value = default;
        return false;
    }

    public static object GetConstantValue(OpData data, params ReadOnlySpan<NewSpirvBuffer> buffers)
    {
        if (!TryGetConstantValue(data, out var value, buffers))
            throw new InvalidOperationException($"Can't process constant {data.IdResult}");

        return value;
    }

    // Note: this will return false if constant can't be resolved yet (i.e. due to unresolved generics). If it is not meant to become a constant (even later), behavior is undefined.
    public static bool TryGetConstantValue(OpData data, out object value, params ReadOnlySpan<NewSpirvBuffer> buffers)
    {
        // Check for unresolved values
        if (data.Op == Op.OpSDSLGenericParameter)
        {
            value = default;
            return false;
        }

        if (data.Op == Op.OpConstantStringSDSL)
        {
            var operand2 = data.Get("literalString");
            value = operand2.ToLiteral<string>();
            return true;
        }

        int typeId = data.Op switch
        {
            Op.OpConstant or Op.OpSpecConstant => data.Memory.Span[1],
        };
        var operand = data.Get("value");
        foreach (var buffer in buffers)
        {
            if (buffer.TryGetInstructionById(typeId, out var typeInst))
            {
                if (typeInst.Op == Op.OpTypeInt)
                {
                    var type = (OpTypeInt)typeInst;
                    value = type switch
                    {
                        { Width: <= 32, Signedness: 0 } => operand.ToLiteral<uint>(),
                        { Width: <= 32, Signedness: 1 } => operand.ToLiteral<int>(),
                        { Width: 64, Signedness: 0 } => operand.ToLiteral<ulong>(),
                        { Width: 64, Signedness: 1 } => operand.ToLiteral<long>(),
                        _ => throw new NotImplementedException($"Unsupported int width {type.Width}"),
                    };
                    return true;
                }
                else if (typeInst.Op == Op.OpTypeFloat)
                {
                    var type = new OpTypeFloat(typeInst);
                    value = type switch
                    {
                        { Width: 16 } => operand.ToLiteral<Half>(),
                        { Width: 32 } => operand.ToLiteral<float>(),
                        { Width: 64 } => operand.ToLiteral<double>(),
                        _ => throw new NotImplementedException($"Unsupported float width {type.Width}"),
                    };
                    return true;
                }
                else
                    throw new NotImplementedException($"Unsupported context dependent number with type {typeInst.Op}");
            }
        }
        throw new Exception("Cannot find type instruction for id " + typeId);
    }

    record struct GenericParameter(SymbolType Type, int ResultId, int ResultType, int Index, string Name, bool Resolved, object Value);

    abstract class GenericResolver
    {
        public abstract bool NeedsResolve();
        public abstract bool TryResolveGenericValue(SymbolType genericParameterType, string genericParameterName, int index, out object value);

        public virtual void PostProcess(string classNameWithGenerics, List<GenericParameter> genericParameters)
        {
        }
    }

    class GenericResolverFromValues(string[]? genericValues) : GenericResolver
    {
        public override bool NeedsResolve() => genericValues != null && genericValues.Length > 0;

        public override bool TryResolveGenericValue(SymbolType genericParameterType, string genericParameterName, int index, out object value)
        {
            var genericValue = genericValues![index];
            switch (genericParameterType)
            {
                case ScalarType { TypeName: "int" }:
                    value = int.Parse(genericValue);
                    return true;
                case ScalarType { TypeName: "float" }:
                    value = float.Parse(genericValue);
                    return true;
                case ScalarType { TypeName: "bool" }:
                    value = bool.Parse(genericValue);
                    return true;
                case GenericParameterType g:
                    value = genericValue;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    class GenericResolverFromClassInstantiation(ShaderClassInstantiation classSource, NewSpirvBuffer instantiatingBuffer, ResolveStep resolveStep) : GenericResolver
    {
        private Dictionary<int, string> names;

        public override bool NeedsResolve() => classSource.GenericArguments.Length > 0;

        public override bool TryResolveGenericValue(SymbolType genericParameterType, string genericParameterName, int index, out object value)
        {
            if (!TryGetConstantValue(classSource.GenericArguments[index], out value, instantiatingBuffer))
            {
                if (names == null)
                    ShaderClass.ProcessNameAndTypes(instantiatingBuffer, 0, instantiatingBuffer.Count, out names, out _);

                value = names.TryGetValue(classSource.GenericArguments[index], out var genericArgumentName)
                    ? $"%{genericArgumentName}[{classSource.GenericArguments[index]}]"
                    : $"%{classSource.GenericArguments[index]}";
                return false;
            }

            return true;
        }

        public override void PostProcess(string classNameWithGenerics, List<GenericParameter> genericParameters)
        {
            // Fully resolved?
            if (genericParameters.All(x => x.Resolved))
            {
                if (resolveStep == ResolveStep.Mix)
                {
                    classSource.ClassName = classNameWithGenerics;
                    classSource.GenericArguments = [];
                }
            }
            else if (resolveStep == ResolveStep.Mix)
            {
                throw new InvalidOperationException("During mix phase, shaders generics are expected to be fully resolved");
            }
        }
    }


    private static void InstantiateGenericShader(NewSpirvBuffer shader, string className, GenericResolver genericResolver, IExternalShaderLoader shaderLoader, ReadOnlySpan<ShaderMacro> macros)
    {
        ShaderClass.ProcessNameAndTypes(shader, 0, shader.Count, out var names, out var types);

        var bound = shader.Header.Bound;

        var resolvedLinks = new Dictionary<int, string>();
        var semantics = new Dictionary<string, string>();

        var genericParameters = new List<GenericParameter>();
        foreach (var i in shader)
        {
            if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is { } genericParameter)
            {
                var genericParameterType = types[genericParameter.ResultType];
                var genericParameterName = names[genericParameter.ResultId];
                var resolved = genericResolver.TryResolveGenericValue(genericParameterType, genericParameterName, genericParameters.Count, out var genericValue);
                genericParameters.Add(new(genericParameterType, genericParameter.ResultId, genericParameter.ResultType, i.Index, genericParameterName, resolved, genericValue));
            }
        }

        Console.WriteLine($"[Shader] Instantiating {className} with values {string.Join(",", genericParameters.Select(x => x.Value))}");

        StringBuilder classNameWithGenericsBuilder = new();
        classNameWithGenericsBuilder.Append(className).Append("<");

        for (int i = 0; i < genericParameters.Count; i++)
        {
            var genericParameter = genericParameters[i];
            var index = genericParameter.Index;
            if (i > 0)
                classNameWithGenericsBuilder.Append(",");
            classNameWithGenericsBuilder.Append(genericParameter.Value.ToString());

            if (!genericParameter.Resolved)
                continue;

            switch (genericParameter.Type)
            {
                case ScalarType { TypeName: "int" }:
                    shader.Replace(index, new OpConstant<int>(genericParameter.ResultType, genericParameter.ResultId, (int)genericParameter.Value));
                    break;
                case ScalarType { TypeName: "float" }:
                    shader.Replace(index, new OpConstant<float>(genericParameter.ResultType, genericParameter.ResultId, (float)genericParameter.Value));
                    break;
                case ScalarType { TypeName: "bool" }:
                    if ((bool)genericParameter.Value)
                        shader.Replace(index, new OpConstantTrue(genericParameter.ResultType, genericParameter.ResultId));
                    else
                        shader.Replace(index, new OpConstantFalse(genericParameter.ResultType, genericParameter.ResultId));
                    break;
                case GenericParameterType g when g.Kind is GenericParameterKindSDSL.LinkType:
                    shader.Replace(index, new OpConstantStringSDSL(genericParameter.ResultId, (string)genericParameter.Value));
                    resolvedLinks.Add(genericParameter.ResultId, (string)genericParameter.Value);
                    break;
                case GenericParameterType g when g.Kind is GenericParameterKindSDSL.Semantic:
                    shader.Replace(index, new OpConstantStringSDSL(genericParameter.ResultId, (string)genericParameter.Value));
                    semantics.Add(names[genericParameter.ResultId], (string)genericParameter.Value);
                    break;
                case GenericParameterType g when g.Kind is GenericParameterKindSDSL.MemberNameResolved:
                    // There should be no more reference to this MemberName (it should have been resolved during InstantiateMemberNames())
                    shader.Replace(index, new OpNop());
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        classNameWithGenericsBuilder.Append(">");
        var classNameWithGenerics = classNameWithGenericsBuilder.ToString();

        foreach (var i in shader)
        {
            if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderDeclaration)
            {
                shaderDeclaration.ShaderName = classNameWithGenerics;
            }
        }

        TransformResolvedSemantics(shader, semantics);
        TransformResolvedLinkIdIntoLinkString(shader, resolvedLinks);

        // In case we had to increase bound (new instructions), update header
        shader.Header = shader.Header with { Bound = bound };

        genericResolver.PostProcess(classNameWithGenerics, genericParameters);
    }

    private static void TransformResolvedSemantics(NewSpirvBuffer shader, Dictionary<string, string> semantics)
    {
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: { Value: Decoration.UserSemantic, Parameters: { } m } } decorate)
            {
                var n = new LiteralValue<string>(m.Span);
                if (semantics.TryGetValue(n.Value, out var newSemantic))
                {
                    n.Value = newSemantic;
                    decorate.Decoration = new(decorate.Decoration.Value, n.Words);
                }
                n.Dispose();
            }
            else if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.UserSemantic, Parameters: { } m2 } } decorate2)
            {
                var n = new LiteralValue<string>(m2.Span);
                if (semantics.TryGetValue(n.Value, out var newSemantic))
                {
                    n.Value = newSemantic;
                    decorate2.Decoration = new(decorate2.Decoration.Value, n.Words);
                }
                n.Dispose();
            }
        }
    }

    private static NewSpirvBuffer InstantiateMemberNames(NewSpirvBuffer shader, string shaderName, GenericResolver genericResolver, IExternalShaderLoader shaderLoader, ReadOnlySpan<ShaderMacro> macros)
    {
        bool hasUnresolvableShader = false;
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpUnresolvableShaderSDSL && (OpUnresolvableShaderSDSL)i is { } unresolvableShader)
            {
                hasUnresolvableShader = true;
            }
        }

        if (!hasUnresolvableShader)
            return shader;

        var instantiatedGenericsMacros = new List<(string Name, string Definition)>();
        var genericParameterIndex = 0;
        ShaderClass.ProcessNameAndTypes(shader, 0, shader.Count, out var names, out var types);
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is { } genericParameter)
            {
                var genericParameterName = names[genericParameter.ResultId];
                var genericParameterType = types[genericParameter.ResultType];
                if (genericParameterType is GenericParameterType { Kind: GenericParameterKindSDSL.MemberName })
                {
                    if (genericResolver.TryResolveGenericValue(genericParameterType, genericParameterName, genericParameterIndex, out var value))
                        instantiatedGenericsMacros.Add((names[genericParameter], value.ToString()));
                }
                genericParameterIndex++;
            }
            else if (i.Op == Op.OpUnresolvableShaderSDSL && (OpUnresolvableShaderSDSL)i is { } unresolvableShader)
            {
                var code = unresolvableShader.ShaderCode;
                if (instantiatedGenericsMacros.Count > 0)
                {
                    // Add something to shaderName (which is used as key in ShaderLoader cache)
                    var originalShaderName = shaderName;
                    shaderName += $"_{string.Join("_", instantiatedGenericsMacros.Select(x => x.Definition))}";

                    // Note: we apply the preprocessor only the shader body to transform generics parameter into their actual value without touching the generic definition
                    code = code.Substring(0, unresolvableShader.ShaderCodeNameEnd)
                                // Update shader name for ShaderLoader cache
                                .Replace(originalShaderName, shaderName)
                                // Mark MemberName as resolved
                                .Replace("MemberName ", "MemberNameResolved ")
                        + MonoGamePreProcessor.Run(code.Substring(unresolvableShader.ShaderCodeNameEnd), $"{shaderName}.sdsl", CollectionsMarshal.AsSpan(instantiatedGenericsMacros));
                }

                if (!shaderLoader.LoadExternalBuffer(shaderName, code, macros, out shader, out _))
                    throw new InvalidOperationException();
                return shader;
            }
        }

        return shader;
    }

    private static void TransformResolvedLinkIdIntoLinkString(NewSpirvBuffer shader, Dictionary<int, string> resolvedLinks)
    {
        // Try to resolve LinkType generics
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpMemberDecorate
                && ((OpMemberDecorate)i) is { Decoration: { Value: Decoration.LinkIdSDSL, Parameters: { } m } } linkDecorate)
            {
                using var n = new LiteralValue<int>(m.Span);
                if (resolvedLinks.TryGetValue(n.Value, out var resolvedValue))
                {
                    shader.Replace(index, new OpMemberDecorateString(linkDecorate.StructureType, linkDecorate.Member, new ParameterizedFlag<Decoration>(Decoration.LinkSDSL, [.. resolvedValue.AsDisposableLiteralValue().Words])));
                }
            }
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
                 || op.Kind == OperandKind.PairIdRefIdRef))
            {
                foreach (ref var word in op.Words)
                {
                    if (idRemapping.TryGetValue(word, out var to1))
                        word = to1;
                }
            }

            if ((op.Kind == OperandKind.PairIdRefLiteralInteger)
                && idRemapping.TryGetValue(op.Words[0], out var to2))
            {
                if (op.Quantifier != OperandQuantifier.One)
                    throw new NotImplementedException();
                op.Words[0] = to2;
            }

            if ((op.Kind == OperandKind.PairLiteralIntegerIdRef
                 || op.Kind == OperandKind.PairIdRefIdRef)
                && idRemapping.TryGetValue(op.Words[1], out var to3))
            {
                if (op.Quantifier != OperandQuantifier.One)
                    throw new NotImplementedException();
                op.Words[1] = to3;
            }
        }
    }

    /// <summary>
    /// Gets or load a shader, with generic instantiation (if requested).
    /// </summary>
    /// <param name="shaderLoader"></param>
    /// <param name="classSource">The generics parameters should be in <see cref="parentBuffer"/>.</param>
    /// <param name="macros"></param>
    /// <param name="resolveStep"></param>
    /// <returns></returns>
    /// <param name="parentBuffer"></param>
    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, ReadOnlySpan<ShaderMacro> macros, ResolveStep resolveStep, NewSpirvBuffer parentBuffer)
    {
        return GetOrLoadShader(shaderLoader, classSource.ClassName, new GenericResolverFromClassInstantiation(classSource, parentBuffer, resolveStep), macros);
    }

    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string className, string[] genericValues, ReadOnlySpan<ShaderMacro> macros)
    {
        return GetOrLoadShader(shaderLoader, className, new GenericResolverFromValues(genericValues), macros);
    }


    private static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string className, GenericResolver genericResolver, ReadOnlySpan<ShaderMacro> macros)
    {
        var shader = GetOrLoadShader(shaderLoader, className, macros, out var isFromCache);

        if (!isFromCache)
            Spv.Dis(shader, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);

        if (genericResolver.NeedsResolve())
        {
            shader = InstantiateMemberNames(shader, className, genericResolver, shaderLoader, macros);

            // Copy shader
            shader = CopyShader(shader);

            InstantiateGenericShader(shader, className, genericResolver, shaderLoader, macros);
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

    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string className, ReadOnlySpan<ShaderMacro> defines, out bool isFromCache)
    {
        Console.WriteLine($"[Shader] Requesting non-generic class {className}");

        if (!shaderLoader.LoadExternalBuffer(className, defines, out var buffer, out isFromCache))
            throw new InvalidOperationException($"Could not load shader [{className}]");

        if (!isFromCache)
            Console.WriteLine($"[Shader] Loading non-generic class {className} for 1st time");

        return buffer;
    }
}
