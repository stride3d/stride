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
    public List<ShaderAttribute>? Attributes { get; set; } = null;
}


public partial class SamplerStateParameter(Identifier name, Expression value, TextLocation info) : ShaderElement(info)
{
    public Identifier Name { get; set; } = name;
    public Expression Value { get; set; } = value;

    public override string ToString()
    {
        return $"{Name} = {Value}";
    }
}

public partial class ShaderSamplerState(Identifier name, TextLocation info) : MethodOrMember(info)
{
    public Identifier Name { get; set; } = name;
    public List<SamplerStateParameter> Parameters { get; set; } = [];

    public Symbol? Symbol { get; private set; }

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        base.ProcessSymbol(table, context);
        Type = new PointerType(new SamplerType(), Specification.StorageClass.UniformConstant);
        table.DeclaredTypes.TryAdd(Type.ToString(), Type);

        var sid = new SymbolID(Name, SymbolKind.SamplerState);
        Symbol = new Symbol(sid, Type, 0, OwnerType: table.CurrentShader);
        table.CurrentShader!.Variables.Add((Symbol, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));
    }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        (var builder, var context) = compiler;
        var registeredType = context.GetOrRegister(Type);
        if (table.RootSymbols.TryGetValue(Name, out _))
            throw new Exception($"SamplerState {Name} already defined");

        var variableId = context.Bound++;

        // Encode all sampler state fields into a single OpDecorate
        // Defaults match SamplerStateDescription.Default: Linear(21) filter, Clamp(3) addressing, no LOD bias, 16x aniso, Never(1) compare, full LOD range
        int filter = 21; // TextureFilter.Linear = MIN_MAG_MIP_LINEAR
        int addressU = (int)Specification.SamplerTextureAddressModeSDSL.Clamp;
        int addressV = (int)Specification.SamplerTextureAddressModeSDSL.Clamp;
        int addressW = (int)Specification.SamplerTextureAddressModeSDSL.Clamp;
        int mipLODBias = 0; // BitConverter.SingleToInt32Bits(0.0f) == 0
        int maxAnisotropy = 16;
        int comparisonFunc = (int)Specification.SamplerComparisonFuncSDSL.Never;
        int minLOD = BitConverter.SingleToInt32Bits(-float.MaxValue);
        int maxLOD = BitConverter.SingleToInt32Bits(float.MaxValue);
        int borderR = 0, borderG = 0, borderB = 0, borderA = 0; // Black (0,0,0,0)
        foreach (var parameter in Parameters)
        {
            switch (parameter.Name)
            {
                case "Filter":
                    filter = (int)Enum.Parse<Specification.SamplerFilterSDSL>(((Identifier)parameter.Value).Name, true);
                    break;
                case "AddressU":
                    addressU = (int)Enum.Parse<Specification.SamplerTextureAddressModeSDSL>(((Identifier)parameter.Value).Name, true);
                    break;
                case "AddressV":
                    addressV = (int)Enum.Parse<Specification.SamplerTextureAddressModeSDSL>(((Identifier)parameter.Value).Name, true);
                    break;
                case "AddressW":
                    addressW = (int)Enum.Parse<Specification.SamplerTextureAddressModeSDSL>(((Identifier)parameter.Value).Name, true);
                    break;
                case "MipLODBias":
                    mipLODBias = BitConverter.SingleToInt32Bits((float)((FloatLiteral)parameter.Value).Value);
                    break;
                case "MaxAnisotropy":
                    maxAnisotropy = ((IntegerLiteral)parameter.Value).IntValue;
                    break;
                case "ComparisonFunc":
                    comparisonFunc = (int)Enum.Parse<Specification.SamplerComparisonFuncSDSL>(((Identifier)parameter.Value).Name, true);
                    break;
                case "MinLOD":
                    minLOD = BitConverter.SingleToInt32Bits((float)((FloatLiteral)parameter.Value).Value);
                    break;
                case "MaxLOD":
                    maxLOD = BitConverter.SingleToInt32Bits((float)((FloatLiteral)parameter.Value).Value);
                    break;
                case "BorderColor":
                    {
                        if (parameter.Value is not VectorLiteral { TypeName.Name: "float4", Values: { Count: 4 } args })
                            throw new NotSupportedException($"BorderColor must be float4(r, g, b, a)");
                        borderR = BitConverter.SingleToInt32Bits((float)((NumberLiteral)args[0]).DoubleValue);
                        borderG = BitConverter.SingleToInt32Bits((float)((NumberLiteral)args[1]).DoubleValue);
                        borderB = BitConverter.SingleToInt32Bits((float)((NumberLiteral)args[2]).DoubleValue);
                        borderA = BitConverter.SingleToInt32Bits((float)((NumberLiteral)args[3]).DoubleValue);
                        break;
                    }
                default:
                    throw new NotImplementedException($"SamplerState parameter '{parameter.Name}' not implemented");
            }
        }
        // Only emit immutable sampler state when the declaration has explicit parameters.
        // Samplers without inline state (e.g. "stage SamplerState Sampler;") are dynamic
        // and set at runtime via Parameters.Set().
        if (Parameters.Count > 0)
        {
            context.Add(new OpDecorate(variableId, Specification.Decoration.SamplerStateSDSL, [
                filter, addressU, addressV, addressW, mipLODBias, maxAnisotropy, comparisonFunc, minLOD, maxLOD,
                borderR, borderG, borderB, borderA
            ]));
        }

        var variable = builder.Insert(new OpVariableSDSL(registeredType, variableId, Specification.StorageClass.UniformConstant, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None, null));
        context.AddName(variable.ResultId, Name);
        Symbol!.IdRef = variableId;

        RGroup.DecorateVariableLinkInfo(table, shader, context, Info, Name, Attributes, variable);
    }

    public override string ToString()
    {
        return $"SamplerState {Name} ({string.Join(", ", Parameters)})";
    }
}
public partial class ShaderSamplerComparisonState(Identifier name, TextLocation info) : ShaderSamplerState(name, info)
{
    public override string ToString()
    {
        return $"SamplerComparisonState {Name} ({string.Join(", ", Parameters)})";
    }
}


public partial class ShaderCompose(Identifier name, TypeName shader, bool isArray, TextLocation info) : MethodOrMember(info)
{
    public Identifier Name { get; set; } = name;
    public TypeName Shader { get; set; } = shader;
    public bool IsArray { get; set; } = isArray;
    public override string ToString() => $"compose {Shader}{(IsArray ? "[]" : "")} {Name}";
}

public sealed partial class ShaderMember(
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

    public Symbol? Symbol { get; private set; }

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        base.ProcessSymbol(table, context);
        foreach (var generic in TypeName.Generics)
            generic.ProcessSymbol(table);
        if (!TypeName.TryResolveType(table, context, out var memberType))
        {
            if (TypeName.Name.Contains("<"))
                throw new NotImplementedException("Can't have member variables with generic shader types");
            var classSource = new ShaderClassInstantiation(TypeName.Name, []);
            var shader = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, table.CurrentMacros.AsSpan(), ResolveStep.Compile, context);
            classSource.Buffer = shader;
            var shaderType = ShaderClass.LoadAndCacheExternalShaderDefinition(table, context, classSource);

            // Resolve again (we don't use shaderType directly, because it might lack info such as ArrayType)
            TypeName.ProcessSymbol(table);
            memberType = TypeName.Type!;
        }

        if (memberType is AppendStructuredBufferType or ConsumeStructuredBufferType)
        {
            var bufTypeName = memberType is AppendStructuredBufferType ? "AppendStructuredBuffer" : "ConsumeStructuredBuffer";
            table.AddError(new(TypeName.Info, $"{bufTypeName} is not supported. Use RWStructuredBuffer with a separate counter buffer instead (variable '{Name}')."));
            return;
        }

        var storageClass = (memberType, StorageClass, StreamKind) switch
        {
            (TextureType or BufferType, _, _) => Specification.StorageClass.UniformConstant,
            (StructuredBufferType or ByteAddressBufferType, _, _) => Specification.StorageClass.StorageBuffer,
            (_, StorageClass.GroupShared, _) => Specification.StorageClass.Workgroup,
            (_, StorageClass.Static, _) => Specification.StorageClass.Private,
            (_, _, StreamKind.Stream or StreamKind.PatchStream) => Specification.StorageClass.Private,
            _ => Specification.StorageClass.Uniform,
        };

        if (TypeModifier == TypeModifier.Const)
        {
            if (Value == null)
                throw new InvalidOperationException($"Constant {Name} doesn't have a value");

            // Constant: compile right away
            var constantValue = Value.CompileConstantValue(table, context, memberType);
            // Infer size for unsized arrays (e.g. `static const uint info[] = {...};`) from
            // the initializer; otherwise indexing the constant later allocates a temp variable
            // typed as runtime array, mismatching the OpSpecConstantComposite's sized OpTypeArray.
            if (memberType is ArrayType { Size: -1 } && Value.ValueType is ArrayType { Size: > 0 } inferred)
                memberType = inferred;
            context.SetName(constantValue.Id, Name);
            var constant = new Symbol(new(Name, SymbolKind.Constant), memberType, constantValue.Id, OwnerType: table.CurrentShader);
            table.CurrentFrame.Add(Name, constant);
            Type = memberType;

            // This constant is visible when inherited (name stored in decoration to avoid dedup conflicts)
            context.Add(new OpDecorateString(constantValue.Id, Specification.Decoration.ShaderConstantSDSL, Name));
        }
        else
        {
            Type = new PointerType(memberType, storageClass);
            table.DeclaredTypes.TryAdd(Type.ToString(), Type);
        }

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
        Symbol = new Symbol(sid, Type, 0, OwnerType: table.CurrentShader!);
        table.CurrentShader!.Variables.Add((Symbol, IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None));

        Value?.ProcessSymbol(table, memberType);
    }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var registeredType = context.GetOrRegister(Type);
        var variable = context.Bound++;

        var pointerType = (PointerType)Type!;

        var variableFlags = IsStaged ? Specification.VariableFlagsMask.Stage : Specification.VariableFlagsMask.None;
        if (StreamKind == StreamKind.Stream || StreamKind == StreamKind.PatchStream)
            variableFlags |= Specification.VariableFlagsMask.Stream;

        int? initializerId = null;
        if (Value != null)
        {
            var valueType = pointerType.BaseType;

            if (pointerType.StorageClass == Specification.StorageClass.Uniform)
            {
                // Uniform variables become cbuffer members — their default values are set from the CPU side.
                // Compile the initializer as a constant value (no function wrapper needed).
                var constantValue = Value.CompileConstantValue(table, context, valueType);
                initializerId = constantValue.Id;
            }
            else
            {
                // For other storage classes, wrap in an initializer method called from the entry point wrapper.
                // This is necessary in case they can't be created as pure constant.
                // TODO: some of them could become proper const, we could simplify those and use simpler system with constant ID (like StorageClass.Uniform)
                var functionType = new FunctionType(valueType, []);
                initializerId = builder.Insert(new OpFunction(context.GetOrRegister(valueType), context.Bound++, Specification.FunctionControlMask.Const, context.GetOrRegister(functionType))).ResultId;
                builder.Insert(new OpLabel(context.Bound++));

                var initialValue = Value.CompileAsValue(table, compiler);
                initialValue = builder.Convert(context, initialValue, pointerType.BaseType);

                builder.Return(initialValue);
                builder.Insert(new OpFunctionEnd());

                context.AddName(initializerId.Value, $"{Name}_Initializer");
            }
        }

        // Note: StorageClass was decided in Shader.Compile()
        builder.Insert(new OpVariableSDSL(registeredType, variable, pointerType.StorageClass, variableFlags, initializerId));
        if (Semantic != null)
            context.Add(new OpDecorateString(variable, Specification.Decoration.UserSemantic, Semantic.Name));
        context.AddName(variable, Name);

        Symbol!.IdRef = variable;

        if (StreamKind == StreamKind.PatchStream)
            context.Add(new OpDecorate(variable, Specification.Decoration.Patch, []));

        if (pointerType.BaseType is StructuredBufferType sb)
            context.Add(new OpDecorateString(variable, Specification.Decoration.UserTypeGOOGLE, $"{(sb.WriteAllowed ? "rw" : "")}structuredbuffer:<{sb.BaseType.ToId().ToLowerInvariant()}>"));
        else if (pointerType.BaseType is ByteAddressBufferType bab)
            context.Add(new OpDecorateString(variable, Specification.Decoration.UserTypeGOOGLE, bab.WriteAllowed ? "rwbyteaddressbuffer" : "byteaddressbuffer"));

        if (pointerType.BaseType is ByteAddressBufferType { WriteAllowed: false } or StructuredBufferType { WriteAllowed: false })
            context.Add(new OpDecorate(variable, Specification.Decoration.NonWritable, []));

        RGroup.DecorateVariableLinkInfo(table, shader, context, Info, Name, Attributes, variable);
    }

    public override string ToString()
    {
        if (Attributes != null)
            return $"[{string.Join(" ", Attributes.Select(x => x.ToString()))}]\n{TypeName} {Name}";
        else
            return $"{StreamKind.ToString().ToLowerInvariant()} {StorageClass.ToString().ToLowerInvariant()} {TypeName} {Name}";
    }
}

public partial class MethodParameter(TypeName type, Identifier name, TextLocation info, ParameterModifiers modifiers = ParameterModifiers.None, Expression? defaultValue = null, Identifier? semantic = null) : Node(info)
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

public partial class ShaderMethod(
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

    public SymbolFrame? SymbolFrame { get; private set; }
    public List<Symbol> ParameterSymbols { get; private set; } = new();

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        ReturnTypeName.ProcessSymbol(table);
        var ftype = new FunctionType(ReturnTypeName.Type!, []);
        function = SpirvBuilder.DeclareFunction(context, Name, ftype, IsStaged);

        var functionFlags = Specification.FunctionFlagsMask.None;
        if (IsAbstract)
            functionFlags |= Specification.FunctionFlagsMask.Abstract;
        if (IsOverride)
            functionFlags |= Specification.FunctionFlagsMask.Override;
        if (IsVirtual)
            functionFlags |= Specification.FunctionFlagsMask.Virtual;
        if (IsStaged)
            functionFlags |= Specification.FunctionFlagsMask.Stage;

        table.Push();
        ParameterSymbols.Clear();
        foreach (var p in Parameters)
        {
            p.TypeName.ProcessSymbol(table);
            var argSym = p.TypeName.Type!;
            table.DeclaredTypes.TryAdd(argSym.ToString(), argSym);
            p.Type = argSym;
            var parameterType = GenerateParameterType(p);
            ftype.ParameterTypes.Add(new(parameterType, p.Modifiers));
            var parameterSymbol = new Symbol(new(p.Name, SymbolKind.Variable), parameterType, 0, OwnerType: table.CurrentShader);
            table.CurrentFrame.Add(p.Name, parameterSymbol);
            ParameterSymbols.Add(parameterSymbol);
        }

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

            if (arg.Semantic != null)
            {
                // We use OpMemberDecorateString on the function ID
                // but this is not valid so we'll need to make sure to remove that after the ShaderMixer
                context.Add(new OpMemberDecorateString(function.Id, index, Specification.Decoration.UserSemantic, arg.Semantic));
            }
        }

        var symbol = new Symbol(new(Name, SymbolKind.Method, IsStage: IsStaged), ftype, function.Id, MemberAccessWithImplicitThis: ftype, OwnerType: table.CurrentShader);

        if (firstDefaultParameter != -1)
        {
            context.Add(new OpDecorate(function.Id, Specification.Decoration.FunctionParameterDefaultValueSDSL, [.. defaultParameters[firstDefaultParameter..]]));

            var defaultExprs = new ConstantExpression[defaultParameters.Length - firstDefaultParameter];
            for (int i = 0; i < defaultExprs.Length; i++)
                defaultExprs[i] = ConstantExpression.ParseFromBuffer(defaultParameters[firstDefaultParameter + i], context.GetBuffer(), context);

            symbol = symbol with
            {
                MethodDefaultParameters = new(defaultExprs),
            };
        }

        Type = ftype;
        table.DeclaredTypes.TryAdd(Type.ToString(), Type);

        SymbolFrame = table.Pop();
        table.CurrentShader!.Methods.Add((symbol, functionFlags));
    }

    // SPIR-V spec: SpacingX / VertexOrderX / PointMode execution modes are only valid on
    // TessellationEvaluation entry points. HLSL places them on the hull shader function,
    // so for spec compliance we emit them on DSMain's function id instead. Throws if
    // no DSMain method is declared — a tessellation control shader without a matching
    // evaluation shader cannot produce valid SPIR-V anyway.
    private static int GetTessEvaluationFunctionId(SymbolTable table, int fallbackId)
    {
        if (table.CurrentShader != null)
        {
            foreach (var (symbol, _) in table.CurrentShader.Methods)
            {
                if (symbol.Id.Name == "DSMain")
                    return symbol.IdRef;
            }
        }
        return fallbackId;
    }

    private static PointerType GenerateParameterType(MethodParameter p)
    {
        // Opaque types (image/sampler) must use UniformConstant storage class —
        // Vulkan forbids OpStore to these types (VUID-StandaloneSpirv-OpTypeImage-06924),
        // so they cannot be copied into Function-storage variables.
        if (p.Type is TextureType or SamplerType)
            return new PointerType(p.Type!, Specification.StorageClass.UniformConstant);

        return new PointerType(p.Type!, Specification.StorageClass.Function);
    }

    public void ProcessSymbolBody(SymbolTable table, SpirvContext context)
    {
        table.Push(SymbolFrame!);
        Body?.ProcessSymbol(table);
        table.Pop();
    }

    public void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler, bool hasUnresolvableGenerics)
    {
        var (builder, context) = compiler;

        if (Attributes != null)
        {
            Span<int> attrParamBuffer = stackalloc int[8]; // max attribute parameters
            foreach (var attribute in Attributes)
            {
                if (attribute is AnyShaderAttribute anyAttribute)
                {
                    if (anyAttribute.Name == "numthreads")
                    {
                        var parameters = attrParamBuffer[..anyAttribute.Parameters.Count];
                        for (var index = 0; index < anyAttribute.Parameters.Count; index++)
                        {
                            var compiled = anyAttribute.Parameters[index].CompileConstantValue(table, context);
                            var expr = ConstantExpression.ParseFromBuffer(compiled.Id, context.GetBuffer(), context);
                            if (!expr.TryEvaluate(out var value) || value is null)
                                throw new InvalidOperationException();
                            parameters[index] = Convert.ToInt32(value);
                        }

                        context.Add(new OpExecutionMode(function.Id, Specification.ExecutionMode.LocalSize, new(parameters)));
                    }
                    else if (anyAttribute.Name == "maxvertexcount")
                    {
                        var compiled = anyAttribute.Parameters[0].CompileConstantValue(table, context);
                        var expr = ConstantExpression.ParseFromBuffer(compiled.Id, context.GetBuffer(), context);
                        if (!expr.TryEvaluate(out var value) || value is null)
                            throw new InvalidOperationException();
                        context.Add(new OpExecutionMode(function.Id, Specification.ExecutionMode.OutputVertices, new(Convert.ToInt32(value))));
                    }
                    else if (anyAttribute.Name == "outputcontrolpoints")
                    {
                        var compiled = anyAttribute.Parameters[0].CompileConstantValue(table, context);
                        var expr = ConstantExpression.ParseFromBuffer(compiled.Id, context.GetBuffer(), context);
                        if (!expr.TryEvaluate(out var value) || value is null)
                            throw new InvalidOperationException();
                        context.Add(new OpExecutionMode(function.Id, Specification.ExecutionMode.OutputVertices, new(Convert.ToInt32(value))));
                    }
                    else if (anyAttribute.Name == "patchconstantfunc")
                    {
                        context.Add(new OpDecorateString(function.Id, Specification.Decoration.PatchConstantFuncSDSL, ((StringLiteral)anyAttribute.Parameters[0]).Value));
                    }
                    else if (anyAttribute.Name == "domain")
                    {
                        // Triangles/Quads/Isolines are valid on either TCS or TES per spec.
                        // We emit them only on DSMain (TES), matching glslang's convention
                        // and keeping the SPIR-V minimal. HLSL's [domain] on HS is therefore
                        // skipped here; HLSL also requires [domain] on DS, so DSMain's own
                        // [domain] attribute guarantees the mode ends up in the module.
                        if (EntryPoint != EntryPoint.HullShader)
                        {
                            context.Add(new OpExecutionMode(function.Id, ((StringLiteral)anyAttribute.Parameters[0]).Value switch
                            {
                                "tri" => Specification.ExecutionMode.Triangles,
                                "quad" => Specification.ExecutionMode.Quads,
                                "isolined" => Specification.ExecutionMode.Isolines,
                                _ => throw new NotSupportedException($"Unsupported domain value '{((StringLiteral)anyAttribute.Parameters[0]).Value}'"),
                            }, []));
                        }
                    }
                    else if (anyAttribute.Name == "partitioning")
                    {
                        // Spacing execution modes are only valid on TessellationEvaluation
                        // per SPIR-V spec, but HLSL puts [partitioning] on the hull shader.
                        // Emit the mode on DSMain's function id so the SPIR-V is spec-compliant.
                        context.Add(new OpExecutionMode(GetTessEvaluationFunctionId(table, function.Id), ((StringLiteral)anyAttribute.Parameters[0]).Value switch
                        {
                            "fractional_odd" => Specification.ExecutionMode.SpacingFractionalOdd,
                            "fractional_even" => Specification.ExecutionMode.SpacingFractionalEven,
                            "integer" => Specification.ExecutionMode.SpacingEqual,
                            "pow2" => throw new NotSupportedException("partitioning pow2 is not supported in SPIR-V"),
                            _ => throw new NotSupportedException($"Unsupported partitioning value '{((StringLiteral)anyAttribute.Parameters[0]).Value}'"),
                        }, []));
                    }
                    else if (anyAttribute.Name == "outputtopology")
                    {
                        var value = ((StringLiteral)anyAttribute.Parameters[0]).Value;
                        if (value != "line")
                        {
                            // VertexOrderCw/Ccw are only valid on TessellationEvaluation per
                            // SPIR-V spec; route to DSMain (same reason as partitioning above).
                            context.Add(new OpExecutionMode(GetTessEvaluationFunctionId(table, function.Id), ((StringLiteral)anyAttribute.Parameters[0]).Value switch
                            {
                                "triangle_cw" => Specification.ExecutionMode.VertexOrderCw,
                                "triangle_ccw" => Specification.ExecutionMode.VertexOrderCcw,
                                _ => throw new NotSupportedException($"Unsupported output topology value '{((StringLiteral)anyAttribute.Parameters[0]).Value}'"),
                            }, []));
                        }
                    }
                    else
                    {
                        throw new NotImplementedException($"Can't parse method attribute {anyAttribute} on method {Name}");
                    }
                }
            }
        }

        if (Type is not FunctionType ftype)
            throw new InvalidOperationException();

        table.Push(SymbolFrame!);
        builder.BeginFunction(context, function);

        var functionInfo = new OpFunctionMetadataSDSL(Specification.FunctionFlagsMask.None, 0);

        if (IsOverride)
        {
            // Find parent function
            var parentSymbol = table.ResolveSymbol(function.Name);
            // If multiple symbol with same name, find the proper overload (it should have the exact same signature)
            if (parentSymbol.Type is FunctionGroupType)
                parentSymbol = parentSymbol.GroupMembers.Last(x => x.IdRef != function.Id && (FunctionType)x.Type == function.FunctionType);

            parentSymbol = ShaderDefinition.ImportSymbol(table, context, parentSymbol);

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

        for (var index = 0; index < Parameters.Count; index++)
        {
            var p = Parameters[index];
            var parameterSymbol = ParameterSymbols[index];
            var parameterType = parameterSymbol.Type;
            var paramValue = builder.EmitFunctionParameter(context, p.Name, parameterType);
            parameterSymbol.IdRef = paramValue.Id;
        }

        if (Body is BlockStatement body && !hasUnresolvableGenerics)
        {
            builder.CreateBlock(context);
            Body.Compile(table, compiler);
        }
        else
        {
            builder.Insert(new OpUnreachable());
        }

        // After compiling the body, check if this stage function referenced non-stage members
        if (builder.CurrentFunction is { ReferencesNonStageMembers: true })
            functionInfo.Flags |= Specification.FunctionFlagsMask.ReferencesNonStage;

        builder.EndFunction();
        table.Pop();
    }

    public override string ToString()
    {
        return $"{ReturnTypeName} {Name}()\n{Body}\n";
    }
}

public record struct ShaderParameter(TypeName TypeName, Identifier Name);


public abstract class ParameterListNode(TextLocation info) : Node(info);

public partial class ShaderParameterDeclarations(TextLocation info) : ParameterListNode(info)
{
    public List<ShaderParameter> Parameters { get; set; } = [];
}

public partial class ShaderExpressionList(TextLocation info) : ParameterListNode(info)
{
    public List<Expression> Values { get; set; } = [];

    public List<Expression>.Enumerator GetEnumerator() => Values.GetEnumerator();

    public override string ToString()
    {
        return string.Join(", ", Values);
    }
}
