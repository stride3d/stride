using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
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
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;



public interface IShaderImporter
{
    SymbolType Import(ShaderClassInstantiation classSource, SpirvContext declaringContext);

    /// <summary>
    /// Resolves an imported struct type by name from a shader.
    /// Returns the full <see cref="StructuredType"/> with members, or null if not available.
    /// </summary>
    StructuredType? ResolveStructType(ShaderSymbol shader, string structName);
}

public class EmptyShaderImporter : IShaderImporter
{
    public SymbolType Import(ShaderClassInstantiation classSource, SpirvContext declaringContext)
    {
        return new ShaderSymbol(classSource.ClassName, classSource.GenericArguments);
    }

    public virtual StructuredType? ResolveStructType(ShaderSymbol shader, string structName) => null;
}

/// <summary>
/// An <see cref="IShaderImporter"/> that resolves struct types by loading shader buffers
/// from an <see cref="IExternalShaderLoader"/>.
/// </summary>
public class ShaderLoaderImporter(IExternalShaderLoader loader) : EmptyShaderImporter
{
    public override StructuredType? ResolveStructType(ShaderSymbol shader, string structName)
    {
        if (loader.LoadExternalBuffer(shader.Name, [], out var buffer, out _, out _))
        {
            foreach (var (_, symbolType) in buffer.Context.ReverseTypes)
            {
                if (symbolType is StructuredType st && st.ToId() == structName)
                    return st;
            }
        }
        return null;
    }
}

public partial class ShaderClass(Identifier name, TextLocation info) : ShaderDeclaration(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<IdentifierBase> Mixins { get; set; } = [];
    public bool Internal { get; set; }

    // Note: We should make this method incremental (called many times in ShaderMixer)
    //       And possibly do the type deduplicating at the same time? (TypeDuplicateRemover)

    /// <summary>
    /// Registers all OpName instructions in the context's Names dictionary.
    /// Safe to call multiple times (uses TryAdd).
    /// </summary>
    public static void RegisterContextNames(SpirvContext context)
    {
        for (var i = 0; i < context.Count; i++)
        {
            var instruction = context[i];
            if (instruction.Op == Op.OpName)
            {
                OpName nameInstruction = instruction;
                context.Names.TryAdd(nameInstruction.Target, nameInstruction.Name);
            }
        }
    }

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
                context.Types.Add(symbolType, typeId);
            }
            else
            {
                // TryAdd: skip if already registered (idempotent for cached contexts)
                if (context.ReverseTypes.TryAdd(typeId, symbolType))
                    context.Types.TryAdd(symbolType, typeId);
            }
        }

        void RegisterName(int target, string name)
        {
            if (allowReplace)
                context.Names[target] = name;
            else
                context.Names.TryAdd(target, name);
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
                    16 => ScalarType.Half,
                    32 => ScalarType.Float,
                    64 => ScalarType.Double,
                    _ => throw new InvalidOperationException(),
                });
            }
            else if (instruction.Op == Op.OpTypeInt)
            {
                OpTypeInt intInstruction = instruction;
                RegisterType(intInstruction.ResultId, (intInstruction.Width, intInstruction.Signedness == 1) switch
                {
                    (32, true) => ScalarType.Int,
                    (32, false) => ScalarType.UInt,
                    (64, true) => ScalarType.Int64,
                    (64, false) => ScalarType.UInt64,
                    _ => throw new NotSupportedException($"Unsupported integer type: width={intInstruction.Width}, signed={intInstruction.Signedness == 1}"),
                });
            }
            else if (instruction.Op == Op.OpTypeBool)
            {
                OpTypeBool boolInstruction = instruction;
                RegisterType(boolInstruction.ResultId, ScalarType.Boolean);
            }
            else if (instruction.Op == Op.OpTypePointer && (OpTypePointer)instruction is { } pointerInstruction)
            {
                var innerType = context.ReverseTypes[pointerInstruction.Type];
                RegisterType(pointerInstruction.ResultId, new PointerType(innerType, pointerInstruction.StorageClass));
            }
            else if (instruction.Op == Op.OpTypeVoid && (OpTypeVoid)instruction is { } voidInstruction)
            {
                RegisterType(voidInstruction.ResultId, ScalarType.Void);
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
                var fieldsData = typeStructInstruction.MemberTypes;
                var fields = new List<StructuredTypeMember>();
                for (var index = 0; index < fieldsData.WordCount; index++)
                {
                    var fieldData = fieldsData.Words[index];
                    var type = context.ReverseTypes[fieldData];
                    if (!memberNames.TryGetValue((typeStructInstruction.ResultId, index), out var name))
                        name = $"_member{index}";
                    fields.Add(new(name, type, TypeModifier.None));
                }
                // TODO: Ideally we shouldn't depend on struct OpName, so we should use UserTypeGOOGLE?
                StructuredType structType = (blocks.Contains(typeStructInstruction.ResultId))
                    ? structName switch
                    {
                        "type.ByteAddressBuffer" => new ByteAddressBufferType(false),
                        "type.RWByteAddressBuffer" => new ByteAddressBufferType(true),
                        var s when s.StartsWith("type.StructuredBuffer.") => new StructuredBufferType(fields[0].Type is ArrayType a ? a.BaseType : fields[0].Type),
                        var s when s.StartsWith("type.RWStructuredBuffer.") => new StructuredBufferType(fields[0].Type is ArrayType a2 ? a2.BaseType : fields[0].Type, true),
                        var s when s.StartsWith("type.AppendStructuredBuffer.") => new AppendStructuredBufferType(fields[0].Type is ArrayType a3 ? a3.BaseType : fields[0].Type),
                        var s when s.StartsWith("type.ConsumeStructuredBuffer.") => new ConsumeStructuredBufferType(fields[0].Type is ArrayType a4 ? a4.BaseType : fields[0].Type),
                        var s when s.StartsWith("type.") => new ConstantBufferSymbol(structName.Substring("type.".Length), fields),
                        _ => throw new InvalidOperationException(),
                    }
                    : new StructType(structName, fields);
                RegisterType(typeStructInstruction.ResultId, structType);
            }
            else if (instruction.Op == Op.OpTypeArray && (OpTypeArray)instruction is { } typeArray)
            {
                var innerType = context.ReverseTypes[typeArray.ElementType];
                var sizeExpr = ConstantExpression.ParseFromBuffer(typeArray.Length, context.GetBuffer(), context);
                if (sizeExpr.TryEvaluate(out var arraySizeObj) && arraySizeObj is IConvertible)
                    RegisterType(typeArray.ResultId, new ArrayType(innerType, Convert.ToInt32(arraySizeObj)));
                else
                    RegisterType(typeArray.ResultId, new ArrayType(innerType, -1, sizeExpr));
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
                foreach (var operand in typeFunctionInstruction.ParameterTypes)
                {
                    parameterTypes.Add(new(context.ReverseTypes[operand.Item1], (ParameterModifiers)operand.Item2));
                }
                RegisterType(typeFunctionInstruction.ResultId, new FunctionType(returnType, parameterTypes));
            }
            else if (instruction.Op == Op.OpTypeImage && new OpTypeImage(instruction) is { } typeImage)
            {
                // SampledType now stores the full return type (e.g. float4) during compilation.
                var returnType = context.ReverseTypes[typeImage.SampledType];
                if (typeImage.Dim == Dim.Buffer)
                {
                    RegisterType(typeImage.ResultId, new BufferType(returnType is ScalarType s ? s : ScalarType.Float, typeImage.Sampled == 2));
                }
                else
                {
                    TextureType textureType = typeImage.Dim switch
                    {
                        Dim.Dim1D => new Texture1DType(returnType),
                        Dim.Dim2D => new Texture2DType(returnType),
                        Dim.Dim3D => new Texture3DType(returnType),
                        Dim.Cube => new TextureCubeType(returnType),
                        _ => throw new NotImplementedException(),
                    };
                    textureType = textureType with
                    {
                        Depth = typeImage.Depth,
                        Arrayed = typeImage.Arrayed == 1 ? true : false,
                        Multisampled = typeImage.MS == 1 ? true : false,
                        Format = typeImage.ImageFormat,
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
            else if (instruction.Op == Op.OpTypeStreamsSDSL && (OpTypeStreamsSDSL)instruction is { } typeStreams)
            {
                RegisterType(typeStreams.ResultId, new StreamsType(typeStreams.Kind));
            }
            else if (instruction.Op == Op.OpTypeGeometryStreamOutputSDSL && (OpTypeGeometryStreamOutputSDSL)instruction is { } typeGeometryStreamOutput)
            {
                RegisterType(typeGeometryStreamOutput.ResultId, new GeometryStreamType(context.ReverseTypes[typeGeometryStreamOutput.BaseType], typeGeometryStreamOutput.Kind));
            }
            else if (instruction.Op == Op.OpTypePatchSDSL && (OpTypePatchSDSL)instruction is { } typePatch)
            {
                RegisterType(typePatch.ResultId, new PatchType(context.ReverseTypes[typePatch.BaseType], typePatch.Kind, typePatch.Size));
            }
            // Import placeholders — registered here with EmptyShaderImporter during generic instantiation.
            // When called with allowReplace=true from CreateShaderType, these get upgraded to real ShaderDefinition.
            else if (instruction.Op == Op.OpImportShaderSDSL && (OpImportShaderSDSL)instruction is { } importShader)
            {
                var classSource = SpirvBuilder.ConvertToShaderClassSource(context, importShader);
                var shaderSymbol = realShaderImporter.Import(classSource, context);
                RegisterType(importShader.ResultId, shaderSymbol);
            }
            else if (instruction.Op == Op.OpImportStructSDSL && (OpImportStructSDSL)instruction is { } importStruct)
            {
                // Resolve the imported struct to the real type (with members) from the owning shader.
                // Without this, an empty placeholder StructType("Name", []) is created, which
                // wouldn't match the full struct when used in function type parameters.
                StructuredType? resolved = null;
                if (context.ReverseTypes.TryGetValue(importStruct.Shader, out var shaderType)
                    && shaderType is ShaderSymbol shaderSym)
                {
                    resolved = realShaderImporter.ResolveStructType(shaderSym, importStruct.StructName);
                }
                RegisterType(importStruct.ResultId, resolved ?? new StructType(importStruct.StructName, []));
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

    /// <summary>
    /// Resolves OpImportShaderSDSL/OpImportStructSDSL into SymbolTable.loadedShaders
    /// without mutating the cached shader context.
    /// </summary>
    private static void ResolveImportsIntoTable(SymbolTable table, SpirvContext mainContext, SpirvContext shaderContext)
    {
        for (var i = 0; i < shaderContext.Count; i++)
        {
            var instruction = shaderContext[i];
            if (instruction.Op == Op.OpImportShaderSDSL && (OpImportShaderSDSL)instruction is { } importShader)
            {
                var classSource = SpirvBuilder.ConvertToShaderClassSource(shaderContext, importShader);

                // Check if already loaded by name (not by ID — IDs are context-local and can collide
                // between different cached shader contexts)
                var stringKey = ResolveImportStringKey(importShader.ShaderName, classSource.GenericArguments);
                if (stringKey != null
                    && table.DeclaredShaders.TryGetValue(stringKey, out var existingDef))
                {
                    table.RegisterLoadedShader(importShader.ResultId, existingDef);
                    continue;
                }

                var shaderDef = LoadAndCacheExternalShaderType(table, mainContext, classSource, shaderContext);
                table.RegisterLoadedShader(importShader.ResultId, shaderDef);
            }
        }
    }

    /// <summary>
    /// Resolves a shader import's generic argument IDs to their string values and returns
    /// the fully-qualified shader name (e.g. "SphericalHarmonicsUtils&lt;3&gt;"), or null if
    /// any generic argument cannot be resolved as a constant.
    /// </summary>
    private static string? ResolveImportStringKey(string shaderName, ConstantExpression[] genericArgs)
    {
        if (genericArgs.Length == 0)
            return shaderName;
        var args = new string[genericArgs.Length];
        for (int j = 0; j < genericArgs.Length; j++)
        {
            if (!genericArgs[j].TryEvaluate(out var value) || value is null)
                return null;
            args[j] = ShaderClassSource.ConvertGenericArgToString(value);
        }
        return $"{shaderName}<{string.Join(",", args)}>";
    }

    class ReplaceTypes(Dictionary<SymbolType, SymbolType> TypesToReplace) : TypeRewriter
    {
        public override SymbolType DefaultVisit(SymbolType node) => TypesToReplace.TryGetValue(node, out var result) ? result : node;
    }

    private static ShaderDefinition CreateShaderType(SymbolTable table, SpirvContext context, ShaderBuffers shaderBuffers, ShaderClassInstantiation classSource)
    {
        // Resolve imports from the cached shader context into the symbol table without mutating the frozen context.
        ResolveImportsIntoTable(table, context, shaderBuffers.Context);

        var variables = new List<(Symbol Symbol, VariableFlagsMask Flags)>();
        var methods = new List<(Symbol Symbol, FunctionFlagsMask Flags)>();
        var methodsDefaultParameters = new Dictionary<int, MethodSymbolDefaultParameters>();
        var structTypes = new List<(StructuredType Type, int ImportedId)>();

        // Build full inheritance list
        List<ShaderClassInstantiation> inheritanceList = new();
        SpirvBuilder.BuildInheritanceListWithoutSelf(table.ShaderLoader, context, classSource, table.CurrentMacros.AsSpan(), shaderBuffers.Context, inheritanceList, ResolveStep.Compile);

        // Load all the inherited shaders
        List<ShaderDefinition> inheritedShaderSymbols = new();
        foreach (var inheritedClass in inheritanceList)
            inheritedShaderSymbols.Add(LoadAndCacheExternalShaderDefinition(table, context, inheritedClass));

        var shaderType = new ShaderDefinition(classSource.ClassName, classSource.GenericArguments)
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
            else if (i.Op == Op.OpDecorate)
            {
                // OpDecorate binary layout: [header][target][decoration][params...]
                // Read raw memory to avoid InitializeProperties overwrite bug when params.count > 1
                var span = i.Data.Memory.Span;
                if (span.Length >= 3 && span[2] == (int)Decoration.FunctionParameterDefaultValueSDSL)
                {
                    var target = span[1];
                    var defaultIds = span[3..];
                    var defaultExprs = new ConstantExpression[defaultIds.Length];
                    for (int j = 0; j < defaultIds.Length; j++)
                        defaultExprs[j] = ConstantExpression.ParseFromBuffer(defaultIds[j], shaderBuffers.Context.GetBuffer(), shaderBuffers.Context);
                    methodsDefaultParameters.Add(target, new(defaultExprs));
                }
            }
            if (i.Op == Op.OpDecorateString && (OpDecorateString)i is
            {
                Decoration: Decoration.ShaderConstantSDSL,
                Target: var target2,
                Value: var constName,
            })
            {
                if (!shaderBuffers.Context.GetBuffer().TryGetInstructionById(target2, out var typeInstruction))
                    throw new InvalidOperationException();
                var resultType = typeInstruction.Data.IdResultType!.Value;
                var constExpr = ConstantExpression.ParseFromBuffer(target2, shaderBuffers.Context.GetBuffer(), shaderBuffers.Context);
                var symbol = new Symbol(new(constName, SymbolKind.Constant), shaderBuffers.Context.ReverseTypes[resultType], 0, ExternalConstant: new(constExpr), OwnerType: shaderType);
                variables.Add((symbol, VariableFlagsMask.None));
            }
        }

        for (var index = 0; index < shaderBuffers.Buffer.Count; index++)
        {
            var instruction = shaderBuffers.Buffer[index];
            if (instruction.Op == Op.OpVariableSDSL && (OpVariableSDSL)instruction is { } variable &&
                variable.StorageClass != Specification.StorageClass.Function)
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
                if (shaderBuffers.Buffer[index + 1].Op == Op.OpFunctionMetadataSDSL && (OpFunctionMetadataSDSL)shaderBuffers.Buffer[index + 1] is { } functionInfo)
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

    private static void RegisterShaderType(SymbolTable table, ShaderDefinition shaderType)
    {
        table.DeclaredShaders.Add(shaderType.ToClassName(), shaderType);
    }

    public void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        builder.Insert(new OpShaderSDSL(Name));

        var openGenerics = new ConstantExpression[Generics != null ? Generics.Parameters.Count : 0];
        var currentShader = new ShaderDefinition(Name, openGenerics);
        table.Push();
        table.CurrentShader = currentShader;

        var hasUnresolvableGenerics = false;
        // Map MemberName parameter names to their GenericParamExpr so inheritance args can reference them
        var memberNameParams = new Dictionary<string, GenericParamExpr>();
        if (Generics != null)
        {
            for (int i = 0; i < Generics.Parameters.Count; i++)
            {
                var genericParameter = Generics.Parameters[i];
                ProcessGenericSymbol(table, context, i, genericParameter);
                openGenerics[i] = new GenericParamExpr(i, Name);
                var genericParameterType = genericParameter.TypeName.Type;

                if (genericParameterType is GenericParameterType { Kind: GenericParameterKindSDSL.MemberName })
                {
                    hasUnresolvableGenerics = true;
                    memberNameParams[genericParameter.Name] = new GenericParamExpr(i, Name);
                }
            }
        }

        table.InheritedShaders.Clear();
        foreach (var mixin in Mixins)
        {
            var mixinGenerics = (mixin as GenericIdentifier)?.Generics;
            var generics = new ConstantExpression[mixinGenerics != null ? mixinGenerics.Values.Count : 0];
            if (mixinGenerics != null)
            {
                for (int i = 0; i < mixinGenerics.Values.Count; i++)
                {
                    // Special case: if it's an identifier and can't be resolved, we'll consider it's a string instead
                    if (mixinGenerics.Values[i] is Identifier identifier)
                    {
                        if (table.TryResolveSymbol(identifier.Name, out var symbol))
                        {
                            mixinGenerics.Values[i].ProcessSymbol(table);
                            var constantId = mixinGenerics.Values[i].CompileConstantValue(table, context).Id;
                            generics[i] = ConstantExpression.ParseFromBuffer(constantId, context.GetBuffer(), context);
                        }
                        else if (memberNameParams.TryGetValue(identifier.Name, out var memberNameRef))
                        {
                            // MemberName generic params aren't in the symbol table but should be
                            // referenced as GenericParamExpr so they participate in generic resolution
                            generics[i] = memberNameRef;
                        }
                        else
                        {
                            generics[i] = new StringConstExpr(identifier.Name);
                        }
                    }
                    else if (mixinGenerics.Values[i] is AccessorChainExpression accessChain)
                    {
                        generics[i] = new StringConstExpr(accessChain.ToString());
                    }
                    else
                    {
                        mixinGenerics.Values[i].ProcessSymbol(table);
                        var constantId = mixinGenerics.Values[i].CompileConstantValue(table, context).Id;
                        generics[i] = ConstantExpression.ParseFromBuffer(constantId, context.GetBuffer(), context);
                    }
                }
            }
            var shaderClassSource = new ShaderClassInstantiation(mixin.Name, generics);
            SpirvBuilder.BuildInheritanceListIncludingSelf(table.ShaderLoader, context, shaderClassSource, table.CurrentMacros.AsSpan(), table.InheritedShaders, ResolveStep.Compile);
        }

        RegisterShaderType(table, currentShader);

        table.CurrentShader = currentShader;

        var shaderSymbols = new List<ShaderDefinition>();
        foreach (var mixin in table.InheritedShaders)
        {
            shaderSymbols.Add(mixin.Symbol = LoadAndCacheExternalShaderDefinition(table, context, mixin));
        }

        foreach (var shaderType in shaderSymbols)
        {
            Inherit(table, context, shaderType, true);
        }

        // Process symbols and generate types
        foreach (var td in Elements.OfType<TypeDef>())
            table.AddError(new(td.Info, $"typedef is not implemented: '{td}'"));
        foreach (var member in Elements.OfType<ShaderStruct>())
            member.ProcessSymbol(table, context);
        foreach (var member in Elements.OfType<ShaderMember>())
            member.ProcessSymbol(table, context);
        foreach (var member in Elements.OfType<ShaderBuffer>())
            member.ProcessSymbol(table, context);
        foreach (var member in Elements.OfType<ShaderSamplerState>())
            member.ProcessSymbol(table, context);
        // In case a method is calling another method not yet processed, we first declare all methods, then analysis of method body
        // (SPIR-V allow forward calling)
        foreach (var method in Elements.OfType<ShaderMethod>())
            method.ProcessSymbol(table, context);

        // If any errors occurred during symbol processing, skip method body analysis and compilation
        if (table.Errors.Count > 0)
            return;

        if (!hasUnresolvableGenerics)
        {
            foreach (var member in Elements.OfType<ShaderMethod>())
                member.ProcessSymbolBody(table, context);
        }

        RenameCBufferVariables();

        foreach (var member in Elements.OfType<ShaderStruct>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderMember>())
        {
            if (member.TypeModifier == TypeModifier.Const)
                continue;
            if (compiler.SourceFileId is int fid1)
                builder.EmitLine(fid1, member.Info.Line, member.Info.Column);
            member.Compile(table, this, compiler);
        }
        foreach (var member in Elements.OfType<ShaderBuffer>())
        {
            if (compiler.SourceFileId is int fid2)
                builder.EmitLine(fid2, member.Info.Line, member.Info.Column);
            member.Compile(table, this, compiler);
        }
        foreach (var member in Elements.OfType<ShaderSamplerState>())
        {
            if (compiler.SourceFileId is int fid3)
                builder.EmitLine(fid3, member.Info.Line, member.Info.Column);
            member.Compile(table, this, compiler);
        }
        foreach (var method in Elements.OfType<ShaderMethod>())
        {
            if (compiler.SourceFileId is int fid4)
                builder.EmitLine(fid4, method.Info.Line, method.Info.Column);
            method.Compile(table, this, compiler, hasUnresolvableGenerics);
        }

        if (hasUnresolvableGenerics)
        {
            var code = Info.Text.ToString();
            // We also store end of name so that we can later easily use macro system to rename generics without changing the generics header
            var nameInfo = Generics?.Parameters.LastOrDefault().Name.Info ?? Name.Info;
            var endOfNameIndex = nameInfo.Range.End.Value - Info.Range.Start.Value;
            builder.Insert(new OpUnresolvableShaderSDSL(Info.Text.ToString(), endOfNameIndex));
        }

        table.InheritedShaders.Clear();
        table.CurrentShader = null;
        table.Pop();
    }

    public int ProcessGenericSymbol(SymbolTable table, SpirvContext context, int index, ShaderParameter genericParameter)
    {
        genericParameter.TypeName.ProcessSymbol(table);
        var genericParameterType = genericParameter.TypeName.Type!;

        // Wrap resource types in pointer (same as member variables)
        if (genericParameterType is TextureType or BufferType)
            genericParameterType = new PointerType(genericParameterType, Specification.StorageClass.UniformConstant);
        else if (genericParameterType is StructuredBufferType or ByteAddressBufferType)
            genericParameterType = new PointerType(genericParameterType, Specification.StorageClass.StorageBuffer);

        table.DeclaredTypes.TryAdd(genericParameterType.ToString(), genericParameterType);

        var genericParameterTypeId = context.GetOrRegister(genericParameterType);
        context.Add(new OpGenericParameterSDSL(genericParameterTypeId, context.Bound, index, Name.Name));
        context.AddName(context.Bound, genericParameter.Name);

        // Note: we skip MemberName because they should have been replaced with the preprocessor during SpirvBuilder.InstantiateMemberNames() step
        if (genericParameterType is not GenericParameterType { Kind: GenericParameterKindSDSL.MemberName or GenericParameterKindSDSL.MemberNameResolved })
            table.CurrentFrame.Add(genericParameter.Name, new(new(genericParameter.Name, SymbolKind.ConstantGeneric), genericParameterType, context.Bound, OwnerType: table.CurrentShader));

        return context.Bound++;
    }

    // If multiple cbuffer with same name, they will be merged
    // Still, we rename them internally to avoid name clashes (in HLSL name is skipped so it's OK, but for example OpImportStructSDSL/OpImportVariableSDSL would be ambiguous)
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


    public static void Inherit(SymbolTable table, SpirvContext context, ShaderDefinition shaderType, bool addToRoot)
    {
        var shaderId = context.GetOrImportShader(shaderType);
        table.RegisterLoadedShader(shaderId, shaderType);

        foreach (var structType in shaderType.StructTypes)
        {
            // Struct types are real SymbolTypes — register them for type resolution
            table.DeclaredTypes.TryAdd(structType.Type.Name, structType.Type);
        }


        if (addToRoot)
            table.CurrentShader!.InheritedShaders.Add(shaderType);

        // Mark inherit
        context.Add(new OpMixinInheritSDSL(shaderId, Spirv.Specification.MixinInheritFlagsMask.None));
    }

    public static ShaderDefinition LoadAndCacheExternalShaderDefinition(SymbolTable table, SpirvContext context, ShaderClassInstantiation classSource)
    {
        // Already processed?
        if (table.DeclaredShaders.TryGetValue(classSource.ToClassNameWithGenerics(), out var cachedShader))
            return cachedShader;

        var shaderType = LoadExternalShaderType(table, context, classSource);
        return shaderType;
    }

    public static ShaderDefinition LoadAndCacheExternalShaderType(SymbolTable table, SpirvContext context, ShaderClassInstantiation classSource, SpirvContext declaringContext)
    {
        // Already processed?
        if (table.DeclaredShaders.TryGetValue(classSource.ToClassNameWithGenerics(), out var cachedShader))
            return cachedShader;

        if (classSource.Buffer == null)
        {
            var shader = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, table.CurrentMacros.AsSpan(), ResolveStep.Compile, declaringContext);
            classSource.Buffer = shader;
        }
        var shaderType = LoadExternalShaderType(table, context, classSource);
        return shaderType;
    }

    public static ShaderDefinition LoadExternalShaderType(SymbolTable table, SpirvContext context, ShaderClassInstantiation classSource)
    {
        var shaderBuffers = classSource.Buffer ?? throw new InvalidOperationException($"Shader buffers not loaded for {classSource.ClassName}");

        var shaderType = CreateShaderType(table, context, shaderBuffers, classSource);

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


public partial class ShaderGenerics(Identifier typename, Identifier name, TextLocation info) : Node(info)
{
    public Identifier Name { get; set; } = name;
    public Identifier TypeName { get; set; } = typename;
}
