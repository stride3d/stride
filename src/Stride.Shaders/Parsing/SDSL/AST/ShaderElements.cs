using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class ShaderElement(TextLocation info) : Node(info)
{
    public SymbolType? Type { get; set; }

    public virtual void ProcessSymbol(SymbolTable table)
    {
    }
}


public enum StorageClass
{
    None,
    Extern,
    NoInterpolation,
    Precise,
    Shared,
    GroupShared,
    Static,
    Uniform,
    Volatile
}

public enum TypeModifier
{
    None,
    Const,
    RowMajor,
    ColumnMajor
}
public enum InterpolationModifier
{
    None,
    Linear,
    Centroid,
    NoInterpolation,
    NoPerspective,
    Sample
}

public enum StreamKind
{
    None,
    Stream,
    PatchStream
}

public static class ShaderVariableInformationExtensions
{
    public static StreamKind ToStreamKind(this string str)
    {
        return str switch
        {
            "stream" => StreamKind.Stream,
            "patchstream" => StreamKind.PatchStream,
            _ => StreamKind.None
        };
    }
    public static InterpolationModifier ToInterpolationModifier(this string str)
    {
        return str switch
        {
            "linear" => InterpolationModifier.Linear,
            "centroid" => InterpolationModifier.Centroid,
            "nointerpolation" => InterpolationModifier.NoInterpolation,
            "noperspective" => InterpolationModifier.NoPerspective,
            "sample" => InterpolationModifier.Sample,
            _ => InterpolationModifier.None
        };
    }
    public static StorageClass ToStorageClass(this string str)
    {
        return str switch
        {
            "extern" => StorageClass.Extern,
            "nointerpolation" => StorageClass.NoInterpolation,
            "precise" => StorageClass.Precise,
            "shared" => StorageClass.Shared,
            "groupshared" => StorageClass.GroupShared,
            "static" => StorageClass.Static,
            "uniform" => StorageClass.Uniform,
            "volatile" => StorageClass.Volatile,
            _ => StorageClass.None
        };
    }

    public static TypeModifier ToTypeModifier(this string str)
    {
        return str switch
        {
            "const" => TypeModifier.Const,
            "row_major" => TypeModifier.RowMajor,
            "column_major" => TypeModifier.ColumnMajor,
            _ => TypeModifier.None
        };
    }
}

public class ShaderVariable(TypeName typeName, Identifier name, Expression? value, TextLocation info) : ShaderElement(info)
{
    public Identifier Name { get; set; } = name;
    public TypeName TypeName { get; set; } = typeName;
    public Expression? Value { get; set; } = value;
    public StorageClass StorageClass { get; set; } = StorageClass.None;
    public TypeModifier TypeModifier { get; set; } = TypeModifier.None;
    public override string ToString()
    {
        return $"{(StorageClass != StorageClass.None ? $"{StorageClass} " : "")}{(TypeModifier != TypeModifier.None ? $"{TypeModifier} " : "")} {TypeName} {Name} = {Value}";
    }
}

public class TypeDef(TypeName type, Identifier name, TextLocation info) : ShaderElement(info)
{
    public Identifier Name { get; set; } = name;
    public TypeName TypeName { get; set; } = type;

    public override string ToString()
    {
        return $"typedef {TypeName} {Name}";
    }
}

public abstract class ShaderBuffer(string name, TextLocation info) : ShaderElement(info)
{
    public string Name { get; set; } = name;
    public List<ShaderMember> Members { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table)
    {
        var fields = new List<(string Name, SymbolType Type)>();
        foreach (var smem in Members)
        {
            smem.Type = smem.TypeName.ResolveType(table);
            table.DeclaredTypes.TryAdd(smem.Type.ToString(), smem.Type);

            fields.Add((smem.Name, smem.Type));
        }

        Type = new ConstantBufferSymbol(Name, fields);
        table.DeclaredTypes.TryAdd(Name, Type);
        var kind = this switch
        {
            CBuffer => SymbolKind.CBuffer,
            TBuffer => SymbolKind.TBuffer,
            RGroup => SymbolKind.RGroup,
            _ => throw new NotSupportedException()
        };
    }

    public abstract void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler);
}

public class ShaderStructMember(TypeName typename, Identifier identifier, TextLocation info) : Node(info)
{
    public TypeName TypeName { get; set; } = typename;
    public SymbolType? Type { get; set; }
    public Identifier Name { get; set; } = identifier;
    public List<ShaderAttribute> Attributes { get; set; } = [];

    public override string ToString()
    {
        if (Type is not null)
            return $"{Type} {Name}";
        else return $"{TypeName} {Name}";
    }
}

public class ShaderStruct(Identifier typename, TextLocation info) : ShaderElement(info)
{
    public Identifier TypeName { get; set; } = typename;
    public List<ShaderStructMember> Members { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table)
    {
        var fields = new List<(string Name, SymbolType Type)>();
        foreach (var smem in Members)
        {
            smem.Type = smem.TypeName.ResolveType(table);
            table.DeclaredTypes.TryAdd(smem.Type.ToString(), smem.Type);

            fields.Add((smem.Name, smem.Type));
        }

        Type = new StructType(TypeName.ToString() ?? "", fields);
        table.DeclaredTypes.TryAdd(TypeName.ToString(), Type);
    }

    public override string ToString()
    {
        return $"struct {TypeName} ({string.Join(", ", Members)})";
    }
}


public sealed class CBuffer(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var pointerType = context.GetOrRegister(new PointerType(Type, Specification.StorageClass.Uniform));
        var variable = context.Bound++;
        // TODO: Add a StreamSDSL storage class?
        context.Add(new OpVariable(pointerType, variable, Specification.StorageClass.Uniform, null));
        context.Variables.Add(Name, new(variable, pointerType, Name));
        context.AddName(variable, Name);

        for (var index = 0; index < Members.Count; index++)
        {
            var member = Members[index];
            var sid = new SymbolID(member.Name, SymbolKind.CBuffer, Storage.Uniform);
            var symbol = new Symbol(sid, new PointerType(member.Type, Specification.StorageClass.Uniform), variable, index);
            table.CurrentFrame.Add(member.Name, symbol);

            if (member.Attributes != null && member.Attributes.Count > 0)
            {
                foreach (var attribute in member.Attributes)
                {
                    if (attribute is AnyShaderAttribute anyAttribute && anyAttribute.Name == "Link")
                    {
                        if (anyAttribute.Parameters[0] is StringLiteral linkLiteral)
                        {
                            // Try to resolve generic parameter when encoded as string (deprecated)
                            if (table.TryResolveSymbol(linkLiteral.Value, out var linkLiteralSymbol))
                            {
                                // TODO: make it a warning only?
                                table.Errors.Add(new(Info, "LinkType generics should be passed without quotes"));
                            }

                            context.Add(new OpMemberDecorateString(context.GetOrRegister(Type), index, ParameterizedFlags.DecorationLinkSDSL(linkLiteral.Value)));
                        }
                        else if (anyAttribute.Parameters[0] is Identifier identifier)
                        {
                            if (!table.TryResolveSymbol(identifier.Name, out var linkSymbol))
                            {
                                throw new InvalidOperationException();
                            }
                            context.Add(new OpMemberDecorateString(context.GetOrRegister(Type), index, ParameterizedFlags.DecorationLinkIdSDSL(linkSymbol.IdRef)));
                        }
                        else
                        {
                            throw new NotImplementedException($"Attribute {attribute} is not supported");
                        }
                    }
                    else
                        throw new NotImplementedException($"Attribute {attribute} is not supported");
                }
            }
        }
    }
}

public sealed class RGroup(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        for (var index = 0; index < Members.Count; index++)
        {
            var member = Members[index];

            (var storageClass, var kind) = member.Type switch
            {
                TextureType => (Specification.StorageClass.UniformConstant, SymbolKind.Variable),
                SamplerType => (Specification.StorageClass.UniformConstant, SymbolKind.SamplerState),
                _ => throw new NotImplementedException(),
            };

            var type = new PointerType(member.Type, storageClass);
            var typeId = context.GetOrRegister(type);
            context.FluentAdd(new OpVariable(typeId, context.Bound++, storageClass, null), out var variable);
            context.AddName(variable.ResultId, member.Name);
            var sid = new SymbolID(member.Name, kind, Storage.Uniform);
            var symbol = new Symbol(sid, type, variable.ResultId);
            table.CurrentFrame.Add(member.Name, symbol);
        }
    }
}

public sealed class TBuffer(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
