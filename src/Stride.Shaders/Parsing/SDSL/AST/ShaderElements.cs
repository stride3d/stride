using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using System;
using System.Collections.Generic;

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

/// <summary>
/// Note: row/column major is defined from HLSL point of view (SPIR-V will have opposite)
/// </summary>
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

[Flags]
public enum ParameterModifiers : int
{
    None = 0x0,
    In = 0x1,
    Out = 0x2,
    InOut = In | Out,

    Const = 0x10,
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

    public static ParameterModifiers ToParameterModifiers(this string str)
    {
        return str switch
        {
            "in" => ParameterModifiers.In,
            "out" => ParameterModifiers.Out,
            "inout" => ParameterModifiers.InOut,
            "const" => ParameterModifiers.Const,
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

public abstract class ShaderBuffer : ShaderElement
{
    public string Name { get; set; }
    public string? LogicalGroup { get; } = null;
    public List<ShaderMember> Members { get; set; } = [];

    public ShaderBuffer(string name, TextLocation info) : base(info)
    {
        var dotIndex = name.IndexOf('.');
        Name = dotIndex != -1 ? name.Substring(0, dotIndex) : name;
        LogicalGroup = dotIndex != -1 ? name.Substring(dotIndex + 1) : null;
    }

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        var fields = new List<StructuredTypeMember>();
        foreach (var smem in Members)
        {
            smem.Type = smem.TypeName.ResolveType(table, context);
            table.DeclaredTypes.TryAdd(smem.Type.ToString(), smem.Type);

            fields.Add(new(smem.Name, smem.Type, smem.TypeModifier));
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
        base.ProcessSymbol(table, context);
        var fields = new List<StructuredTypeMember>();
        foreach (var smem in Members)
        {
            smem.Type = smem.TypeName.ResolveType(table, context);
            table.DeclaredTypes.TryAdd(smem.Type.ToString(), smem.Type);

            fields.Add(new(smem.Name, smem.Type, smem.TypeModifier));
        }

        Type = new StructType(TypeName.ToString(), fields);
        table.DeclaredTypes.Add(TypeName.ToString(), Type);
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

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        base.ProcessSymbol(table, context);
        foreach (var cbMember in Members)
            cbMember.Type = cbMember.TypeName.ResolveType(table, context);
    }

    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var constantBufferType = (ConstantBufferSymbol)Type;

        // We try to avoid clash in case multiple cbuffer TYPE with same name
        // The variable itself is handled by adding a .0 .1 etc. in Shader.RenameCBufferVariables()
        int tryCount = 0;
        var typeName = constantBufferType.Name;
        while (!table.DeclaredTypes.TryAdd(constantBufferType.ToId(), Type))
        {
            typeName = $"{typeName}_{++tryCount}";
            constantBufferType = constantBufferType with { Name = typeName };
        }
        Type = constantBufferType;

        context.DeclareCBuffer(constantBufferType);
        var pointerType = new PointerType(Type, Specification.StorageClass.Uniform);
        var variable = context.Bound++;

        context.AddName(variable, Name);
        if (LogicalGroup != null)
            context.Add(new OpDecorateString(variable, Specification.Decoration.LogicalGroupSDSL, LogicalGroup));

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

            if (member.Attributes != null && member.Attributes.Count > 0)
            {
                var linkInfo = ProcessLinkAttributes(table, Info, member.Attributes);
                if (linkInfo.LinkId is int linkId)
                    context.Add(new OpMemberDecorate(context.GetOrRegister(Type), index, Specification.Decoration.LinkIdSDSL, [linkId]));
                else if (linkInfo.LinkName != null)
                    context.Add(new OpMemberDecorateString(context.GetOrRegister(Type), index, Specification.Decoration.LinkSDSL, linkInfo.LinkName));
            }
        }

        builder.Insert(new OpVariableSDSL(context.GetOrRegister(pointerType), variable, Specification.StorageClass.Uniform, isStaged == true ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None, null));

        var sid = new SymbolID(Name, SymbolKind.CBuffer, Storage.Uniform);
        var symbol = new Symbol(sid, pointerType, variable);
        table.CurrentShader.Variables.Add((symbol, (isStaged ?? false) ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));
    }
}

public sealed class RGroup(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var resourceGroupId = context.ResourceGroupBound++;

        for (var index = 0; index < Members.Count; index++)
        {
            var member = Members[index];

            (var storageClass, var kind) = member.Type switch
            {
                TextureType => (Specification.StorageClass.UniformConstant, SymbolKind.Variable),
                SamplerType => (Specification.StorageClass.UniformConstant, SymbolKind.SamplerState),
                BufferType => (Specification.StorageClass.UniformConstant, SymbolKind.TBuffer),
                _ => throw new NotImplementedException(),
            };

            var type = new PointerType(member.Type, storageClass);
            var typeId = context.GetOrRegister(type);
            var variable = builder.Insert(new OpVariableSDSL(typeId, context.Bound++, storageClass, member.IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None, null));
            context.AddName(variable.ResultId, member.Name);

            DecorateVariableLinkInfo(table, shaderClass, context, Info, member.Name, member.Attributes, variable.ResultId);

            context.Add(new OpDecorateString(variable.ResultId, Specification.Decoration.ResourceGroupSDSL, Name));
            // We also store an ID because multiple rgroup might have the same name,
            // but we still want to know which one was in the same "block" when we try to optimize them (we can only optimize a resource if all the resource in the same rgroup block can be optimized)
            context.Add(new OpDecorate(variable.ResultId, Specification.Decoration.ResourceGroupIdSDSL, [resourceGroupId]));
            if (LogicalGroup != null)
                context.Add(new OpDecorateString(variable.ResultId, Specification.Decoration.LogicalGroupSDSL, LogicalGroup));

            var sid = new SymbolID(member.Name, kind, Storage.Uniform);
            var symbol = new Symbol(sid, type, variable.ResultId);
            table.CurrentShader.Variables.Add((symbol, member.IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));
        }
    }

    internal static void DecorateVariableLinkInfo(SymbolTable table, ShaderClass shaderClass, SpirvContext context, TextLocation info, string memberName, List<ShaderAttribute> attributes, int variableId)
    {
        var linkInfo = CBuffer.ProcessLinkAttributes(table, info, attributes);
        if (linkInfo.LinkId is int linkId)
            context.Add(new OpDecorate(variableId, Specification.Decoration.LinkIdSDSL, [linkId]));
        else if (linkInfo.LinkName != null)
            context.Add(new OpDecorateString(variableId, Specification.Decoration.LinkSDSL, linkInfo.LinkName));
    }
}

public sealed class TBuffer(string name, TextLocation info) : ShaderBuffer(name, info)
{
    public override void Compile(SymbolTable table, ShaderClass shaderClass, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
