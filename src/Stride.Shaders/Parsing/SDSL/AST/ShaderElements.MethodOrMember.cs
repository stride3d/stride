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
using CommunityToolkit.HighPerformance;
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

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        base.ProcessSymbol(table, context);
        Type = new SamplerType();
        table.DeclaredTypes.TryAdd(Type.ToString(), Type);
    }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        (var builder, var context) = compiler;
        Type = new PointerType(new SamplerType(), Specification.StorageClass.UniformConstant);
        var registeredType = context.GetOrRegister(Type);
        if (!table.RootSymbols.TryGetValue(Name, out _))
        {
            var variableId = context.Bound++;

            // We store SamplerState as decoration for later processing during ShaderMixer.ProcessReflection()
            // Note: we make sure to do it before the OpVariableSDSL as per SPIR-V spec so that it is correctly processed later
            foreach (var parameter in Parameters)
            {
                switch (parameter.Name)
                {
                    case "Filter":
                        {
                            var filter = Enum.Parse<Specification.SamplerFilterSDSL>(((Identifier)parameter.Value).Name, true);
                            context.Add(new OpDecorate(variableId, Specification.Decoration.SamplerStateFilter, [(int)filter]));
                            break;
                        }
                    case "AddressU":
                        {
                            var addressMode = Enum.Parse<Specification.SamplerTextureAddressModeSDSL>(((Identifier)parameter.Value).Name, true);
                            context.Add(new OpDecorate(variableId, Specification.Decoration.SamplerStateAddressU, [(int)addressMode]));
                            break;
                        }
                    case "AddressV":
                        {
                            var addressMode = Enum.Parse<Specification.SamplerTextureAddressModeSDSL>(((Identifier)parameter.Value).Name, true);
                            context.Add(new OpDecorate(variableId, Specification.Decoration.SamplerStateAddressV, [(int)addressMode]));
                            break;
                        }
                    case "AddressW":
                        {
                            var addressMode = Enum.Parse<Specification.SamplerTextureAddressModeSDSL>(((Identifier)parameter.Value).Name, true);
                            context.Add(new OpDecorate(variableId, Specification.Decoration.SamplerStateAddressW, [(int)addressMode]));
                            break;
                        }
                    case "MipLODBias":
                        {
                            var mipLODBias = (float)((FloatLiteral)parameter.Value).Value;
                            context.Add(new OpDecorateString(variableId, Specification.Decoration.SamplerStateMipLODBias, mipLODBias.ToString()));
                            break;
                        }
                    case "MaxAnisotropy":
                        {
                            var maxAnisotropy = ((IntegerLiteral)parameter.Value).IntValue;
                            context.Add(new OpDecorate(variableId, Specification.Decoration.SamplerStateMaxAnisotropy, [maxAnisotropy]));
                            break;
                        }
                    case "MinLOD":
                        {
                            var minLOD = (float)((FloatLiteral)parameter.Value).Value;
                            context.Add(new OpDecorateString(variableId, Specification.Decoration.SamplerStateMinLOD, minLOD.ToString()));
                            break;
                        }
                    case "MaxLOD":
                        {
                            var maxLOD = (float)((FloatLiteral)parameter.Value).Value;
                            context.Add(new OpDecorateString(variableId, Specification.Decoration.SamplerStateMaxLOD, maxLOD.ToString()));
                            break;
                        }
                    case "ComparisonFunc":
                        {
                            var filter = Enum.Parse<Specification.SamplerComparisonFuncSDSL>(((Identifier)parameter.Value).Name, true);
                            context.Add(new OpDecorate(variableId, Specification.Decoration.SamplerStateComparisonFunc, [(int)filter]));
                            break;
                        }
                    case "BorderColor":
                    default:
                        throw new NotImplementedException();
                }
            }

            var variable = builder.Insert(new OpVariableSDSL(registeredType, variableId, Specification.StorageClass.UniformConstant, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None, null));
            context.AddName(variable.ResultId, Name);
            
            RGroup.DecorateVariableLinkInfo(table, shader, context, Info, Name, Attributes, variable);

            var sid = new SymbolID(Name, SymbolKind.SamplerState);
            var symbol = new Symbol(sid, Type, variable.ResultId);
            table.CurrentShader.Variables.Add((symbol, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));
        }
        else throw new Exception($"SamplerState {Name} already defined");
    }

    public override string ToString()
    {
        return $"SamplerState {Name} ({string.Join(", ", Parameters)})";
    }
}
public class ShaderSamplerComparisonState(Identifier name, TextLocation info) : ShaderSamplerState(name, info)
{
    public override string ToString()
    {
        return $"SamplerComparisonState {Name} ({string.Join(", ", Parameters)})";
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

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        base.ProcessSymbol(table, context);
        if (!TypeName.TryResolveType(table, context, out var memberType))
        {
            if (TypeName.Name.Contains("<"))
                throw new NotImplementedException("Can't have member variables with generic shader types");
            var classSource = new ShaderClassInstantiation(TypeName.Name, []);
            var shader = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, table.CurrentMacros.AsSpan(), ResolveStep.Compile, context);
            classSource.Buffer = shader;
            var shaderType = ShaderClass.LoadAndCacheExternalShaderType(table, context, classSource);

            // Resolve again (we don't use shaderType direclty, because it might lack info such as ArrayType)
            memberType = TypeName.ResolveType(table, context);
        }

        var storageClass = (memberType, StorageClass, StreamKind) switch
        {
            (TextureType or BufferType, _, _) => Specification.StorageClass.UniformConstant,
            (StructuredBufferType, _, _) => Specification.StorageClass.StorageBuffer,
            (_, StorageClass.GroupShared, _) => Specification.StorageClass.Workgroup,
            (_, StorageClass.Static, _) => Specification.StorageClass.Private,
            (_, _, StreamKind.Stream) => Specification.StorageClass.Private,
            _ => Specification.StorageClass.Uniform, 
        };

        if (TypeModifier == TypeModifier.Const)
        {
            if (Value == null)
                throw new InvalidOperationException($"Constant {Name} doesn't have a value");
            
            // Constant: compile right away
            var constantValue = Value.CompileConstantValue(table, context, memberType);
            context.SetName(constantValue.Id, Name);
            var symbol = new Symbol(new(Name, SymbolKind.Constant), memberType, constantValue.Id);
            table.CurrentFrame.Add(Name, symbol);
            Type = memberType;

            // This constant is visible when inherited
            context.Add(new OpDecorate(constantValue.Id, Specification.Decoration.ShaderConstantSDSL, []));
        }
        else
        {
            Type = new PointerType(memberType, storageClass);
            table.DeclaredTypes.TryAdd(Type.ToString(), Type);
        }
    }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var registeredType = context.GetOrRegister(Type);
        var variable = context.Bound++;

        var pointerType = (PointerType)Type;

        var variableFlags = IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None;
        if (StreamKind == StreamKind.Stream)
            variableFlags |= Specification.VariableFlagsMask.Stream;

        int? initializerMethod = null;
        if (Value != null)
        {
            var valueType = pointerType.BaseType;

            // TODO: differentiate const from code that needs to go in entry point?
            // TODO: move to entry point
            var functionType = new FunctionType(valueType, []);
            initializerMethod = builder.Insert(new OpFunction(context.GetOrRegister(valueType), context.Bound++, Specification.FunctionControlMask.Const, context.GetOrRegister(functionType))).ResultId;
            builder.Insert(new OpLabel(context.Bound++));

            var initialValue = Value.CompileAsValue(table, compiler);
            initialValue = builder.Convert(context, initialValue, pointerType.BaseType);

            builder.Return(initialValue);
            builder.Insert(new OpFunctionEnd());

            context.AddName(initializerMethod.Value, $"{Name}_Initializer");
        }

        // Note: StorageClass was decided in Shader.Compile()
        builder.Insert(new OpVariableSDSL(registeredType, variable, pointerType.StorageClass, variableFlags, initializerMethod));
        if (Semantic != null)
            context.Add(new OpDecorateString(variable, Specification.Decoration.UserSemantic, Semantic.Name));
        context.AddName(variable, Name);

        if (pointerType.BaseType is StructuredBufferType)
        {
            context.Add(new OpDecorateString(variable, Specification.Decoration.UserTypeGOOGLE, $"structuredbuffer:<{pointerType.BaseType.ToId().ToLowerInvariant()}>"));
        }

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
        var symbol = new Symbol(sid, pointerType, variable, OwnerType: table.CurrentShader);
        table.CurrentShader.Variables.Add((symbol, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));
    }

    public override string ToString()
    {
        if (Attributes != null)
            return $"[{string.Join(" ", Attributes.Select(x => x.ToString()))}]\n{TypeName} {Name}";
        else
            return $"{StreamKind.ToString().ToLowerInvariant()} {StorageClass.ToString().ToLowerInvariant()} {TypeName} {Name}";
    }
}

public class MethodParameter(TypeName type, Identifier name, TextLocation info, ParameterModifiers modifiers = ParameterModifiers.None, Expression? defaultValue = null, Identifier? semantic = null) : Node(info)
{
    public TypeName TypeName { get; set; } = type;
    public SymbolType? Type { get; set; }
    public Identifier Name { get; set; } = name;
    public Identifier? Semantic { get; set; } = semantic;
    public Expression? DefaultValue { get; set; } = defaultValue;
    public ParameterModifiers Modifiers { get; set; } = modifiers;

    public override string ToString()
    {
        return $"{Type} {Name}{(DefaultValue != null ? $" = {DefaultValue}" : "")}";
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
    
    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        base.ProcessSymbol(table, context);
        var ftype = new FunctionType(ReturnTypeName.ResolveType(table, context), []);
        foreach (var arg in Parameters)
        {
            var argSym = arg.TypeName.ResolveType(table, context);
            table.DeclaredTypes.TryAdd(argSym.ToString(), argSym);
            arg.Type = argSym;
            ftype.ParameterTypes.Add(new(new PointerType(arg.Type, Specification.StorageClass.Function), arg.Modifiers));
        }
        Type = ftype;

        table.DeclaredTypes.TryAdd(Type.ToString(), Type);
    }

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
        
        Span<int> defaultParameters = stackalloc int[Parameters.Count];
        var firstDefaultParameter = -1;
        for (var index = 0; index < Parameters.Count; index++)
        {
            var arg = Parameters[index];
            if (firstDefaultParameter != -1 && arg.DefaultValue == null)
                throw new InvalidOperationException($"Parameter {index} ({arg.Name}) in method {Name} doesn't have a default but a previous argument had a default value");
            if (arg.DefaultValue != null)
            {
                if (firstDefaultParameter == -1)
                    firstDefaultParameter = index;
                defaultParameters[index] = arg.DefaultValue.CompileConstantValue(table, context, arg.Type).Id;
            }
        }

        var symbol = new Symbol(new(Name, SymbolKind.Method, IsStage: IsStaged), Type, function.Id, MemberAccessWithImplicitThis: Type, OwnerType: table.CurrentShader);

        if (firstDefaultParameter != -1)
        {
            context.Add(new OpDecorate(function.Id, Specification.Decoration.FunctionParameterDefaultValueSDSL, [.. defaultParameters[firstDefaultParameter..]]));

            symbol.MethodDefaultParameters = new(context, defaultParameters.Slice(firstDefaultParameter).ToArray());
        }

        table.CurrentShader.Methods.Add((symbol, functionFlags));
    }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler, bool hasUnresolvableGenerics)
    {
        var (builder, context) = compiler;

        foreach (var attribute in Attributes)
        {
            if (attribute is AnyShaderAttribute numThreads && numThreads.Name == "numthreads")
            {
                Span<int> parameters = stackalloc int[numThreads.Parameters.Count];
                for (var index = 0; index < numThreads.Parameters.Count; index++)
                {
                    var parameter = numThreads.Parameters[index];
                    
                    // TODO: avoid emitting in context (use a temp buffer?)
                    var constantArraySize = parameter.CompileConstantValue(table, context);
                    if (!context.TryGetConstantValue(constantArraySize.Id, out var value, out _, false))
                        throw new InvalidOperationException();
                    
                    parameters[index] = (int)value;
                }

                context.Add(new OpExecutionMode(function.Id, Specification.ExecutionMode.LocalSize, new(parameters)));
            }
        }

        table.Push();
        Span<int> defaultParameters = stackalloc int[Parameters.Count];
        var firstDefaultParameter = -1;
        for (var index = 0; index < Parameters.Count; index++)
        {
            var arg = Parameters[index];
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

            if (Body is BlockStatement body && !hasUnresolvableGenerics)
            {
                table.Push();
                builder.CreateBlock(context);
                foreach (var s in body)
                    s.Compile(table, compiler);
                table.Pop();
            }
            else
            {
                builder.Insert(new OpUnreachable());
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
