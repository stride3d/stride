using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;



public interface IShaderImporter
{
    ShaderSymbol Import(ShaderClassInstantiation classSource, SpirvContext declaringContext);
}

public class EmptyShaderImporter : IShaderImporter
{
    public ShaderSymbol Import(ShaderClassInstantiation classSource, SpirvContext declaringContext)
    {
        return new ShaderSymbol(classSource.ClassName, classSource.GenericArguments);
    }
}

public class ShaderClass(Identifier name, TextLocation info) : ShaderDeclaration(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<Mixin> Mixins { get; set; } = [];

    // Note: We should make this method incremental (called many times in ShaderMixer)
    //       And possibly do the type deduplicating at the same time? (TypeDuplicateRemover)

    public static void ProcessNameAndTypes(SpirvContext context, IShaderImporter? shaderImporter = null, bool allowReplace = false)
    {
        ProcessNameAndTypes(context, 0, context.Count, shaderImporter, allowReplace);
    }

    public static void ProcessNameAndTypes(SpirvContext context, int start, int end, IShaderImporter? shaderImporter = null, bool allowReplace = false)
    {
        void RegisterType(int typeId, SymbolType symbolType)
        {
            if (allowReplace && context.ReverseTypes.TryGetValue(typeId, out var existingSymbolType))
            {
                context.ReverseTypes[typeId] = symbolType;
                context.Types.Remove(existingSymbolType);
            }
            else
            {
                context.ReverseTypes.Add(typeId, symbolType);
            }
            context.Types.Add(symbolType, typeId);
        }

        void RegisterName(int target, string name)
        {
            if (allowReplace)
                context.Names[target] = name;
            else
                context.Names.Add(target, name);
        }

        var realShaderImporter = shaderImporter ?? new EmptyShaderImporter();
        var importedShaders = new Dictionary<int, ShaderSymbol>();

        var memberNames = new Dictionary<(int, int), string>();
        var blocks = new HashSet<int>();
        for (var i = start; i < end; i++)
        {
            var instruction = context[i];
            if (instruction.Op == Op.OpName)
            {
                OpName nameInstruction = instruction;
                RegisterName(nameInstruction.Target, nameInstruction.Name);
            }
            else if (instruction.Op == Op.OpMemberName)
            {
                OpMemberName nameInstruction = instruction;
                memberNames.Add((nameInstruction.Type, nameInstruction.Member), nameInstruction.Name);
            }
            else if (instruction.Op == Op.OpDecorate)
            {
                var decorateInstruction = new OpDecorate(instruction);
                if (decorateInstruction.Decoration == Decoration.Block)
                    blocks.Add(decorateInstruction.Target);
            }
            else if (instruction.Op == Op.OpTypeFloat)
            {
                OpTypeFloat floatInstruction = instruction;
                //if (floatInstruction.FloatingPointEncoding != 0)
                //    throw new InvalidOperationException();

                RegisterType(floatInstruction.ResultId, floatInstruction.Width switch
                {
                    16 => ScalarType.From("half"),
                    32 => ScalarType.From("float"),
                    64 => ScalarType.From("double"),
                    _ => throw new InvalidOperationException(),
                });
            }
            else if (instruction.Op == Op.OpTypeInt)
            {
                OpTypeInt intInstruction = instruction;
                RegisterType(intInstruction.ResultId, (intInstruction.Width, intInstruction.Signedness == 1) switch
                {
                    (32, true) => ScalarType.From("int"),
                    (32, false) => ScalarType.From("uint"),
                    (64, true) => ScalarType.From("long"),
                    (64, false) => ScalarType.From("ulong"),
                });
            }
            else if (instruction.Op == Op.OpTypeBool)
            {
                OpTypeBool boolInstruction = instruction;
                RegisterType(boolInstruction.ResultId, ScalarType.From("bool"));
            }
            else if (instruction.Op == Op.OpTypePointer && (OpTypePointer)instruction is { } pointerInstruction)
            {
                var innerType = context.ReverseTypes[pointerInstruction.Type];
                RegisterType(pointerInstruction.ResultId, new PointerType(innerType, pointerInstruction.Storageclass));
            }
            else if (instruction.Op == Op.OpTypeVoid && (OpTypeVoid)instruction is { } voidInstruction)
            {
                RegisterType(voidInstruction.ResultId, ScalarType.From("void"));
            }
            else if (instruction.Op == Op.OpTypeVector && (OpTypeVector)instruction is { } vectorInstruction)
            {
                var innerType = (ScalarType)context.ReverseTypes[vectorInstruction.ComponentType];
                RegisterType(vectorInstruction.ResultId, new VectorType(innerType, vectorInstruction.ComponentCount));
            }
            else if (instruction.Op == Op.OpTypeMatrix && (OpTypeMatrix)instruction is { } matrixInstruction)
            {
                var innerType = (VectorType)context.ReverseTypes[matrixInstruction.ColumnType];
                RegisterType(matrixInstruction.ResultId, new MatrixType(innerType.BaseType, innerType.Size, matrixInstruction.ColumnCount));
            }
            else if (instruction.Op == Op.OpTypeStruct && (OpTypeStruct)instruction is { } typeStructInstruction)
            {
                var structName = context.Names[typeStructInstruction.ResultId];
                var fieldsData = typeStructInstruction.Values;
                var fields = new List<StructuredTypeMember>();
                for (var index = 0; index < fieldsData.WordCount; index++)
                {
                    var fieldData = fieldsData.Words[index];
                    var type = context.ReverseTypes[fieldData];
                    if (!memberNames.TryGetValue((typeStructInstruction.ResultId, index), out var name))
                        name = $"_member{index}";
                    fields.Add(new(name, type, TypeModifier.None));
                }
                StructuredType structType = (blocks.Contains(typeStructInstruction.ResultId))
                    ? structName switch 
                    {
                        var s when s.StartsWith("type.StructuredBuffer.") => new StructuredBufferType(fields[0].Type),
                        var s when s.StartsWith("type.") => new ConstantBufferSymbol(structName.Substring("type.".Length), fields),
                        _ => throw new InvalidOperationException(),
                    }
                    : new StructType(structName, fields);
                RegisterType(typeStructInstruction.ResultId, structType);
            }
            else if (instruction.Op == Op.OpTypeArray && (OpTypeArray)instruction is { } typeArray)
            {
                var innerType = context.ReverseTypes[typeArray.ElementType];
                if (context.TryGetConstantValue(typeArray.Length, out var arraySizeObject, out _, false))
                {
                    RegisterType(typeArray.ResultId, new ArrayType(innerType, (int)arraySizeObject));
                }
                else
                {
                    // Constant can't be computed; we need to save aside all opcodes
                    var bufferForConstant = context.ExtractConstantAsSpirvBuffer(typeArray.Length);
                    RegisterType(typeArray.ResultId, new ArrayType(innerType, -1, (typeArray.Length, bufferForConstant)));
                }
            }
            else if (instruction.Op == Op.OpTypeRuntimeArray && (OpTypeRuntimeArray)instruction is { } typeRuntimeArray)
            {
                var innerType = context.ReverseTypes[typeRuntimeArray.ElementType];
                RegisterType(typeRuntimeArray.ResultId, new ArrayType(innerType, -1));
            }
            else if (instruction.Op == Op.OpTypeFunctionSDSL && new OpTypeFunctionSDSL(instruction) is { } typeFunctionInstruction)
            {
                var tmp = new OpTypeFunction(instruction);
                var returnType = context.ReverseTypes[typeFunctionInstruction.ReturnType];
                var parameterTypes = new List<FunctionParameter>();
                foreach (var operand in typeFunctionInstruction.Values)
                {
                    parameterTypes.Add(new(context.ReverseTypes[operand.Item1], (ParameterModifiers)operand.Item2));
                }
                RegisterType(typeFunctionInstruction.ResultId, new FunctionType(returnType, parameterTypes));
            }
            else if (instruction.Op == Op.OpTypeImage && new OpTypeImage(instruction) is { } typeImage)
            {
                var sampledType = (ScalarType)context.ReverseTypes[typeImage.SampledType];
                if (typeImage.Dim == Dim.Buffer)
                {
                    RegisterType(typeImage.ResultId, new BufferType(sampledType));
                }
                else
                {
                    TextureType textureType = typeImage.Dim switch
                    {
                        Dim.Dim1D => new Texture1DType(sampledType),
                        Dim.Dim2D => new Texture2DType(sampledType),
                        Dim.Dim3D => new Texture3DType(sampledType),
                        Dim.Cube => new TextureCubeType(sampledType),
                        _ => throw new NotImplementedException(),
                    };
                    textureType = textureType with
                    {
                        Depth = typeImage.Depth,
                        Arrayed = typeImage.Arrayed == 1 ? true : false,
                        Multisampled = typeImage.MS == 1 ? true : false,
                        Format = typeImage.Imageformat,
                        Sampled = typeImage.Sampled,
                    };

                    RegisterType(typeImage.ResultId, textureType);
                }
            }
            else if (instruction.Op == Op.OpTypeSampler && new OpTypeSampler(instruction) is { } typeSampler)
            {
                RegisterType(typeSampler.ResultId, new SamplerType());
            }
            else if (instruction.Op == Op.OpTypeGenericSDSL && (OpTypeGenericSDSL)instruction is { } typeGeneric)
            {
                RegisterType(typeGeneric.ResultId, new GenericParameterType(typeGeneric.Kind));
            }
            else if (instruction.Op == Op.OpTypeStreamsSDSL && (OpTypePointer)instruction is { } typeStreams)
            {
                RegisterType(typeStreams.ResultId, new StreamsType());
            }
            // Unresolved content
            // This only happens during EvaluateInheritanceAndCompositions so it's not important to have all information valid
            else if (instruction.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)instruction is { } importShader)
            {
                var classSource = new ShaderClassInstantiation(importShader.ShaderName, importShader.Values.Elements.Memory.ToArray());
                var shaderSymbol = realShaderImporter.Import(classSource, context);

                RegisterType(importShader.ResultId, shaderSymbol);
            }
            else if (instruction.Op == Op.OpSDSLImportStruct && (OpSDSLImportStruct)instruction is { } importStruct)
            {
                var shaderSymbol = (ShaderSymbol)context.ReverseTypes[importStruct.Shader];
                if (shaderSymbol is LoadedShaderSymbol loadedShaderSymbol)
                {
                    var structName = importStruct.StructName;
                    RegisterType(importStruct.ResultId, loadedShaderSymbol.StructTypes.Single(x => x.Type.ToId() == structName).Type);
                }
                else
                {
                    RegisterType(importStruct.ResultId, new StructType(importStruct.StructName, []));
                }
            }
        }

        // Second pass (for processing when info from first pass is needed)
        for (var i = start; i < end; i++)
        {
            var instruction = context[i];

            // Can be declared before OpTypeStruct, so done in second pass
            if (instruction.Op == Op.OpMemberDecorate && (OpMemberDecorate)instruction is { } memberDecorate)
            {
                var structType = (StructuredType)context.ReverseTypes[memberDecorate.StructureType];
                // Note: SPIR-V and HLSL have opposite meaning for Rows/Columns 
                if (memberDecorate.Decoration == Decoration.ColMajor)
                    structType.Members[memberDecorate.Member] = structType.Members[memberDecorate.Member] with { TypeModifier = TypeModifier.RowMajor };
                else if (memberDecorate.Decoration == Decoration.RowMajor)
                    structType.Members[memberDecorate.Member] = structType.Members[memberDecorate.Member] with { TypeModifier = TypeModifier.ColumnMajor };
            }
        }
    }

    class ReplaceTypes(Dictionary<SymbolType, SymbolType> TypesToReplace) : TypeRewriter
    {
        public override SymbolType DefaultVisit(SymbolType node) => TypesToReplace.TryGetValue(node, out var result) ? result : node;
    }

    public class ShaderImporter(SymbolTable table, SpirvContext context) : IShaderImporter
    {
        public ShaderSymbol Import(ShaderClassInstantiation classSource, SpirvContext declaringContext)
        {
            return LoadAndCacheExternalShaderType(table, context, classSource, declaringContext);
        }
    }

    private static LoadedShaderSymbol CreateShaderType(SymbolTable table, SpirvContext context, ShaderBuffers shaderBuffers, ShaderClassInstantiation classSource)
    {
        // Reprocess types, this is necessary for:
        // - ArrayType (with proper updated constants without generics)
        // - ShaderClass (properly loaded as LoadedShaderSymbol)
        ProcessNameAndTypes(shaderBuffers.Context, new ShaderImporter(table, context), true);

        var variables = new List<(Symbol Symbol, VariableFlagsMask Flags)>();
        var methods = new List<(Symbol Symbol, FunctionFlagsMask Flags)>();
        var methodsDefaultParameters = new Dictionary<int, MethodSymbolDefaultParameters>();
        var structTypes = new List<(StructuredType Type, int ImportedId)>();
        
        // Build full inheritance list
        List<ShaderClassInstantiation> inheritanceList = new();
        SpirvBuilder.BuildInheritanceListWithoutSelf(table.ShaderLoader, context, classSource, table.CurrentMacros.AsSpan(), shaderBuffers.Context, inheritanceList, ResolveStep.Compile);

        // Load all the inherited shaders
        List<LoadedShaderSymbol> inheritedShaderSymbols = new();
        foreach (var inheritedClass in inheritanceList)
            inheritedShaderSymbols.Add(LoadAndCacheExternalShaderType(table, context, inheritedClass));

        var shaderType = new LoadedShaderSymbol(classSource.ClassName, classSource.GenericArguments)
        {
            Variables = variables,
            Methods = methods,
            StructTypes = structTypes,
            InheritedShaders = inheritedShaderSymbols,
        };

        foreach (var i in shaderBuffers.Context)
        {
            if (i.Op == Op.OpTypeStruct && (OpTypeStruct)i is { } typeStructInstruction)
            {
                structTypes.Add(((StructuredType)shaderBuffers.Context.ReverseTypes[typeStructInstruction.ResultId], -1));
            }
            else if (i.Op == Op.OpDecorate && (OpDecorate)i is
                {
                    Decoration: Decoration.FunctionParameterDefaultValueSDSL,
                    Target: var target,
                } decorateFunctionParameters)
            {
                methodsDefaultParameters.Add(target, new(shaderBuffers.Context, decorateFunctionParameters.DecorationParameters.Span.ToArray()));
            }
            else if (i.Op == Op.OpDecorate && (OpDecorate)i is
                 {
                     Decoration: Decoration.ShaderConstantSDSL,
                     Target: var target2,
                 } decorateShaderConstant)
            {
                if (!shaderBuffers.Context.GetBuffer().TryGetInstructionById(target2, out var typeInstruction))
                    throw new InvalidOperationException();
                var resultType = typeInstruction.Data.IdResultType.Value;
                var symbol = new Symbol(new(shaderBuffers.Context.Names[target2], SymbolKind.Constant), shaderBuffers.Context.ReverseTypes[resultType], 0, ExternalConstant: new(shaderBuffers.Context, target2), OwnerType: shaderType);
                variables.Add((symbol, VariableFlagsMask.None));
            }
        }
        
        for (var index = 0; index < shaderBuffers.Buffer.Count; index++)
        {
            var instruction = shaderBuffers.Buffer[index];
            if (instruction.Op == Op.OpVariableSDSL && (OpVariableSDSL)instruction is { } variable &&
                variable.Storageclass != Specification.StorageClass.Function)
            {
                if (!shaderBuffers.Context.Names.TryGetValue(variable.ResultId, out var variableName))
                    variableName = $"_{variable.ResultId}";
                var variableType = shaderBuffers.Context.ReverseTypes[variable.ResultType];

                var sid = new SymbolID(variableName, SymbolKind.Variable, variable.Flags.HasFlag(VariableFlagsMask.Stream) ? Storage.Stream : 0, IsStage: (variable.Flags & VariableFlagsMask.Stage) != 0);
                variables.Add((new(sid, variableType, 0, OwnerType: shaderType), variable.Flags));
            }

            if (instruction.Op == Op.OpFunction)
            {
                var functionFlags = FunctionFlagsMask.None;
                if (shaderBuffers.Buffer[index + 1].Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)shaderBuffers.Buffer[index + 1] is { } functionInfo)
                    functionFlags = functionInfo.Flags;

                OpFunction functionInstruction = instruction;
                var functionName = shaderBuffers.Context.Names[functionInstruction.ResultId];
                var functionType = shaderBuffers.Context.ReverseTypes[functionInstruction.FunctionType];

                var sid = new SymbolID(functionName, SymbolKind.Method, IsStage: (functionFlags & FunctionFlagsMask.Stage) != 0);
                MethodSymbolDefaultParameters? methodDefaultParameters = methodsDefaultParameters.TryGetValue(functionInstruction.ResultId, out var methodDefaultParametersValue)
                    ? methodDefaultParametersValue
                    : null;
                methods.Add((new(sid, functionType, 0, MethodDefaultParameters: methodDefaultParameters, OwnerType: shaderType), functionFlags));
            }

            if (instruction.Op == Op.OpTypeStruct && (OpTypeStruct)instruction is { } typeStructInstruction)
            {
                structTypes.Add(((StructuredType)shaderBuffers.Context.ReverseTypes[typeStructInstruction.ResultId], -1));
            }
        }

        return shaderType;
    }

    private static void RegisterShaderType(SymbolTable table, ShaderSymbol shaderType)
    {
        table.DeclaredTypes.Add(shaderType.ToClassName(), shaderType);
    }

    public void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        builder.Insert(new OpSDSLShader(name));

        table.Push();

        var openGenerics = new int[Generics != null ? Generics.Parameters.Count : 0];
        var hasUnresolvableGenerics = false;
        if (Generics != null)
        {
            for (int i = 0; i < Generics.Parameters.Count; i++)
            {
                var genericParameter = Generics.Parameters[i];
                var genericParameterType = genericParameter.TypeName.ResolveType(table, context);
                table.DeclaredTypes.TryAdd(genericParameterType.ToString(), genericParameterType);

                var genericParameterTypeId = context.GetOrRegister(genericParameterType);
                context.Add(new OpSDSLGenericParameter(genericParameterTypeId, context.Bound, i, Name.Name));
                context.AddName(context.Bound, genericParameter.Name);

                // Note: we skip MemberName because they should have been replaced with the preprocessor during SpirvBuilder.InstantiateMemberNames() step
                if (genericParameterType is not GenericParameterType { Kind: GenericParameterKindSDSL.MemberName or GenericParameterKindSDSL.MemberNameResolved })
                    table.CurrentFrame.Add(genericParameter.Name, new(new(genericParameter.Name, SymbolKind.ConstantGeneric), genericParameterType, context.Bound));

                openGenerics[i] = context.Bound;

                context.Bound++;

                if (genericParameterType is GenericParameterType { Kind: GenericParameterKindSDSL.MemberName })
                    hasUnresolvableGenerics = true;
            }
        }

        var inheritanceList = new List<ShaderClassInstantiation>();
        foreach (var mixin in Mixins)
        {
            var generics = new int[mixin.Generics != null ? mixin.Generics.Values.Count : 0];
            if (mixin.Generics != null)
            {
                for (int i = 0; i < mixin.Generics.Values.Count; i++)
                {
                    // Special case: if it's an identifier and can't be resolved, we'll consider it's a string instead
                    if (mixin.Generics.Values[i] is Identifier identifier)
                    {
                        if (table.TryResolveSymbol(identifier.Name, out var symbol))
                        {
                            generics[i] = mixin.Generics.Values[i].CompileConstantValue(table, context).Id;
                        }
                        else
                        {
                            generics[i] = context.Add(new OpConstantStringSDSL(context.Bound++, identifier.Name)).IdResult.Value;
                        }
                    }
                    else if (mixin.Generics.Values[i] is AccessorChainExpression accessChain)
                    {
                        generics[i] = context.Add(new OpConstantStringSDSL(context.Bound++, accessChain.ToString())).IdResult.Value;
                    }
                    else
                    {
                        generics[i] = mixin.Generics.Values[i].CompileConstantValue(table, context).Id;
                    }
                }
            }
            var shaderClassSource = new ShaderClassInstantiation(mixin.Name, generics);
            SpirvBuilder.BuildInheritanceListIncludingSelf(table.ShaderLoader, context, shaderClassSource, table.CurrentMacros.AsSpan(), inheritanceList, ResolveStep.Compile);
        }

        var currentShader = new LoadedShaderSymbol(Name, openGenerics);
        RegisterShaderType(table, currentShader);

        table.CurrentShader = currentShader;
        table.InheritedShaders = inheritanceList;

        var shaderSymbols = new List<LoadedShaderSymbol>();
        foreach (var mixin in inheritanceList)
        {
            shaderSymbols.Add(mixin.Symbol = LoadAndCacheExternalShaderType(table, context, mixin));
        }

        foreach (var shaderType in shaderSymbols)
        {
            Inherit(table, context, shaderType, true);
        }

        foreach (var member in Elements)
        {
            // Do this early: we want struct to be available for function parameters (same loop)
            member.ProcessSymbol(table, context);

            if (member is ShaderMethod func)
            {
                var ftype = new FunctionType(func.ReturnTypeName.ResolveType(table, context), []);
                foreach (var arg in func.Parameters)
                {
                    var argSym = arg.TypeName.ResolveType(table, context);
                    table.DeclaredTypes.TryAdd(argSym.ToString(), argSym);
                    arg.Type = argSym;
                    ftype.ParameterTypes.Add(new(new PointerType(arg.Type, Specification.StorageClass.Function), arg.Modifiers));
                }
                func.Type = ftype;

                table.DeclaredTypes.TryAdd(func.Type.ToString(), func.Type);
            }
            else if (member is ShaderMember svar)
            {
                if (!svar.TypeName.TryResolveType(table, context, out var memberType))
                {
                    if (svar.TypeName.Name.Contains("<"))
                        throw new NotImplementedException("Can't have member variables with generic shader types");
                    var classSource = new ShaderClassInstantiation(svar.TypeName.Name, []);
                    var shader = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, table.CurrentMacros.AsSpan(), ResolveStep.Compile, context);
                    classSource.Buffer = shader;
                    var shaderType = LoadAndCacheExternalShaderType(table, context, classSource);

                    // Resolve again (we don't use shaderType direclty, because it might lack info such as ArrayType)
                    memberType = svar.TypeName.ResolveType(table, context);
                }

                var storageClass = svar.StorageClass == StorageClass.Static || svar.StreamKind == StreamKind.Stream
                    ? Specification.StorageClass.Private
                    : Specification.StorageClass.Uniform;
                if (memberType is TextureType || memberType is BufferType)
                    storageClass = Specification.StorageClass.UniformConstant;
                if (memberType is StructuredBufferType)
                    storageClass = Specification.StorageClass.StorageBuffer;

                if (svar.TypeModifier == TypeModifier.Const)
                {
                    if (svar.Value == null)
                        throw new InvalidOperationException($"Constant {svar.Name} doesn't have a value");
                    
                    // Constant: compile right away
                    var constantValue = svar.Value.CompileConstantValue(table, context, memberType);
                    context.SetName(constantValue.Id, svar.Name);
                    var symbol = new Symbol(new(svar.Name, SymbolKind.Constant), memberType, constantValue.Id);
                    table.CurrentFrame.Add(svar.Name, symbol);
                    svar.Type = memberType;

                    // This constant is visible when inherited
                    context.Add(new OpDecorate(constantValue.Id, Decoration.ShaderConstantSDSL, []));
                }
                else
                {
                    svar.Type = new PointerType(memberType, storageClass);
                    table.DeclaredTypes.TryAdd(svar.Type.ToString(), svar.Type);
                }
            }
            else if (member is CBuffer cb)
            {
                foreach (var cbMember in cb.Members)
                {
                    cbMember.Type = cbMember.TypeName.ResolveType(table, context);
                    //var symbol = new Symbol(new(cbMember.Name, SymbolKind.CBuffer), cbMember.Type);
                    //symbols.Add(symbol);
                }
            }
            else if (member is ShaderSamplerState samplerState)
            {
                samplerState.Type = new SamplerType();
                table.DeclaredTypes.TryAdd(samplerState.Type.ToString(), samplerState.Type);
            }
        }

        RenameCBufferVariables();

        foreach (var member in Elements.OfType<ShaderStruct>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderBuffer>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderMember>())
        {
            if (member.TypeModifier == TypeModifier.Const)
                continue;
            member.Compile(table, this, compiler);
        }
        foreach (var member in Elements.OfType<ShaderSamplerState>())
            member.Compile(table, this, compiler);

        // In case calling a method not yet processed, we first register method types
        // (SPIR-V allow forward calling)
        foreach (var method in Elements.OfType<ShaderMethod>())
            method.Declare(table, this, compiler);

        foreach (var method in Elements.OfType<ShaderMethod>())
            method.Compile(table, this, compiler, hasUnresolvableGenerics);

        if (hasUnresolvableGenerics)
        {
            var code = Info.Text.ToString();
            // We also store end of name so that we can later easily use macro system to rename generics without changing the generics header
            var nameInfo = Generics?.Parameters.LastOrDefault().Name.Info ?? Name.Info;
            var endOfNameIndex = nameInfo.Range.End.Value - Info.Range.Start.Value;
            builder.Insert(new OpUnresolvableShaderSDSL(Info.Text.ToString(), endOfNameIndex));
        }

        table.InheritedShaders = null;
        table.CurrentShader = null;
        table.Pop();
    }

    // If multiple cbuffer with same name, they will be merged
    // Still, we rename them internally to avoid name clashes (in HLSL name is skipped so it's OK, but for example OpSDSLImportStruct/OpSDSLImportVariable would be ambiguous)
    private void RenameCBufferVariables()
    {
        var cbuffersByNames = Elements.OfType<CBuffer>().GroupBy(x => x.Name);
        foreach (var cbufferGroup in cbuffersByNames)
        {
            if (cbufferGroup.Count() > 1)
            {
                int index = 0;
                foreach (var cbuffer in cbufferGroup)
                {
                    cbuffer.Name = $"{cbuffer.Name}.{index}";
                    index++;
                }
            }
        }
    }

    // If multiple cbuffer with same name Test, they will be renamed Test.0 Test.1 etc.
    public static string GetCBufferRealName(string cbufferName)
    {
        var dotIndex = cbufferName.IndexOf('.');
        if (dotIndex != -1)
            return cbufferName.Substring(0, dotIndex);

        return cbufferName;
    }


    public static void Inherit(SymbolTable table, SpirvContext context, LoadedShaderSymbol shaderType, bool addToRoot)
    {
        var shaderId = context.GetOrRegister(shaderType);

        foreach (var structType in shaderType.StructTypes)
        {
            // Add the struct like if it was part of our shader (but using the imported id)
            table.DeclaredTypes.TryAdd(structType.Type.Name, structType.Type);
        }


        if (addToRoot)
            table.CurrentShader.InheritedShaders.Add(shaderType);

        // Mark inherit
        context.Add(new OpSDSLMixinInherit(shaderId));
    }

    public static LoadedShaderSymbol LoadAndCacheExternalShaderType(SymbolTable table, SpirvContext context, ShaderClassInstantiation classSource)
    {
        // Already processed?
        if (table.DeclaredTypes.TryGetValue(classSource.ToClassNameWithGenerics(), out var symbolType))
            return (LoadedShaderSymbol)symbolType;

        if (classSource.Buffer == null)
            throw new InvalidOperationException($"{nameof(classSource)}.{nameof(classSource.Buffer)} need to be set");

        var shaderType = LoadExternalShaderType(table, context, classSource);
        return shaderType;
    }

    public static LoadedShaderSymbol LoadAndCacheExternalShaderType(SymbolTable table, SpirvContext context, ShaderClassInstantiation classSource, SpirvContext declaringContext)
    {
        // Already processed?
        if (table.DeclaredTypes.TryGetValue(classSource.ToClassNameWithGenerics(), out var symbolType))
            return (LoadedShaderSymbol)symbolType;

        if (classSource.Buffer == null)
        {
            var shader = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, table.CurrentMacros.AsSpan(), ResolveStep.Compile, declaringContext);
            classSource.Buffer = shader;
        }
        var shaderType = LoadExternalShaderType(table, context, classSource);
        return shaderType;
    }

    public static LoadedShaderSymbol LoadExternalShaderType(SymbolTable table, SpirvContext context, ShaderClassInstantiation classSource)
    {
        var shaderBuffer = classSource.Buffer;

        var shaderType = CreateShaderType(table, context, shaderBuffer.Value, classSource);

        RegisterShaderType(table, shaderType);

        return shaderType;
    }


    public override string ToString()
    {
        return
$"""
Class : {Name}
Generics : {string.Join(", ", Generics)}
Inherits from : {string.Join(", ", Mixins)}
Body :
{string.Join("\n", Elements)}
""";
    }
}


public class ShaderGenerics(Identifier typename, Identifier name, TextLocation info) : Node(info)
{
    public Identifier Name { get; set; } = name;
    public Identifier TypeName { get; set; } = typename;
}

public class Mixin(Identifier name, TextLocation info) : Node(info)
{
    public List<Identifier> Path { get; set; } = [];
    public Identifier Name { get; set; } = name;
    public ShaderExpressionList? Generics { get; set; }
    public override string ToString()
        => Generics switch
        {
            null => Name.Name,
            _ => $"{Name}<{Generics}>"
        };
}

public abstract class ShaderMixinValue(TextLocation info) : Node(info);
public class ShaderMixinExpression(Expression expression, TextLocation info) : ShaderMixinValue(info)
{
    public Expression Value { get; set; } = expression;
}
public class ShaderMixinIdentifier(Identifier identifier, TextLocation info) : ShaderMixinValue(info)
{
    public Identifier Value { get; set; } = identifier;
}