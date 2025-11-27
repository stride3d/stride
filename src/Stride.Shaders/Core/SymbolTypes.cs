using System.Diagnostics.CodeAnalysis;
using System.Text;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Core;



public abstract record SymbolType()
{
    /// <summary>
    /// Converts to an identifier compatible with <see cref="Spirv.Core.Op.OpName"/>.
    /// </summary>
    /// <returns></returns>
    public virtual string ToId() => ToString();

    public static bool TryGetNumeric(string name, [MaybeNullWhen(false)] out SymbolType result)
    {
        if (ScalarType.Types.TryGetValue(name, out var s))
        {
            result = s;
            return true;
        }
        else if (VectorType.Types.TryGetValue(name, out var v))
        {
            result = v;
            return true;
        }
        else if (MatrixType.Types.TryGetValue(name, out var m))
        {
            result = m;
            return true;
        }
        else if (name == "void")
        {
            result = ScalarType.From("void");
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }
    public static bool TryGetBufferType(string name, string? templateType, [MaybeNullWhen(false)] out SymbolType result)
    {
        (result, bool found) = (name, templateType) switch
        {
            ("Buffer", "float") => (new BufferType(ScalarType.From("float"), -1) as SymbolType, true),
            ("Buffer", "int") => (new BufferType(ScalarType.From("int"), -1), true),
            ("Buffer", "uint") => (new BufferType(ScalarType.From("uint"), -1), true),
            // TODO: Use scalar type instead of vector type as in SPIR-V spec?
            ("Buffer", "float2") => (new BufferType(VectorType.From("float2"), -1), true),
            ("Buffer", "float3") => (new BufferType(VectorType.From("float3"), -1), true),
            ("Buffer", "float4") => (new BufferType(VectorType.From("float4"), -1), true),
            ("Buffer", "int2") => (new BufferType(VectorType.From("int2"), -1), true),
            ("Buffer", "int3") => (new BufferType(VectorType.From("int3"), -1), true),
            ("Buffer", "int4") => (new BufferType(VectorType.From("int4"), -1), true),
            ("Buffer", "uint2") => (new BufferType(VectorType.From("uint2"), -1), true),
            ("Buffer", "uint3") => (new BufferType(VectorType.From("uint3"), -1), true),
            ("Buffer", "uint4") => (new BufferType(VectorType.From("uint4"), -1), true),
            ("Texture", null) => (new Texture1DType(ScalarType.From("float")), true),
            ("Texture1D", null) => (new Texture1DType(ScalarType.From("float")), true),
            ("Texture2D", null) => (new Texture2DType(ScalarType.From("float")), true),
            ("Texture3D", null) => (new Texture3DType(ScalarType.From("float")), true),
            ("Texture", "int4" or "uint4" or "float4") => (new Texture1DType(VectorType.From(templateType).BaseType), true),
            ("Texture1D", "int4" or "uint4" or "float4") => (new Texture1DType(VectorType.From(templateType).BaseType), true),
            ("Texture2D", "int4" or "uint4" or "float4") => (new Texture2DType(VectorType.From(templateType).BaseType), true),
            ("Texture3D", "int4" or "uint4" or "float4") => (new Texture3DType(VectorType.From(templateType).BaseType), true),

            _ => (null, false)
        };
        return found;
    }
}

public sealed record UndefinedType(string TypeName) : SymbolType()
{
    public override string ToString()
    {
        return TypeName;
    }
}

public sealed record PointerType(SymbolType BaseType, Specification.StorageClass StorageClass) : SymbolType()
{
    public override string ToId() => $"ptr_{StorageClass}_{BaseType.ToId()}";
    public override string ToString() => $"*{BaseType}";
}

public sealed partial record ScalarType(string TypeName) : SymbolType()
{
    public override string ToString() => TypeName;
}
public sealed partial record VectorType(ScalarType BaseType, int Size) : SymbolType()
{
    public override string ToString() => $"{BaseType}{Size}";
}
public sealed partial record MatrixType(ScalarType BaseType, int Rows, int Columns) : SymbolType()
{
    public override string ToString() => $"{BaseType}{Rows}x{Columns}";
}
public sealed record ArrayType(SymbolType BaseType, int Size) : SymbolType()
{
    public override string ToString() => $"{BaseType}[{Size}]";
}
public record StructuredType(string Name, List<(string Name, SymbolType Type)> Members) : SymbolType()
{
    public override string ToId() => Name;
    public override string ToString() => $"{Name}{{{string.Join(", ", Members.Select(x => $"{x.Type} {x.Name}"))}}}";

    public bool TryGetFieldType(string name, [MaybeNullWhen(false)] out SymbolType type)
    {
        foreach (var field in Members)
        {
            if (field.Name == name)
            {
                type = field.Type;
                return true;
            }
        }

        type = null;
        return false;
    }
    public int TryGetFieldIndex(string name)
    {
        for (var index = 0; index < Members.Count; index++)
        {
            var field = Members[index];
            if (field.Name == name)
                return index;
        }

        return -1;
    }

}

public sealed record StructType(string Name, List<(string Name, SymbolType Type)> Members) : StructuredType(Name, Members);
public sealed record BufferType(SymbolType BaseType, int Size) : SymbolType()
{
    public override string ToString() => $"Buffer<{BaseType}, {Size}>";
}

// TODO: Add sampler parameters
public sealed record SamplerType() : SymbolType()
{
    public override string ToId() => $"type_sampler";
    public override string ToString() => $"SamplerState";
}
public sealed record SampledImage(TextureType ImageType) : SymbolType()
{
    public override string ToString() => $"SampledImage<{ImageType}>";
}

public abstract record TextureType(ScalarType ReturnType, Dim Dimension, int Depth, bool Arrayed, bool Multisampled, int Sampled, ImageFormat Format) : SymbolType()
{
    public override string ToId() => $"Texture_{ReturnType}";
    public override string ToString() => $"Texture<{ReturnType}>({Dimension}, {Depth}, {Arrayed}, {Multisampled}, {Sampled}, {Format})";
}

public sealed record Texture1DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim1D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"Texture1D<{ReturnType}>";
}
public sealed record Texture2DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim2D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"Texture2D<{ReturnType}>";
}
public sealed record Texture3DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim3D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"Texture3D<{ReturnType}>";
}

public sealed record TextureCubeType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Cube, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"TextureCube<{ReturnType}>";
}

public sealed record FunctionGroupType() : SymbolType();

public sealed record FunctionType(SymbolType ReturnType, List<SymbolType> ParameterTypes) : SymbolType()
{
    public bool Equals(FunctionType? other)
    {
        if (other is null)
            return false;
        if (ReturnType == null || other.ReturnType == null)
            return false;
        if (ParameterTypes == null || other.ParameterTypes == null)
            return false;
        return ReturnType == other.ReturnType && ParameterTypes.SequenceEqual(other.ParameterTypes);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + ReturnType.GetHashCode();
        foreach (var item in ParameterTypes)
        {
            hash = hash * 31 + item.GetHashCode();
        }
        return hash;
    }

    public override string ToId()
    {
        var builder = new StringBuilder();
        builder.Append($"fn_");
        for (int i = 0; i < ParameterTypes.Count; i++)
        {
            builder.Append(ParameterTypes[i].ToId());
            builder.Append('_');
        }
        return builder.Append(ReturnType.ToId()).ToString();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"fn(");
        for (int i = 0; i < ParameterTypes.Count; i++)
        {
            builder.Append(ParameterTypes[i]);
            if (i < ParameterTypes.Count - 1)
                builder.Append('*');
        }
        return builder.Append($")->{ReturnType}").ToString();
    }
}

public sealed record StreamsSymbol : SymbolType;

public sealed record ConstantBufferSymbol(string Name, List<(string Name, SymbolType Type)> Members) : StructuredType(Name, Members);
public sealed record ParamsSymbol(string Name, List<(string Name, SymbolType Type)> Symbols) : SymbolType;
public sealed record EffectSymbol(string Name, List<(string Name, SymbolType Type)> Symbols) : SymbolType;

public sealed record ShaderSymbol(string Name, int[] GenericArguments, List<Symbol> Components) : SymbolType
{
    public Symbol Get(string name, SymbolKind kind)
    {
        foreach (var e in Components)
            if (e.Id.Kind == kind && e.Id.Name == name)
                return e;
        throw new ArgumentException($"{name} not found in Mixin {Name}");
    }
    public bool TryGet(string name, SymbolKind kind, out Symbol? value)
    {
        foreach (var e in Components)
            if (e.Id.Kind == kind && e.Id.Name == name)
            {
                value = e;
                return true;
            }
        value = null!;
        return false;
    }

    public string ToClassName()
    {
        if (GenericArguments.Length == 0)
            return Name;

        var className = new ShaderClassInstantiation(Name, GenericArguments);
        return className.ToClassName();
    }
}
