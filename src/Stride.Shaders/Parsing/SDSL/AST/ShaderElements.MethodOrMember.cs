using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class MethodOrMember(TextLocation info, bool isStaged = false) : ShaderElement(info)
{
    public bool IsStaged { get; set; } = isStaged;
    public List<ShaderAttribute> Attributes { get; set; } = [];
}


public class SamplerStateAssign(Identifier name, Expression value, TextLocation info) : ShaderElement(info)
{
    public Identifier Name { get; set; } = name;
    public Expression Value { get; set; } = value;

    public override string ToString()
    {
        return $"{Name} = {Value}";
    }
}

public class ShaderSamplerState(Identifier name, TextLocation info) : MethodOrMember(info)
{
    public Identifier Name { get; set; } = name;
    public List<SamplerStateAssign> Members { get; set; } = [];

    public override string ToString()
    {
        return $"SamplerState {Name} ({string.Join(", ", Members)})";
    }
}
public class ShaderSamplerComparisonState(Identifier name, TextLocation info) : MethodOrMember(info)
{
    public Identifier Name { get; set; } = name;
    public List<SamplerStateAssign> Members { get; set; } = [];

    public override string ToString()
    {
        return $"SamplerState {Name} ({string.Join(", ", Members)})";
    }
}


public class ShaderCompose(Identifier name, Mixin mixin, bool isArray, TextLocation info) : MethodOrMember(info)
{
    public Identifier Name { get; set; } = name;
    public Mixin Mixin { get; set; } = mixin;
    public bool IsArray { get; set; } = isArray;
    public override string ToString() => $"compose {Mixin}{(IsArray ? "[]" : "")} {Name}";
}

public sealed class ShaderMember(
        TypeName typeName,
        Identifier identifier,
        Expression? initialValue,
        TextLocation location,
        bool isStaged = false,
        StreamKind streamKind = StreamKind.None,
        Identifier? semantic = null,
        InterpolationModifier interpolation = InterpolationModifier.None,
        StorageClass storageClass = StorageClass.None,
        TypeModifier typeModifier = TypeModifier.None
    ) : MethodOrMember(location, isStaged)
{
    public Identifier Name { get; set; } = identifier;
    public TypeName TypeName { get; set; } = typeName;
    public Identifier? Semantic { get; set; } = semantic;
    public StreamKind StreamKind { get; set; } = streamKind;
    public bool IsArray => TypeName?.IsArray ?? false;
    public Expression? Value { get; set; } = initialValue;
    public TypeModifier TypeModifier { get; set; } = typeModifier;
    public StorageClass StorageClass { get; set; } = storageClass;
    public InterpolationModifier Interpolation { get; set; } = interpolation;

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        #warning replace
        // var (builder, context, _) = compiler;
        // var registeredType = context.GetOrRegister(Type);
        // var variable = context.Bound++;
        // // TODO: Add a StreamSDSL storage class?
        // context.Buffer.AddOpVariable(variable, registeredType, Specification.StorageClass.Private, null);
        // context.Variables.Add(Name, new(variable, registeredType, Name));
        // if (Semantic != null)
        //     context.Buffer.AddOpDecorateString(variable, Specification.Decoration.UserSemantic, null, null, Semantic.Name);
        // context.AddName(variable, Name);

        // var sid =
        //     new SymbolID
        //     (
        //         Name,
        //         TypeModifier == TypeModifier.Const ? SymbolKind.Constant : SymbolKind.Variable,
        //         StreamKind switch
        //         {
        //             StreamKind.Stream or StreamKind.PatchStream => Storage.Stream,
        //             _ => Storage.None
        //         }
        //     );
        // var symbol = new Symbol(sid, Type, variable);
        // table.CurrentShader.Components.Add(symbol);
        // table.CurrentFrame.Add(Name, symbol);
    }

    public override string ToString()
    {
        if (Attributes != null)
            return $"[{string.Join(" ", Attributes.Select(x => x.ToString()))}]\n{TypeName} {Name}";
        else
            return $"{StreamKind.ToString().ToLowerInvariant()} {StorageClass.ToString().ToLowerInvariant()} {TypeName} {Name}";
    }
}

public class MethodParameter(TypeName type, Identifier name, TextLocation info, string? storage = null, Expression? arraySize = null, Identifier? semantic = null) : Node(info)
{
    public TypeName TypeName { get; set; } = type;
    public SymbolType? Type { get; set; }
    public Identifier Name { get; set; } = name;
    public Identifier? Semantic { get; set; } = semantic;
    public Expression? ArraySize { get; set; } = arraySize;
    public string? Storage { get; set; } = storage;

    public override string ToString()
    {
        return $"{Type} {Name}";
    }
}

public class ShaderMethod(
        TypeName returnType,
        Identifier name,
        TextLocation info,
        Identifier? visibility = null,
        Identifier? storage = null,
        bool isStaged = false,
        bool isAbstract = false,
        bool isVirtual = false,
        bool isStatic = false,
        bool isOverride = false,
        bool isClone = false
    ) : MethodOrMember(info, isStaged)
{

    public SymbolType? ReturnType { get; set; }
    public TypeName ReturnTypeName { get; set; } = returnType;
    public Identifier Name { get; set; } = name;
    public EntryPoint EntryPoint { get; } =
        name.Name switch
        {
            "VSMain" => EntryPoint.VertexShader,
            "PSMain" => EntryPoint.PixelShader,
            "CSMain" => EntryPoint.ComputeShader,
            "GSMain" => EntryPoint.GeometryShader,
            "DSMain" => EntryPoint.DomainShader,
            "HSMain" => EntryPoint.HullShader,
            _ => 0
        };
    public Identifier? Visibility { get; set; } = visibility;
    public Identifier? Storage { get; set; } = storage;
    public bool? IsAbstract { get; set; } = isAbstract;
    public bool IsStatic { get; set; } = isStatic;
    public bool? IsVirtual { get; set; } = isVirtual;
    public bool? IsOverride { get; set; } = isOverride;
    public bool? IsClone { get; set; } = isClone;
    public List<MethodParameter> Parameters { get; set; } = [];

    public BlockStatement? Body { get; set; }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        table.Push();
        foreach (var arg in Parameters)
        {
            var argSym = arg.TypeName.ResolveType(table);
            table.DeclaredTypes.TryAdd(argSym.ToString(), argSym);
            arg.Type = argSym;
        }
        
        var (builder, context, _) = compiler;
        SpirvFunction function;
        if (Type is FunctionType ftype)
        {
            function = builder.CreateFunction(context, Name, ftype);
            foreach (var p in Parameters)
            {
                var parameterType = new PointerType(p.Type, Specification.StorageClass.Function);
                var paramValue = builder.AddFunctionParameter(context, p.Name, parameterType);
                table.CurrentFrame.Add(p.Name, new(new(p.Name, SymbolKind.Variable), parameterType, paramValue.Id));
            }

            if (Body is BlockStatement body)
            {
                table.Push();
                builder.CreateBlock(context);
                foreach (var s in body)
                    s.Compile(table, shader, compiler);
                table.Pop();
            }
            builder.EndFunction(context);
        }
        else throw new NotImplementedException();

        table.Pop();

        var symbol = new Symbol(new(Name, SymbolKind.Method), Type, function.Id);
        table.CurrentShader.Components.Add(symbol);
        table.CurrentFrame.Add(Name, symbol);
    }

    public override string ToString()
    {
        return $"{ReturnTypeName} {Name}()\n{Body}\n";
    }
}

public record struct ShaderParameter(TypeName TypeName, Identifier Name);


public abstract class ParameterListNode(TextLocation info) : Node(info);

public class ShaderParameterDeclarations(TextLocation info) : ParameterListNode(info)
{
    public List<ShaderParameter> Parameters { get; set; } = [];
}

public class ShaderExpressionList(TextLocation info) : ParameterListNode(info)
{
    public List<Expression> Values { get; set; } = [];

    public List<Expression>.Enumerator GetEnumerator() => Values.GetEnumerator();

    public override string ToString()
    {
        return string.Join(", ", Values);
    }
}
