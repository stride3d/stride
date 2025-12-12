using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using System;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class ShaderElement(TextLocation info) : Node(info)
{
    public SymbolType? Type { get; set; }

    public virtual void ProcessSymbol(SymbolTable table, SpirvContext context)
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

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        var fields = new List<(string Name, SymbolType Type, TypeModifier TypeModifier)>();
        foreach (var smem in Members)
        {
            smem.Type = smem.TypeName.ResolveType(table, context);
            table.DeclaredTypes.TryAdd(smem.Type.ToString(), smem.Type);

            fields.Add((smem.Name, smem.Type, smem.TypeModifier));
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

    public TypeModifier TypeModifier { get; set; }

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

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        var fields = new List<(string Name, SymbolType Type, TypeModifier TypeModifier)>();
        foreach (var smem in Members)
        {
            smem.Type = smem.TypeName.ResolveType(table, context);
            table.DeclaredTypes.TryAdd(smem.Type.ToString(), smem.Type);

            fields.Add((smem.Name, smem.Type, smem.TypeModifier));
        }

        Type = new StructType(TypeName.ToString() ?? "", fields);
        table.DeclaredTypes.TryAdd(TypeName.ToString(), Type);
    }

    public void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var structType = (StructType)Type;
        context.DeclareStructuredType(structType);
    }

    public override string ToString()
    {
        return $"struct {TypeName} ({string.Join(", ", Members)})";
    }
}


public sealed class CBuffer(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public static (string? LinkName, int? LinkId) ProcessLinkAttributes(SymbolTable table, TextLocation info, List<ShaderAttribute> attributes)
    {
        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                if (attribute is AnyShaderAttribute anyAttribute && anyAttribute.Name == "Link")
                {
                    if (anyAttribute.Parameters[0] is StringLiteral linkLiteral)
                    {
                        // Try to resolve generic parameter when encoded as string (deprecated)
                        if (table.TryResolveSymbol(linkLiteral.Value, out var linkLiteralSymbol))
                        {
                            // TODO: make it a warning only?
                            //table.Errors.Add(new(info, "LinkType generics should be passed without quotes"));
                            return (null, linkLiteralSymbol.IdRef);
                        }

                        return (linkLiteral.Value, null);
                    }
                    else if (anyAttribute.Parameters[0] is Identifier identifier)
                    {
                        if (!table.TryResolveSymbol(identifier.Name, out var linkSymbol))
                        {
                            throw new InvalidOperationException();
                        }
                        return (null, linkSymbol.IdRef);
                    }
                    else
                    {
                        throw new NotImplementedException($"Attribute {attribute} is not supported");
                    }
                }
            }
        }

        return (null, null);
    }

    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        context.DeclareCBuffer((ConstantBufferSymbol)Type);
        var pointerType = context.GetOrRegister(new PointerType(Type, Specification.StorageClass.Uniform));
        var variable = context.Bound++;

        bool? isStaged = null;

        for (var index = 0; index < Members.Count; index++)
        {
            var member = Members[index];

            // Use first member as reference
            if (isStaged == null)
                isStaged = member.IsStaged;
            // Make sure IsStaged for all members match the first member (they're all the same)
            if (isStaged != member.IsStaged)
                throw new InvalidOperationException($"cbuffer {Name} have a mix of stage and non-stage members");

            var sid = new SymbolID(member.Name, SymbolKind.CBuffer, Storage.Uniform);
            var symbol = new Symbol(sid, new PointerType(member.Type, Specification.StorageClass.Uniform), variable, AccessChain: index);
            table.CurrentFrame.Add(member.Name, symbol);

            if (member.Type is MatrixType)
            {
                if (member.TypeModifier != TypeModifier.ColumnMajor)
                    context.Add(new OpMemberDecorate(context.GetOrRegister(Type), index, new ParameterizedFlag<Specification.Decoration>(Specification.Decoration.ColMajor, [])));
                else if (member.TypeModifier != TypeModifier.RowMajor)
                    context.Add(new OpMemberDecorate(context.GetOrRegister(Type), index, new ParameterizedFlag<Specification.Decoration>(Specification.Decoration.RowMajor, [])));
            }

            if (member.Attributes != null && member.Attributes.Count > 0)
            {
                var linkInfo = ProcessLinkAttributes(table, Info, member.Attributes);
                if (linkInfo.LinkId is int linkId)
                    context.Add(new OpMemberDecorate(context.GetOrRegister(Type), index, ParameterizedFlags.DecorationLinkIdSDSL(linkId)));
                else if (linkInfo.LinkName != null)
                    context.Add(new OpMemberDecorateString(context.GetOrRegister(Type), index, ParameterizedFlags.DecorationLinkSDSL(linkInfo.LinkName)));
            }
        }

        // TODO: Add a StreamSDSL storage class?
        context.Add(new OpVariableSDSL(pointerType, variable, Specification.StorageClass.Uniform, isStaged == true ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None, null));
        context.AddName(variable, Name);
    }
}

public sealed class RGroup(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var splitDotIndex = Name.IndexOf('.');
        var resourceGroupName = splitDotIndex != -1 ? Name.Substring(0, splitDotIndex) : Name;
        var logicalGroupName = splitDotIndex != -1 ? Name.Substring(splitDotIndex + 1) : null;

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

            DecorateVariableLinkInfo(table, shaderClass, context, Info, member.Name, member.Attributes, variable.ResultId);

            context.Add(new OpDecorateString(variable.ResultId, ParameterizedFlags.DecorationResourceGroupSDSL(resourceGroupName)));
            if (logicalGroupName != null)
                context.Add(new OpDecorateString(variable.ResultId, ParameterizedFlags.DecorationLogicalGroupSDSL(logicalGroupName)));

            var sid = new SymbolID(member.Name, kind, Storage.Uniform);
            var symbol = new Symbol(sid, type, variable.ResultId);
            table.CurrentFrame.Add(member.Name, symbol);
        }
    }

    internal static void DecorateVariableLinkInfo(SymbolTable table, ShaderClass shaderClass, SpirvContext context, TextLocation info, string memberName, List<ShaderAttribute> attributes, int variableId)
    {
        var linkInfo = CBuffer.ProcessLinkAttributes(table, info, attributes);
        if (linkInfo.LinkId is int linkId)
            context.Add(new OpDecorate(variableId, ParameterizedFlags.DecorationLinkIdSDSL(linkId)));
        else
            context.Add(new OpDecorateString(variableId, ParameterizedFlags.DecorationLinkSDSL(linkInfo.LinkName ?? $"{shaderClass.Name}.{memberName}")));
    }
}

public sealed class TBuffer(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
