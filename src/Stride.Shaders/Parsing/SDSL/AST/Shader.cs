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
    ShaderSymbol Import(ShaderClassInstantiation classSource, NewSpirvBuffer buffer);
}

public class EmptyShaderImporter : IShaderImporter
{
    public ShaderSymbol Import(ShaderClassInstantiation classSource, NewSpirvBuffer buffer)
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

    public static void ProcessNameAndTypes(NewSpirvBuffer buffer, int start, int end, out Dictionary<int, string> names, out Dictionary<int, SymbolType> types, IShaderImporter? shaderImporter = null)
    {
        names = [];
        types = [];

        ProcessNameAndTypes(buffer, start, end, names, types, shaderImporter);
    }

    public static void ProcessNameAndTypes(NewSpirvBuffer buffer, int start, int end, Dictionary<int, string> names, Dictionary<int, SymbolType> types, IShaderImporter? shaderImporter = null)
    {
        var realShaderImporter = shaderImporter ?? new EmptyShaderImporter();
        var importedShaders = new Dictionary<int, ShaderSymbol>();

        var memberNames = new Dictionary<(int, int), string>();
        var blocks = new HashSet<int>();
        for (var i = start; i < end; i++)
        {
            var instruction = buffer[i];
            if (instruction.Op == Op.OpName)
            {
                OpName nameInstruction = instruction;
                names.Add(nameInstruction.Target, nameInstruction.Name);
            }
            else if (instruction.Op == Op.OpMemberName)
            {
                OpMemberName nameInstruction = instruction;
                memberNames.Add((nameInstruction.Type, nameInstruction.Member), nameInstruction.Name);
            }
            else if (instruction.Op == Op.OpDecorate)
            {
                OpDecorate decorateInstruction = instruction;
                if (decorateInstruction.Decoration.Value == Decoration.Block)
                    blocks.Add(decorateInstruction.Target);
            }
            else if (instruction.Op == Op.OpTypeFloat)
            {
                OpTypeFloat floatInstruction = instruction;
                //if (floatInstruction.FloatingPointEncoding != 0)
                //    throw new InvalidOperationException();

                types.Add(floatInstruction.ResultId, floatInstruction.Width switch
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
                types.Add(intInstruction.ResultId, (intInstruction.Width, intInstruction.Signedness == 1) switch
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
                types.Add(boolInstruction.ResultId, ScalarType.From("bool"));
            }
            else if (instruction.Op == Op.OpTypePointer && (OpTypePointer)instruction is { } pointerInstruction)
            {
                var innerType = types[pointerInstruction.Type];
                types.Add(pointerInstruction.ResultId, new PointerType(innerType, pointerInstruction.Storageclass));
            }
            else if (instruction.Op == Op.OpTypeVoid && (OpTypeVoid)instruction is { } voidInstruction)
            {
                types.Add(voidInstruction.ResultId, ScalarType.From("void"));
            }
            else if (instruction.Op == Op.OpTypeVector && (OpTypeVector)instruction is { } vectorInstruction)
            {
                var innerType = (ScalarType)types[vectorInstruction.ComponentType];
                types.Add(vectorInstruction.ResultId, new VectorType(innerType, vectorInstruction.ComponentCount));
            }
            else if (instruction.Op == Op.OpTypeMatrix && (OpTypeMatrix)instruction is { } matrixInstruction)
            {
                var innerType = (VectorType)types[matrixInstruction.ColumnType];
                types.Add(matrixInstruction.ResultId, new MatrixType(innerType.BaseType, innerType.Size, matrixInstruction.ColumnCount));
            }
            else if (instruction.Op == Op.OpTypeStruct && (OpTypeStruct)instruction is { } typeStructInstruction)
            {
                var structName = names[typeStructInstruction.ResultId];
                var fieldsData = typeStructInstruction.Values;
                var fields = new List<(string Name, SymbolType Type, TypeModifier TypeModifier)>();
                for (var index = 0; index < fieldsData.WordCount; index++)
                {
                    var fieldData = fieldsData.Words[index];
                    var type = types[fieldData];
                    var name = memberNames[(typeStructInstruction.ResultId, index)];
                    fields.Add((name, type, TypeModifier.None));
                }
                StructuredType structType = (blocks.Contains(typeStructInstruction.ResultId))
                    ? new ConstantBufferSymbol(structName.StartsWith("type.") ? structName.Substring("type.".Length) : throw new InvalidOperationException(), fields)
                    : new StructType(structName, fields);
                types.Add(typeStructInstruction.ResultId, structType);
            }
            else if (instruction.Op == Op.OpTypeArray && (OpTypeArray)instruction is { } typeArray)
            {
                var innerType = types[typeArray.ElementType];
                if (SpirvBuilder.TryGetConstantValue(typeArray.Length, out var arraySizeObject, buffer))
                {
                    types.Add(typeArray.ResultId, new ArrayType(innerType, (int)arraySizeObject, typeArray.Length));
                }
                else
                {
                    types.Add(typeArray.ResultId, new ArrayType(innerType, -1, typeArray.Length));
                }
            }
            else if (instruction.Op == Op.OpTypeRuntimeArray && (OpTypeRuntimeArray)instruction is { } typeRuntimeArray)
            {
                var innerType = types[typeRuntimeArray.ElementType];
                types.Add(typeRuntimeArray.ResultId, new ArrayType(innerType, -1));
            }
            else if (instruction.Op == Op.OpTypeFunctionSDSL && new OpTypeFunctionSDSL(instruction) is { } typeFunctionInstruction)
            {
                var returnType = types[typeFunctionInstruction.ReturnType];
                var parameterTypes = new List<(SymbolType Type, ParameterModifiers Flags)>();
                foreach (var operand in typeFunctionInstruction.Values)
                {
                    parameterTypes.Add((types[operand.Item1], (ParameterModifiers)operand.Item2));
                }
                types.Add(typeFunctionInstruction.ResultId, new FunctionType(returnType, parameterTypes));
            }
            else if (instruction.Op == Op.OpTypeImage && new OpTypeImage(instruction) is { } typeImage)
            {
                var sampledType = (ScalarType)types[typeImage.SampledType];
                if (typeImage.Dim == Dim.Buffer)
                {
                    types.Add(typeImage.ResultId, new BufferType(sampledType));
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

                    types.Add(typeImage.ResultId, textureType);
                }
            }
            else if (instruction.Op == Op.OpTypeSampler && new OpTypeSampler(instruction) is { } typeSampler)
            {
                types.Add(typeSampler.ResultId, new SamplerType());
            }
            else if (instruction.Op == Op.OpTypeGenericSDSL && (OpTypeGenericSDSL)instruction is { } typeGeneric)
            {
                types.Add(typeGeneric.ResultId, new GenericParameterType(typeGeneric.Kind));
            }
            // Unresolved content
            // This only happens during EvaluateInheritanceAndCompositions so it's not important to have all information valid
            else if (instruction.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)instruction is { } importShader)
            {
                var classSource = new ShaderClassInstantiation(importShader.ShaderName, importShader.Values.Elements.Memory.ToArray());
                var shaderSymbol = realShaderImporter.Import(classSource, buffer);

                types.Add(importShader.ResultId, shaderSymbol);
            }
            else if (instruction.Op == Op.OpSDSLImportStruct && (OpSDSLImportStruct)instruction is { } importStruct)
            {
                var shaderSymbol = (ShaderSymbol)types[importStruct.Shader];
                if (shaderSymbol is LoadedShaderSymbol loadedShaderSymbol)
                {
                    types.Add(importStruct.ResultId, loadedShaderSymbol.StructTypes.Single(x => x.Type.ToId() == importStruct.StructName).Type);
                }
                else
                {
                    types.Add(importStruct.ResultId, new StructType(importStruct.StructName, []));
                }
            }
        }

        // Second pass (for processing when info from first pass is needed)
        for (var i = start; i < end; i++)
        {
            var instruction = buffer[i];

            // Can be declared before OpTypeStruct, so done in second pass
            if (instruction.Op == Op.OpMemberDecorate && (OpMemberDecorate)instruction is { } memberDecorate)
            {
                var structType = (StructuredType)types[memberDecorate.StructureType];
                if (memberDecorate.Decoration == Decoration.ColMajor)
                    structType.Members[memberDecorate.Member] = structType.Members[memberDecorate.Member] with { TypeModifier = TypeModifier.ColumnMajor };
                else if (memberDecorate.Decoration == Decoration.RowMajor)
                    structType.Members[memberDecorate.Member] = structType.Members[memberDecorate.Member] with { TypeModifier = TypeModifier.RowMajor };
            }
        }
    }

    public class ShaderImporter(SymbolTable table) : IShaderImporter
    {
        public ShaderSymbol Import(ShaderClassInstantiation classSource, NewSpirvBuffer buffer)
        {
            return LoadAndCacheExternalShaderType(table, classSource, buffer);
        }
    }

    private static LoadedShaderSymbol CreateShaderType(SymbolTable table, NewSpirvBuffer buffer, ShaderClassInstantiation classSource)
    {
        ProcessNameAndTypes(buffer, 0, buffer.Count, out var names, out var types, new ShaderImporter(table));

        var variables = new List<(Symbol Symbol, VariableFlagsMask Flags)>();
        var methods = new List<(Symbol Symbol, FunctionFlagsMask Flags)>();
        var structTypes = new List<(StructuredType Type, int ImportedId)>();
        for (var index = 0; index < buffer.Count; index++)
        {
            var instruction = buffer[index];
            if (instruction.Op == Op.OpVariableSDSL && (OpVariableSDSL)instruction is { } variable &&
                variable.Storageclass != Specification.StorageClass.Function)
            {
                if (!names.TryGetValue(variable.ResultId, out var variableName))
                    variableName = $"_{variable.ResultId}";
                var variableType = types[variable.ResultType];

                var sid = new SymbolID(variableName, SymbolKind.Variable, Storage.Stream, IsStage: (variable.Flags & VariableFlagsMask.Stage) != 0);
                variables.Add((new(sid, variableType, 0), variable.Flags));
            }

            if (instruction.Op == Op.OpFunction)
            {
                var functionFlags = FunctionFlagsMask.None;
                if (buffer[index + 1].Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)buffer[index + 1] is { } functionInfo)
                    functionFlags = functionInfo.Flags;

                OpFunction functionInstruction = instruction;
                var functionName = names[functionInstruction.ResultId];
                var functionType = types[functionInstruction.FunctionType];

                var sid = new SymbolID(functionName, SymbolKind.Method, IsStage: (functionFlags & FunctionFlagsMask.Stage) != 0);
                methods.Add((new(sid, functionType, 0), functionFlags));
            }

            if (instruction.Op == Op.OpTypeStruct && (OpTypeStruct)instruction is { } typeStructInstruction)
            {
                structTypes.Add(((StructuredType)types[typeStructInstruction.ResultId], -1));
            }
        }

        var shaderType = new LoadedShaderSymbol(classSource.ClassName, classSource.GenericArguments)
        {
            Variables = variables,
            Methods = methods,
            StructTypes = structTypes,
        };
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
        if (Generics != null)
        {
            for (int i = 0; i < Generics.Parameters.Count; i++)
            {
                var genericParameter = Generics.Parameters[i];
                var genericParameterType = genericParameter.TypeName.ResolveType(table, context);
                table.DeclaredTypes.TryAdd(genericParameterType.ToString(), genericParameterType);

                var genericParameterTypeId = context.GetOrRegister(genericParameterType);
                context.Add(new OpSDSLGenericParameter(genericParameterTypeId, context.Bound));
                context.AddName(context.Bound, genericParameter.Name);
                table.CurrentFrame.Add(genericParameter.Name, new(new(genericParameter.Name, SymbolKind.ConstantGeneric), genericParameterType, context.Bound));

                openGenerics[i] = context.Bound;

                context.Bound++;
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
                    generics[i] = mixin.Generics.Values[i].CompileAsValue(table, compiler).Id;
                }
            }
            var shaderClassSource = new ShaderClassInstantiation(mixin.Name, generics);
            SpirvBuilder.BuildInheritanceList(table.ShaderLoader, shaderClassSource, table.CurrentMacros.AsSpan(), inheritanceList, ResolveStep.Compile, context.GetBuffer());
        }

        var shaderSymbols = new List<LoadedShaderSymbol>();
        foreach (var mixin in inheritanceList)
        {
            shaderSymbols.Add(mixin.Symbol = LoadAndCacheExternalShaderType(table, mixin));
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
                    arg.Type = new PointerType(argSym, Specification.StorageClass.Function);
                    ftype.ParameterTypes.Add((arg.Type, arg.Modifiers));
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
                    var shader = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, table.CurrentMacros.AsSpan(), ResolveStep.Compile, context.GetBuffer());
                    classSource.Buffer = shader;
                    var shaderType = LoadAndCacheExternalShaderType(table, classSource, context.GetBuffer());

                    // Resolve again (we don't use shaderType direclty, because it might lack info such as ArrayType)
                    memberType = svar.TypeName.ResolveType(table, context);
                }

                var storageClass = Specification.StorageClass.Private;
                if (memberType is TextureType || memberType is BufferType)
                    storageClass = Specification.StorageClass.UniformConstant;

                svar.Type = new PointerType(memberType, storageClass);
                table.DeclaredTypes.TryAdd(svar.Type.ToString(), svar.Type);
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

        var currentShader = new LoadedShaderSymbol(Name, openGenerics);
        RegisterShaderType(table, currentShader);

        table.CurrentShader = currentShader;
        table.InheritedShaders = inheritanceList;

        // If multiple cbuffer with same name, they will be merged
        // Still, we rename them internally to avoid name clashes (in HLSL name is skipped so it's OK, but for example OpSDSLImportStruct/OpSDSLImportVariable would be ambiguous)
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

        foreach (var member in Elements.OfType<ShaderStruct>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderBuffer>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderMember>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderSamplerState>())
            member.Compile(table, this, compiler);

        // In case calling a method not yet processed, we first register method types
        // (SPIR-V allow forward calling)
        foreach (var method in Elements.OfType<ShaderMethod>())
            method.Declare(table, this, compiler);
        foreach (var method in Elements.OfType<ShaderMethod>())
            method.Compile(table, this, compiler);

        table.InheritedShaders = null;
        table.CurrentShader = null;
        table.Pop();
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
            table.CurrentFrame.AddImplicitShader(shaderType);

        // Mark inherit
        context.Add(new OpSDSLMixinInherit(shaderId));
    }

    public static LoadedShaderSymbol LoadAndCacheExternalShaderType(SymbolTable table, ShaderClassInstantiation classSource)
    {
        // Already processed?
        if (table.DeclaredTypes.TryGetValue(classSource.ToClassName(), out var symbolType))
            return (LoadedShaderSymbol)symbolType;

        if (classSource.Buffer == null)
            throw new InvalidOperationException($"{nameof(classSource)}.{nameof(classSource.Buffer)} need to be set");

        var shaderType = LoadExternalShaderType(table, classSource);
        return shaderType;
    }

    public static LoadedShaderSymbol LoadAndCacheExternalShaderType(SymbolTable table, ShaderClassInstantiation classSource, NewSpirvBuffer parentBuffer)
    {
        // Already processed?
        if (table.DeclaredTypes.TryGetValue(classSource.ToClassName(), out var symbolType))
            return (LoadedShaderSymbol)symbolType;

        if (classSource.Buffer == null)
        {
            var shader = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, table.CurrentMacros.AsSpan(), ResolveStep.Compile, parentBuffer);
            classSource.Buffer = shader;
        }
        var shaderType = LoadExternalShaderType(table, classSource);
        return shaderType;
    }

    public static LoadedShaderSymbol LoadExternalShaderType(SymbolTable table, ShaderClassInstantiation classSource)
    {
        var shaderBuffer = classSource.Buffer;

        var shaderType = CreateShaderType(table, shaderBuffer, classSource);

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