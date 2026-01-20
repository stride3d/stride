using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Core;

public interface ISymbolTypeNode
{
    public void Accept(TypeVisitor visitor);

    public bool Accept<TResult>(TypeVisitor<TResult> visitor);
}

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
        else
        {
            result = null;
            return false;
        }
    }
    public static bool TryGetBufferType(SymbolTable table, SpirvContext context, string name, TypeName? templateTypeName, [MaybeNullWhen(false)] out SymbolType result)
    {
        // Special case: StructuredBuffer allows non vector/scalar types so treat it earlier
        switch (name)
        {
            case "StructuredBuffer":
            case "RWStructuredBuffer":
                var templateType = templateTypeName.ResolveType(table, context);
                result = new StructuredBufferType(templateType, name.StartsWith("RW"));
                return true;
        }

        // Note: templateTypeName is resolved lazily (because it might not be a buffer type and we don't need to resolve it)
        static ScalarType ResolveScalarType(SymbolTable table, SpirvContext context, TypeName? templateTypeName)
        {
            var templateType = templateTypeName?.ResolveType(table, context) ?? ScalarType.Float;

            return templateType switch
            {
                VectorType v => v.BaseType,
                ScalarType s => s,
            };
        }

        SymbolType? foundType = name switch
        {
            "Buffer" => new BufferType(ResolveScalarType(table, context, templateTypeName)),
            "RWBuffer" => new BufferType(ResolveScalarType(table, context, templateTypeName), true),

            "Texture1D" => new Texture1DType(ResolveScalarType(table, context, templateTypeName)),
            "Texture2D" => new Texture2DType(ResolveScalarType(table, context, templateTypeName)),
            "Texture2DMS" => new Texture2DType(ResolveScalarType(table, context, templateTypeName)) { Multisampled = true },
            "Texture3D" => new Texture3DType(ResolveScalarType(table, context, templateTypeName)),
            "TextureCube" => new TextureCubeType(ResolveScalarType(table, context, templateTypeName)),
            
            "Texture1DArray" => new Texture1DType(ResolveScalarType(table, context, templateTypeName)) { Arrayed = true },
            "Texture2DArray" => new Texture2DType(ResolveScalarType(table, context, templateTypeName)) { Arrayed = true },
            "Texture2DMSArray" => new Texture2DType(ResolveScalarType(table, context, templateTypeName)) { Multisampled = true, Arrayed = true },
            "Texture3DArray" => new Texture3DType(ResolveScalarType(table, context, templateTypeName)) { Arrayed = true },
            "TextureCubeArray" => new TextureCubeType(ResolveScalarType(table, context, templateTypeName)) { Arrayed = true },

            "RWTexture1D" => new Texture1DType(ResolveScalarType(table, context, templateTypeName)) { Sampled = 2 },
            "RWTexture2D" => new Texture2DType(ResolveScalarType(table, context, templateTypeName)) { Sampled = 2 },
            "RWTexture3D" => new Texture3DType(ResolveScalarType(table, context, templateTypeName)) { Sampled = 2 },

            "RWTexture1DArray" => new Texture1DType(ResolveScalarType(table, context, templateTypeName)) { Sampled = 2, Arrayed = true },
            "RWTexture2DArray" => new Texture2DType(ResolveScalarType(table, context, templateTypeName)) { Sampled = 2, Arrayed = true },
            
            _ => null,
        };

        if (foundType != null)
        {
            result = foundType;
            return true;
        }

        result = null;
        return false;
    }

    public abstract void Accept(TypeVisitor visitor);

    public abstract TResult Accept<TResult>(TypeVisitor<TResult> visitor);
}

public sealed partial record UndefinedType(string TypeName) : SymbolType()
{
    public override string ToString()
    {
        return TypeName;
    }
}

public sealed partial record PointerType(SymbolType BaseType, Specification.StorageClass StorageClass) : SymbolType()
{
    public override string ToId() => $"ptr_{StorageClass}_{BaseType.ToId()}";
    public override string ToString() => $"*{BaseType}";
}

public enum Scalar
{
    Void,
    Boolean,
    Int,
    UInt,
    Int64,
    UInt64,
    //Half,
    Float,
    Double
}

public sealed partial record ScalarType(Scalar Type) : SymbolType()
{
    public static ScalarType Void { get; } = new(Scalar.Void);
    public static ScalarType Boolean { get; } = new(Scalar.Boolean);
    public static ScalarType Int { get; } = new(Scalar.Int);
    public static ScalarType UInt { get; } = new(Scalar.UInt);
    public static ScalarType Int64 { get; } = new(Scalar.Int64);
    public static ScalarType UInt64 { get; } = new(Scalar.UInt64);
    public static ScalarType Float { get; } = new(Scalar.Float);
    public static ScalarType Double { get; } = new(Scalar.Double);

    public override string ToString() => Type switch
    {
        Scalar.Void => "void",
        Scalar.Boolean => "bool",
        Scalar.Int => "int",
        Scalar.UInt => "uint",
        Scalar.Int64 => "long",
        Scalar.UInt64 => "ulong",
        Scalar.Float => "float",
        Scalar.Double => "double",
        _ => throw new ArgumentOutOfRangeException()
    };
}
public sealed partial record VectorType(ScalarType BaseType, int Size) : SymbolType()
{
    public int Size { get; } = Size >= 2 ? Size : throw new ArgumentException("Argument must be at least 2.", nameof(Size));

    public override string ToString() => $"{BaseType}{Size}";
}
public sealed partial record MatrixType(ScalarType BaseType, int Rows, int Columns) : SymbolType()
{
    public int Rows { get; } = Rows >= 2 ? Rows : throw new ArgumentException("Argument must be at least 2.", nameof(Rows));
    public int Columns { get; } = Columns >= 2 ? Columns : throw new ArgumentException("Argument must be at least 2.", nameof(Columns));

    // Note: this is HLSL-style so Rows/Columns meaning is swapped
    public override string ToString() => $"{BaseType}{Columns}x{Rows}";
}
/// <summary>
/// Array type.
/// </summary>
/// <param name="BaseType">The base type for the array.</param>
/// <param name="Size">The size of the array. If -1, it means size is not defined, such as using [].</param>
public sealed partial record ArrayType(SymbolType BaseType, int Size, (int Id, NewSpirvBuffer Buffer)? SizeExpression = null) : SymbolType()
{
    // We want this mutable for internal use
    public (int Id, NewSpirvBuffer Buffer)? SizeExpression { get; set; } = SizeExpression;
    public override string ToId() => $"{BaseType.ToId()}[{(Size != -1 ? Size : string.Empty)}]";
    public override string ToString() => $"{BaseType}[{(Size != -1 ? Size : string.Empty)}]";
}

public partial record struct StructuredTypeMember(string Name, SymbolType Type, TypeModifier TypeModifier) : ISymbolTypeNode;

public partial record StructuredType(string Name, List<StructuredTypeMember> Members) : SymbolType()
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

public sealed partial record StructType(string Name, List<StructuredTypeMember> Members) : StructuredType(Name, Members)
{
    public override string ToString() => $"struct {base.ToString()}";
}

public sealed partial record StructuredBufferType(SymbolType BaseType, bool WriteAllowed = false) : StructuredType($"{(WriteAllowed ? "RW" : "")}StructuredBuffer<{BaseType.ToId()}>", [new(string.Empty, BaseType, TypeModifier.None)])
{
    public override string ToId() => $"{(WriteAllowed ? "RW" : "")}StructuredBuffer<{BaseType.ToId()}>";

    public override string ToString() => $"{(WriteAllowed ? "RW" : "")}StructuredBuffer<{BaseType}>";
}

public sealed partial record BufferType(ScalarType BaseType, bool WriteAllowed = false) : SymbolType()
{
    public override string ToString() => $"{(WriteAllowed ? "RW" : "")}Buffer<{BaseType}>";
}

// TODO: Add sampler parameters
public sealed partial record SamplerType() : SymbolType()
{
    public override string ToId() => $"type_sampler";
    public override string ToString() => $"SamplerState";
}
public sealed partial record SampledImage(TextureType ImageType) : SymbolType()
{
    public override string ToString() => $"SampledImage<{ImageType}>";
}

public abstract partial record TextureType(ScalarType ReturnType, Dim Dimension, int Depth, bool Arrayed, bool Multisampled, int Sampled, ImageFormat Format) : SymbolType()
{
    public override string ToId() => $"Texture_{ReturnType}";
    public override string ToString() => $"Texture<{ReturnType}>({Dimension}, {Depth}, {Arrayed}, {Multisampled}, {Sampled}, {Format})";
}

public sealed partial record Texture1DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim1D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"{(Sampled == 2 ? "RW" : "")}Texture1D{(Arrayed ? "Array" : "")}<{ReturnType}>";
}
public sealed partial record Texture2DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim2D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"{(Sampled == 2 ? "RW" : "")}Texture2D{(Multisampled ? "MS" : "")}{(Arrayed ? "Array" : "")}<{ReturnType}>";
}
public sealed partial record Texture3DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim3D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"{(Sampled == 2 ? "RW" : "")}Texture3D{(Arrayed ? "Array" : "")}<{ReturnType}>";
}

public sealed partial record TextureCubeType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Cube, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"TextureCube{(Arrayed ? "Array" : "")}<{ReturnType}>";
}

public sealed partial record FunctionGroupType() : SymbolType();

public partial record struct FunctionParameter(SymbolType Type, ParameterModifiers Modifiers) : ISymbolTypeNode;

public sealed partial record FunctionType(SymbolType ReturnType, List<FunctionParameter> ParameterTypes) : SymbolType()
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
            if (ParameterTypes[i].Modifiers.HasFlag(ParameterModifiers.Const))
                builder.Append("const ");
            switch (ParameterTypes[i].Modifiers)
            {
                case var flag when flag.HasFlag(ParameterModifiers.InOut):
                    builder.Append("inout ");
                    break;
                case var flag when flag.HasFlag(ParameterModifiers.Out):
                    builder.Append("out ");
                    break;
                case var flag when flag.HasFlag(ParameterModifiers.In):
                    builder.Append("in ");
                    break;
            }
            builder.Append(ParameterTypes[i].Type.ToId());
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

public sealed partial record StreamsSymbol : SymbolType;

public sealed partial record ConstantBufferSymbol(string Name, List<StructuredTypeMember> Members) : StructuredType(Name, Members)
{
    public override string ToId() => $"type.{Name}";
    public override string ToString() => $"cbuffer {base.ToString()}";
}
public sealed partial record ParamsSymbol(string Name, List<(string Name, SymbolType Type)> Symbols) : SymbolType;
public sealed partial record EffectSymbol(string Name, List<(string Name, SymbolType Type)> Symbols) : SymbolType;

public partial record ShaderSymbol(string Name, int[] GenericArguments) : SymbolType
{
    public string ToClassName()
    {
        if (GenericArguments.Length == 0)
            return Name;

        var className = new ShaderClassInstantiation(Name, GenericArguments);
        return className.ToClassNameWithGenerics();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Name);
        if (GenericArguments.Length > 0)
        {
            builder.Append('<');
            for (int i = 0; i < GenericArguments.Length; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append('%');
                builder.Append(GenericArguments[i]);
            }
            builder.Append('>');
        }
        return builder.ToString();
    }
}

public sealed partial record LoadedShaderSymbol(string Name, int[] GenericArguments) : ShaderSymbol(Name, GenericArguments)
{
    public List<(Symbol Symbol, VariableFlagsMask Flags)> Variables { get; init; } = [];

    public List<(Symbol Symbol, FunctionFlagsMask Flags)> Methods { get; init; } = [];

    public List<(StructuredType Type, int ImportedId)> StructTypes { get; init; } = [];
    public List<LoadedShaderSymbol> InheritedShaders { get; init; } = [];

    internal bool TryResolveSymbol(SymbolTable symbolTable, SpirvContext context, string name, out Symbol symbol)
    {
        if (TryResolveSymbolNoRecursion(this == symbolTable.CurrentShader, context, name, out symbol))
            return true;

        // Process inherited classes
        // note: since it contains all indirectly inherited method too, which is why it is splitted with TryResolveSymbolNoRecursion
        foreach (var inheritedShader in InheritedShaders)
            if (inheritedShader.TryResolveSymbolNoRecursion(false, context, name, out symbol))
                return true;

        return false;
    }

    private bool TryResolveSymbolNoRecursion(bool isCurrentShader, SpirvContext context, string name, out Symbol symbol)
    {
        symbol = default;

        var found = BuildMethodGroup(isCurrentShader, context, name, ref symbol);
        if (found)
        {
            // If any method is found, let's process inherited classes too: we need all method groups to find proper override
            foreach (var inheritedClass in InheritedShaders)
            {
                inheritedClass.BuildMethodGroup(false, context, name, ref symbol);
            }
            return true;
        }

        var variables = CollectionsMarshal.AsSpan(Variables);
        foreach (ref var c in variables)
        {
            if (c.Symbol.Id.Name == name)
            {
                if (c.Symbol.IdRef == 0)
                {
                    // Emit symbol
                    var shaderId = context.GetOrRegister(this);
                    context.ImportShaderVariable(shaderId, ref c.Symbol, c.Flags);
                }

                symbol = c.Symbol;
                if (!isCurrentShader)
                    symbol = symbol with { MemberAccessWithImplicitThis = c.Symbol.Type };
                return true;
            }

            // For cbuffer, all their members are visible directly at the top-level without referencing the cbuffer
            if (c.Symbol.Type is PointerType { StorageClass: Specification.StorageClass.Uniform } p && p.BaseType is ConstantBufferSymbol cb)
            {
                for (int index = 0; index < cb.Members.Count; index++)
                {
                    var member = cb.Members[index];
                    if (member.Name == name)
                    {
                        if (c.Symbol.IdRef == 0)
                        {
                            // Emit symbol
                            var shaderId = context.GetOrRegister(this);
                            context.ImportShaderVariable(shaderId, ref c.Symbol, c.Flags);
                        }

                        var sid = new SymbolID(member.Name, SymbolKind.CBuffer, Storage.Uniform);
                        symbol = new Symbol(sid, new PointerType(member.Type, Specification.StorageClass.Uniform), c.Symbol.IdRef, AccessChain: index);
                        if (!isCurrentShader)
                            symbol = symbol with { MemberAccessWithImplicitThis = c.Symbol.Type };
                        return true;
                    }
                }
            }
        }

        symbol = default;
        return false;
    }

    private bool BuildMethodGroup(bool isCurrentShader, SpirvContext context, string name, ref Symbol symbol)
    {
        var found = false;
        var methods = CollectionsMarshal.AsSpan(Methods);
        foreach (ref var c in methods)
        {
            if (c.Symbol.Id.Name == name)
            {
                if (c.Symbol.IdRef == 0)
                {
                    // Emit symbol
                    // TODO: emit it only when this specific method is *selected* as proper overload (signature) & override (base vs this)
                    var shaderId = context.GetOrRegister(this);
                    context.ImportShaderMethod(shaderId, ref c.Symbol, c.Flags);
                }

                // Combine method symbols if multiple matches
                var methodSymbol = c.Symbol;

                if (!isCurrentShader)
                    methodSymbol = methodSymbol with { MemberAccessWithImplicitThis = c.Symbol.Type };

                // If symbol is set, complete it as a method group
                symbol = symbol.Type switch
                {
                    // First time: just assign to symbol
                    null => methodSymbol,
                    // Second time: create a method group
                    FunctionType => new Symbol(new(name, SymbolKind.MethodGroup, IsStage: symbol.Id.IsStage), new FunctionGroupType(), 0, GroupMembers: [symbol, methodSymbol]),
                    // Third time and later: complete method group
                    FunctionGroupType => symbol with { GroupMembers = symbol.GroupMembers.Add(methodSymbol) },
                };

                found = true;
            }
        }
        return found;
    }

    public override string ToString() => base.ToString();
}

public sealed partial record GenericParameterType(GenericParameterKindSDSL Kind) : SymbolType;

public sealed partial record StreamsType : SymbolType
{
    public override string ToString() => "Streams";
}