using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public record class ShaderMixinInstantiation(List<ShaderClassInstantiation> Mixins, Dictionary<string, ShaderMixinInstantiation> Compositions);

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

    public Dictionary<int, ShaderClassInstantiation> ShaderReferences { get; set; } = new();

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
    private static void BuildInheritanceList(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, NewSpirvBuffer buffer, List<ShaderClassInstantiation> inheritanceList, ResolveStep resolveStep)
    {
        // Build shader name mapping
        var shaderMapping = classSource.ShaderReferences;
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

    public static void BuildInheritanceList(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, List<ShaderClassInstantiation> inheritanceList, ResolveStep resolveStep, NewSpirvBuffer? parentBuffer = null)
    {
        // TODO: cache same instantiations within context?
        if (!inheritanceList.Contains(classSource))
        {
            if (classSource.Buffer == null)
            {
                var shader = GetOrLoadShader(shaderLoader, classSource, resolveStep, parentBuffer);
                classSource.Buffer = shader;
            }

            if (!inheritanceList.Contains(classSource))
            {
                BuildInheritanceList(shaderLoader, classSource, classSource.Buffer, inheritanceList, resolveStep);
                inheritanceList.Add(classSource);
            }
        }
    }

    public static NewSpirvBuffer InstantiateGenericShader(NewSpirvBuffer shader, ShaderClassInstantiation classSource, ResolveStep resolveStep, NewSpirvBuffer? parentBuffer = null)
    {
        // Instantiate generics
        var copiedShader = new NewSpirvBuffer();
        foreach (var i in shader)
        {
            var i2 = new OpData(i.Data.Memory.Span);
            copiedShader.Add(i2);
        }
        shader = copiedShader;

        var generics = new List<int>();
        var genericArgumentIndex = 0;
        Dictionary<int, int> idRemapping = new();
        HashSet<int> targets = new();
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderDeclaration)
            {
                shaderDeclaration.ShaderName = classSource.ToClassName();
            }
            else if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is {} genericParameter)
            {
                idRemapping.Add(genericParameter.ResultId, classSource.GenericArguments[genericArgumentIndex]);
                targets.Add(classSource.GenericArguments[genericArgumentIndex]);
                genericArgumentIndex++;
                SetOpNop(i.Data.Memory.Span);
            }
        }

        // Remove OpName
        for (var index = 0; index < shader.Count; index++)
        {
            var i = shader[index];
            if (i.Op == Op.OpName && (OpName)i is { } name)
            {
                if (idRemapping.ContainsKey(name.Target))
                    SetOpNop(i.Data.Memory.Span);
            }
        }

        if (idRemapping.Count > 0)
            RemapIds(shader, 0, shader.Count, idRemapping);

        // Try to resolve fully the new generic parameter values
        var resolvedParameters = new Dictionary<int, string>();
        if (parentBuffer != null)
        {
            for (var index = 0; index < parentBuffer.Count; index++)
            {
                var i = parentBuffer[index];
                if (i.Op == Op.OpConstant)
                {
                    if (targets.Contains(i.Data.IdResult!.Value))
                    {
                        var value = new LiteralValue<float>(i.Data.Memory.Span[3..]);
                        resolvedParameters.Add(i.Data.IdResult!.Value, value.Value.ToString());

                        // import constant in current shader
                        shader.Add(new OpConstant<float>(i.Data.IdResultType!.Value, i.Data.IdResult!.Value, value.Value));
                    }
                }
                else if (i.Op == Op.OpConstantStringSDSL && (OpConstantStringSDSL)i is { } constantString)
                {
                    if (targets.Contains(i.Data.IdResult!.Value))
                    {
                        var value = constantString.LiteralString;
                        resolvedParameters.Add(i.Data.IdResult!.Value, value);
                    }
                }
                else if (i.Op == Op.OpSDSLGenericParameter && (OpSDSLGenericParameter)i is { } genericParameter)
                {
                    if (targets.Contains(genericParameter.ResultId))
                    {

                    }
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
        if (resolvedParameters.Count == targets.Count)
        {
            var parameters = string.Join(',', classSource.GenericArguments.Select(x => resolvedParameters[x]));
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
                    shaderDeclaration.ShaderName = classSource.ClassName + "<" + parameters + ">";
                }
            }
        }

        return shader;
    }

    public static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }

    private static void RemapIds(NewSpirvBuffer buffer, int shaderStart, int shaderEnd, Dictionary<int, int> idRemapping)
    {
        for (var index = shaderStart; index < buffer.Count; index++)
        {
            var i = buffer[index];
            foreach (var op in i.Data)
            {
                if ((op.Kind == OperandKind.IdRef
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
    }

    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, ShaderClassInstantiation classSource, ResolveStep resolveStep, NewSpirvBuffer? parentBuffer = null)
    {
        var shader = GetOrLoadShader(shaderLoader, classSource.ClassName);

        if (classSource.GenericArguments.Length > 0)
        {
            shader = InstantiateGenericShader(shader, classSource, resolveStep, parentBuffer);
        }

        return shader;
    }

    private static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string className)
    {
        if (!shaderLoader.LoadExternalBuffer(className, out var buffer))
            throw new InvalidOperationException($"Could not load shader [{className}]");

        return buffer;
    }
}
