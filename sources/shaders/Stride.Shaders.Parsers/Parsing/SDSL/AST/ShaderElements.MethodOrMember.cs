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

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class MethodOrMember(TextLocation info, bool isStaged = false) : ShaderElement(info)
{
    public bool IsStaged { get; set; } = isStaged;
    public List<ShaderAttribute>? Attributes { get; set; } = null;

    /// <summary>
    /// An HLSL <c>register(...)</c> or <c>packoffset(...)</c> written after the name. Stride lays out cbuffers and binds resources itself,
    /// so it is dropped with a warning.
    /// </summary>
    public TextLocation? IgnoredRegister { get; set; }

    internal void WarnIgnoredRegister(SymbolTable table, Identifier name)
    {
        if (IgnoredRegister is { } register)
            table.AddWarning(new(register, string.Format(SDSLErrorMessages.SDSL0220, register.Text, name)));
    }
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
        WarnIgnoredRegister(table, Name);
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

    /// <summary>
    /// The initializer that is compiled. A composition gets its shader from the effect, so its initializer is kept in the tree
    /// (tools can read it) but never compiled (SDSL0236), as in Stride 4.3.
    /// </summary>
    private Expression? CompiledValue => IsCompose ? null : Value;

    public override void ProcessSymbol(SymbolTable table, SpirvContext context)
    {
        base.ProcessSymbol(table, context);
        WarnIgnoredRegister(table, Name);
        if (IsCompose && Value != null)
            table.AddWarning(new(Value.Info, string.Format(SDSLErrorMessages.SDSL0236, Name, Value.Info.Text)));
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

        if (Value != null && StreamKind is StreamKind.Stream or StreamKind.PatchStream)
            table.AddError(new(Value.Info, string.Format(SDSLErrorMessages.SDSL0172, Name, Value.Info.Text)));

        // A cbuffer member (any non-static member) or a structured buffer element
        if (storageClass is Specification.StorageClass.Uniform or Specification.StorageClass.StorageBuffer && FindIntegerMatrix(memberType) is { } integerMatrix)
        {
            table.AddError(new(TypeName.Info, string.Format(SDSLErrorMessages.SDSL0195, Name, integerMatrix, new VectorType(integerMatrix.BaseType, integerMatrix.Rows))));
            return;
        }

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

        CompiledValue?.ProcessSymbol(table, memberType);
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
        if (CompiledValue is { } value)
        {
            var valueType = pointerType.BaseType;

            if (pointerType.StorageClass == Specification.StorageClass.Uniform)
            {
                // Uniform variables become cbuffer members — their default values are set from the CPU side.
                // Compile the initializer as a constant value (no function wrapper needed).
                if (value.TryCompileConstantValue(table, context, out var constantValue, valueType))
                    initializerId = constantValue.Id;
                else
                    table.AddError(new(value.Info, string.Format(SDSLErrorMessages.SDSL0173, Name, value.Info.Text, TypeName)));
            }
            else
            {
                // For other storage classes, wrap in an initializer method called from the entry point wrapper.
                // This is necessary in case they can't be created as pure constant.
                // TODO: some of them could become proper const, we could simplify those and use simpler system with constant ID (like StorageClass.Uniform)
                var functionType = new FunctionType(valueType, []);
                initializerId = builder.Insert(new OpFunction(context.GetOrRegister(valueType), context.Bound++, Specification.FunctionControlMask.Const, context.GetOrRegister(functionType))).ResultId;
                builder.Insert(new OpLabel(context.Bound++));

                var initialValue = value.CompileAsValue(table, compiler);
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

        // An explicit matrix layout, for the member the mixer makes of this variable in the Globals cbuffer (SPIR-V and HLSL have
        // opposite meanings: HLSL row_major = SPIR-V ColMajor); removed from the variable before the output
        if (pointerType.BaseType is MatrixType or ArrayType { BaseType: MatrixType } && TypeModifier is TypeModifier.RowMajor or TypeModifier.ColumnMajor)
            context.Add(new OpDecorate(variable, TypeModifier == TypeModifier.RowMajor ? Specification.Decoration.ColMajor : Specification.Decoration.RowMajor, []));

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
    /// <summary>
    /// HLSL return semantic (<c>float4 F() : COLOR</c>): only an entry point's matters, and Stride's come from streams, so it is never emitted.
    /// </summary>
    public Identifier? ReturnSemantic { get; set; }

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
            if (argSym is AppendStructuredBufferType or ConsumeStructuredBufferType)
                table.AddError(new(p.Info, string.Format(SDSLErrorMessages.SDSL0180, p.Name, Name, argSym is AppendStructuredBufferType ? "AppendStructuredBuffer" : "ConsumeStructuredBuffer")));
            else if (argSym is StructuredBufferType or ByteAddressBufferType && (p.Modifiers & ParameterModifiers.Out) != 0)
                table.AddError(new(p.Info, string.Format(SDSLErrorMessages.SDSL0181, p.Name, Name, p.Modifiers == ParameterModifiers.InOut ? "inout" : "out")));
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

        if (!IsOverride)
        {
            if (CheckMissingOverride(table, ftype))
            {
                IsOverride = true;
                functionFlags |= Specification.FunctionFlagsMask.Override;
            }
        }
        else
            CheckOverriddenMethod(table, ftype);

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

    // Without 'override', a method with the signature of an inherited one starts a separate method group,
    // so calls through the base never reach it. The mixer only logs the error message, so it carries the location.
    // An implementation of an abstract method can only mean an override: it is compiled as one, with a warning
    // (returns true). Hiding a method that has a body stays an error.
    private bool CheckMissingOverride(SymbolTable table, FunctionType ftype)
    {
        foreach (var inheritedShader in table.InheritedShaders)
        {
            var inherited = inheritedShader.Symbol!;
            foreach (var (method, flags) in inherited.Methods)
            {
                if (method.Id.Name != Name.Name || method.Type is not FunctionType methodType || methodType != ftype)
                    continue;

                var parameters = string.Join(", ", Parameters.Select(p => p.Type));
                if ((flags & Specification.FunctionFlagsMask.Abstract) != 0)
                {
                    table.AddWarning(new(Name.Info, string.Format(SDSLErrorMessages.SDSL0114, table.CurrentShader!.Name, Name.Name, parameters, Name.Info.Line, inherited.Name)));
                    return true;
                }
                table.AddError(new(Name.Info, string.Format(SDSLErrorMessages.SDSL0115, table.CurrentShader!.Name, Name.Name, parameters, Name.Info.Line, inherited.Name)));
                return false;
            }
        }
        return false;
    }

    // An override needs an inherited method with its parameters and return type (one with its name and other
    // parameters is a mistake; with no inherited method of that name, the method stands alone)
    private void CheckOverriddenMethod(SymbolTable table, FunctionType ftype)
    {
        var sameName = table.InheritedShaders
            .SelectMany(x => x.Symbol!.Methods, (x, method) => (Shader: x.Symbol!, Type: method.Item1.Type as FunctionType, Name: method.Item1.Id.Name))
            .Where(x => x.Name == Name.Name && x.Type != null)
            .ToList();
        if (sameName.Count == 0 || sameName.Exists(x => x.Type == ftype))
            return;

        var parameters = FormatParameters(ftype);
        if (sameName.Find(x => x.Type!.ParameterTypes.SequenceEqual(ftype.ParameterTypes)) is { Type: not null } sameParameters)
            table.AddError(new(Name.Info, string.Format(SDSLErrorMessages.SDSL0175, table.CurrentShader!.Name, Name.Name, parameters, ftype.ReturnType, sameParameters.Shader.Name, sameParameters.Type.ReturnType)));
        else
            table.AddError(new(Name.Info, string.Format(SDSLErrorMessages.SDSL0176, table.CurrentShader!.Name, Name.Name, parameters, string.Join(", ", sameName.Select(x => $"{x.Shader.Name}.{Name.Name}({FormatParameters(x.Type!)})")))));

        static string FormatParameters(FunctionType type) => string.Join(", ", type.ParameterTypes.Select(p => p.Type is PointerType pointer ? pointer.BaseType : p.Type));
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
        // Opaque resources must use UniformConstant storage: see SymbolTypeExtensions.IsOpaqueResource.
        if (p.Type!.IsOpaqueResource())
            return new PointerType(p.Type!, Specification.StorageClass.UniformConstant);
        // Like the buffer members, structured and byte address buffers stay in StorageBuffer storage: the caller's pointer is passed
        // (ShaderMixer.SpecializeBufferParameters removes these parameters, SPIR-V would need variable pointers for them)
        if (p.Type is StructuredBufferType or ByteAddressBufferType)
            return new PointerType(p.Type, Specification.StorageClass.StorageBuffer);

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

        // Note: these values end up as literals of OpExecutionMode
        int EvaluateAttributeParameter(AnyShaderAttribute attribute, int index)
        {
            var parameter = attribute.Parameters[index];
            if (!parameter.TryEvaluateConstantInteger(table, context, out var value))
                table.AddError(new(parameter.Info, $"[{attribute.Name}] parameter must be a constant integer expression"));
            return value;
        }

        if (Attributes != null)
        {
            Span<int> attrParamBuffer = stackalloc int[8]; // max attribute parameters
            foreach (var attribute in Attributes)
            {
                if (attribute is AnyShaderAttribute anyAttribute)
                {
                    if (anyAttribute.Name == "numthreads")
                    {
                        if (EntryPoint != EntryPoint.ComputeShader)
                        {
                            table.AddWarning(new(anyAttribute.Info,
                                $"[numthreads] on '{Name}' is ignored: it applies only to the compute shader entry point 'CSMain'. Set the thread group size via ThreadNumberX/Y/Z (e.g. ComputeEffectShader.ThreadNumbers), which drives both [numthreads] and dispatch."));
                        }
                        else
                        {
                            var parameters = attrParamBuffer[..anyAttribute.Parameters.Count];
                            for (var index = 0; index < anyAttribute.Parameters.Count; index++)
                                parameters[index] = EvaluateAttributeParameter(anyAttribute, index);

                            context.Add(new OpExecutionMode(function.Id, Specification.ExecutionMode.LocalSize, new(parameters)));
                        }
                    }
                    else if (anyAttribute.Name == "maxvertexcount")
                    {
                        context.Add(new OpExecutionMode(function.Id, Specification.ExecutionMode.OutputVertices, new(EvaluateAttributeParameter(anyAttribute, 0))));
                    }
                    else if (anyAttribute.Name == "outputcontrolpoints")
                    {
                        context.Add(new OpExecutionMode(function.Id, Specification.ExecutionMode.OutputVertices, new(EvaluateAttributeParameter(anyAttribute, 0))));
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
                    else if (anyAttribute.Name.Name.Equals("StreamOutput", StringComparison.OrdinalIgnoreCase))
                    {
                        table.AddError(new(anyAttribute.Info, string.Format(SDSLErrorMessages.SDSL0170, Name)));
                    }
                    else
                    {
                        table.AddError(new(anyAttribute.Info, string.Format(SDSLErrorMessages.SDSL0171, anyAttribute.Name, Name)));
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

        if (ftype.ReturnType != ScalarType.Void && !SpirvBuilder.IsBlockTermination(builder.GetLastInstructionType()))
        {
            table.AddError(new(Name.Info, string.Format(SDSLErrorMessages.SDSL0177, table.CurrentShader!.Name, Name.Name)));
            builder.Insert(new OpUnreachable());
        }

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
