using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class MethodOrMember(TextLocation info, bool isStaged = false) : ShaderElement(info)
{
    public bool IsStaged { get; set; } = isStaged;
    public List<ShaderAttribute> Attributes { get; set; } = [];
}


public class SamplerStateParameter(Identifier name, Expression value, TextLocation info) : ShaderElement(info)
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
    public List<SamplerStateParameter> Parameters { get; set; } = [];

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        // TODO: sampler states with paramters not implemented
        // The main issue is that SPIR-V doesn't have a direct equivalent of sampler states with parameters.
        // We can create a basic sampler, but handling parameters would require a more complex approach,
        // potentially storing parameters in a new SDSL specific instruction or decorations

        if (Parameters.Count > 0)
            table.Errors.Add(new SemanticErrors(Info, "Sampler states with parameters are not supported in SPIR-V generation."));

        (var builder, var context) = compiler;
        Type = new PointerType(new SamplerType(), Specification.StorageClass.UniformConstant);
        var registeredType = context.GetOrRegister(Type);
        if (!table.RootSymbols.TryGetValue(Name, out _))
        {
            var register = builder.Insert(new OpVariableSDSL(registeredType, context.Bound++, Specification.StorageClass.UniformConstant, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None, null));
            context.AddName(register.ResultId, Name);

            var sid = new SymbolID(Name, SymbolKind.SamplerState);
            var symbol = new Symbol(sid, Type, register.ResultId);
            table.CurrentShader.Variables.Add((symbol, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));
            table.CurrentFrame.Add(Name, symbol);
        }
        else throw new Exception($"SamplerState {Name} already defined");
    }

    public override string ToString()
    {
        return $"SamplerState {Name} ({string.Join(", ", Parameters)})";
    }
}
public class ShaderSamplerComparisonState(Identifier name, TextLocation info) : MethodOrMember(info)
{
    public Identifier Name { get; set; } = name;
    public List<SamplerStateParameter> Members { get; set; } = [];

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
    public bool IsCompose { get; set; }
    public bool IsArray => TypeName?.IsArray ?? false;
    public Expression? Value { get; set; } = initialValue;
    public TypeModifier TypeModifier { get; set; } = typeModifier;
    public StorageClass StorageClass { get; set; } = storageClass;
    public InterpolationModifier Interpolation { get; set; } = interpolation;

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var registeredType = context.GetOrRegister(Type);
        var variable = context.Bound++;

        // TODO: Add a StreamSDSL storage class?
        var storageClass = Specification.StorageClass.Private;
        if (Type is PointerType pointerType)
            storageClass = pointerType.StorageClass;

        var variableFlags = IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None;

        int? initializerMethod = null;
        if (Value != null)
        {
            var valueType = ((PointerType)Type).BaseType;

            // TODO: differentiate const from code that needs to go in entry point?
            // TODO: move to entry point
            var functionType = new FunctionType(valueType, []);
            initializerMethod = builder.Insert(new OpFunction(context.GetOrRegister(valueType), context.Bound++, Specification.FunctionControlMask.Const, context.GetOrRegister(functionType))).ResultId;
            builder.Insert(new OpLabel(context.Bound++));

            var initialValue = Value.CompileAsValue(table, compiler);
            initialValue = builder.Convert(context, initialValue, ((PointerType)Type).BaseType);

            builder.Return(initialValue);
            builder.Insert(new OpFunctionEnd());

            context.AddName(initializerMethod.Value, $"{Name}_Initializer");
        }

        builder.Insert(new OpVariableSDSL(registeredType, variable, storageClass, variableFlags, initializerMethod));
        if (Semantic != null)
            context.Add(new OpDecorateString(variable, ParameterizedFlags.DecorationUserSemantic(Semantic.Name)));
        context.AddName(variable, Name);

        RGroup.DecorateVariableLinkInfo(table, shader, context, Info, Name, Attributes, variable);

        var sid =
            new SymbolID
            (
                Name,
                TypeModifier == TypeModifier.Const ? SymbolKind.Constant : SymbolKind.Variable,
                StreamKind switch
                {
                    StreamKind.Stream or StreamKind.PatchStream => Storage.Stream,
                    _ => Storage.None
                },
                IsStage: IsStaged
            );
        var symbol = new Symbol(sid, Type, variable);
        table.CurrentShader.Variables.Add((symbol, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));
        table.CurrentFrame.Add(Name, symbol);
    }

    public override string ToString()
    {
        if (Attributes != null)
            return $"[{string.Join(" ", Attributes.Select(x => x.ToString()))}]\n{TypeName} {Name}";
        else
            return $"{StreamKind.ToString().ToLowerInvariant()} {StorageClass.ToString().ToLowerInvariant()} {TypeName} {Name}";
    }
}

public class MethodParameter(TypeName type, Identifier name, TextLocation info, ParameterModifiers modifiers = ParameterModifiers.None, Expression? arraySize = null, Identifier? semantic = null) : Node(info)
{
    public TypeName TypeName { get; set; } = type;
    public SymbolType? Type { get; set; }
    public Identifier Name { get; set; } = name;
    public Identifier? Semantic { get; set; } = semantic;
    public Expression? ArraySize { get; set; } = arraySize;
    public ParameterModifiers Modifiers { get; set; } = modifiers;

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
    // Saved between Declare and Compile pass
    private SpirvFunction function;

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
    public bool IsAbstract { get; set; } = isAbstract;
    public bool IsStatic { get; set; } = isStatic;
    public bool IsVirtual { get; set; } = isVirtual;
    public bool IsOverride { get; set; } = isOverride;
    public bool IsClone { get; set; } = isClone;
    public List<MethodParameter> Parameters { get; set; } = [];

    public BlockStatement? Body { get; set; }

    public void Declare(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        function = builder.DeclareFunction(context, Name, (FunctionType)Type, IsStaged);

        var functionFlags = Specification.FunctionFlagsMask.None;
        if (IsAbstract)
            functionFlags |= Specification.FunctionFlagsMask.Abstract;
        if (IsOverride)
            functionFlags |= Specification.FunctionFlagsMask.Override;
        if (IsVirtual)
            functionFlags |= Specification.FunctionFlagsMask.Virtual;
        if (IsStaged)
            functionFlags |= Specification.FunctionFlagsMask.Stage;

        var symbol = new Symbol(new(Name, SymbolKind.Method, IsStage: IsStaged), Type, function.Id, MemberAccessWithImplicitThis: Type);
        table.CurrentShader.Methods.Add((symbol, functionFlags));
        table.CurrentFrame.Add(Name, symbol);
    }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        table.Push();
        foreach (var arg in Parameters)
        {
            var argSym = arg.TypeName.ResolveType(table, context);
            table.DeclaredTypes.TryAdd(argSym.ToString(), argSym);
            arg.Type = argSym;
        }

        if (Type is FunctionType ftype)
        {
            builder.BeginFunction(context, function);

            var functionInfo = new OpSDSLFunctionInfo(Specification.FunctionFlagsMask.None, 0);

            if (IsOverride == true)
            {
                // Find parent function
                var parentSymbol = table.ResolveSymbol(function.Name);
                // TODO: find proper overload
                if (parentSymbol.Type is FunctionGroupType)
                    parentSymbol = parentSymbol.GroupMembers.Last(x => x.IdRef != function.Id && (FunctionType)x.Type == function.FunctionType);

                functionInfo.Parent = parentSymbol.IdRef;
                functionInfo.Flags |= Specification.FunctionFlagsMask.Override;
            }

            if (IsAbstract == true)
                functionInfo.Flags |= Specification.FunctionFlagsMask.Abstract;
            if (IsVirtual == true)
                functionInfo.Flags |= Specification.FunctionFlagsMask.Virtual;
            if (IsStaged)
                functionInfo.Flags |= Specification.FunctionFlagsMask.Stage;

            builder.Insert(functionInfo);

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
                    s.Compile(table, compiler);
                table.Pop();
            }
            builder.EndFunction();
        }
        else throw new NotImplementedException();

        table.Pop();
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
