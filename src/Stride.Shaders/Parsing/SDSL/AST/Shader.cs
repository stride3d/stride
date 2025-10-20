using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;




public class ShaderClass(Identifier name, TextLocation info) : ShaderDeclaration(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<Mixin> Mixins { get; set; } = [];

    // Note: We should make this method incremental (called many times in ShaderMixer)
    //       And possibly do the type deduplicating at the same time? (TypeDuplicateRemover)

    public static void ProcessNameAndTypes(NewSpirvBuffer buffer, int start, int end, out Dictionary<int, string> names, out Dictionary<int, SymbolType> types)
    {
        names = [];
        types = [];

        ProcessNameAndTypes(buffer, start, end, names, types);
    }

    public static void ProcessNameAndTypes(NewSpirvBuffer buffer, int start, int end, Dictionary<int, string> names, Dictionary<int, SymbolType> types)
    {
        var memberNames = new Dictionary<(int, int), string>();
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
                types.Add(intInstruction.ResultId, ScalarType.From("int"));
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
            else if (instruction.Op == Op.OpTypeStruct && (OpTypeStruct)instruction is { } typeStructInstruction)
            {
                var structName = names[typeStructInstruction.ResultId];
                var fieldsData = typeStructInstruction.Values;
                var fields = new List<(string Name, SymbolType Type)>();
                for (var index = 0; index < fieldsData.WordCount; index++)
                {
                    var fieldData = fieldsData.Words[index];
                    var type = types[fieldData];
                    var name = memberNames[(typeStructInstruction.ResultId, index)];
                    fields.Add((name, type));
                }
                types.Add(typeStructInstruction.ResultId, new StructType(structName, fields));
            }
            else if (instruction.Op == Op.OpTypeFunction && new OpTypeFunction(instruction) is { } typeFunctionInstruction)
            {
                var returnType = types[typeFunctionInstruction.ReturnType];
                var parameterTypes = new List<SymbolType>();
                foreach (var operand in typeFunctionInstruction.Values)
                {
                    parameterTypes.Add(types[operand]);
                }
                types.Add(typeFunctionInstruction.ResultId, new FunctionType(returnType, parameterTypes));
            }
            else if (instruction.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)instruction is { } importShader)
            {
                if (importShader.Type == ImportType.External)
                {
                    types.Add(importShader.ResultId, new ShaderSymbol(importShader.ShaderName, []));
                }
            }
        }
        foreach (var instruction in buffer)
        {
            if (instruction.Op == Op.OpSDSLImportFunction && (OpSDSLImportFunction)instruction is { } importFunction)
            {
                if (types.TryGetValue(importFunction.Shader, out var type) && type is ShaderSymbol shaderSymbol)
                {
                    var returnType = types[importFunction.ResultType];
                    var symbol = new Symbol(new(importFunction.FunctionName, SymbolKind.Method, FunctionFlags: importFunction.Flags), returnType, importFunction.ResultId);
                    // TODO: review if really necessary?
                    // (external functions are resolved differently)
                    shaderSymbol.Components.Add(symbol);
                }
            }
        }
    }

    private static ShaderSymbol CreateShaderType(NewSpirvBuffer buffer, string className)
    {
        ProcessNameAndTypes(buffer, 0, buffer.Count, out var names, out var types);

        var symbols = new List<Symbol>();
        for (var index = 0; index < buffer.Count; index++)
        {
            var instruction = buffer[index];
            if (instruction.Op == Op.OpVariable && (OpVariable)instruction is { } variable &&
                variable.Storageclass != Specification.StorageClass.Function)
            {
                if (!names.TryGetValue(variable.ResultId, out var variableName))
                    variableName = $"_{variable.ResultId}";
                var variableType = types[variable.ResultType];

                var sid = new SymbolID(variableName, SymbolKind.Variable, Storage.Stream);
                symbols.Add(new(sid, variableType, variable.ResultId));
            }

            if (instruction.Op == Op.OpFunction)
            {
                var functionFlags = FunctionFlagsMask.None;
                if (buffer[index + 1].Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)buffer[index + 1] is { } functionInfo)
                    functionFlags = functionInfo.Flags;

                OpFunction functionInstruction = instruction;
                var functionName = names[functionInstruction.ResultId];
                var functionType = types[functionInstruction.FunctionType];

                var sid = new SymbolID(functionName, SymbolKind.Method, FunctionFlags: functionFlags);
                symbols.Add(new(sid, functionType, functionInstruction.ResultId));
            }
        }

        var shaderType = new ShaderSymbol(className, symbols);
        return shaderType;
    }

    private static void RegisterShaderType(SymbolTable table, ShaderSymbol shaderType)
    {
        table.DeclaredTypes.Add(shaderType.Name, shaderType);
    }

    public void Compile(CompilerUnit compiler, SymbolTable table)
    {
        var (builder, context) = compiler;
        context.PutShaderName(Name);

        table.Push();

        var inheritanceList = new List<ShaderClassSource>();
        foreach (var mixin in Mixins)
        {
            SpirvBuilder.BuildInheritanceList(table.ShaderLoader, new ShaderClassSource(mixin.Name), inheritanceList);
        }

        foreach (var mixin in inheritanceList)
        {
            LoadExternalShaderType(table, mixin.ClassName);
        }

        var symbols = new List<Symbol>();
        foreach (var member in Elements)
        {
            if (member is ShaderMethod func)
            {
                var ftype = new FunctionType(func.ReturnTypeName.ResolveType(table), []);
                foreach (var arg in func.Parameters)
                {
                    var argSym = arg.TypeName.ResolveType(table);
                    table.DeclaredTypes.TryAdd(argSym.ToString(), argSym);
                    arg.Type = new PointerType(argSym, Specification.StorageClass.Function);
                    ftype.ParameterTypes.Add(arg.Type);
                }
                func.Type = ftype;

                table.DeclaredTypes.TryAdd(func.Type.ToString(), func.Type);
            }
            else if (member is ShaderMember svar)
            {
                if (!svar.TypeName.TryResolveType(table, out var memberType))
                {
                    memberType = LoadExternalShaderType(table, svar.TypeName.Name);

                    table.DeclaredTypes.TryAdd(memberType.ToString(), memberType);
                }

                svar.Type = new PointerType(memberType, Specification.StorageClass.Private);
                table.DeclaredTypes.TryAdd(svar.Type.ToString(), svar.Type);
            }
            else if (member is CBuffer cb)
            {
                foreach (var cbMember in cb.Members)
                {
                    cbMember.Type = cbMember.TypeName.ResolveType(table);
                    //var symbol = new Symbol(new(cbMember.Name, SymbolKind.CBuffer), cbMember.Type);
                    //symbols.Add(symbol);
                }
            }
            else if (member is ShaderSamplerState samplerState)
            {
                samplerState.Type = new SamplerType(samplerState.Name);
                table.DeclaredTypes.Add(samplerState.Type.ToString(), samplerState.Type);
            }
        }

        var currentShader = new ShaderSymbol(Name, symbols);
        RegisterShaderType(table, currentShader);

        table.CurrentShader = currentShader;
        foreach (var member in Elements)
        {
            member.ProcessSymbol(table);
        }

        foreach (var mixin in inheritanceList)
        {
            var shaderType = (ShaderSymbol)table.DeclaredTypes[mixin.ClassName];

            // Import types and variables/functions
            context.FluentAdd(new OpSDSLImportShader(context.Bound, new(shaderType.Name), ImportType.Inherit), out var shader);
            context.AddName(context.Bound, shaderType.Name);
            context.Bound++;

            foreach (var c in shaderType.Components)
            {
                if (c.Id.Kind == SymbolKind.Variable)
                {
                    var variableTypeId = context.GetOrRegister(c.Type);
                    context.FluentAdd(new OpSDSLImportVariable(variableTypeId, context.Bound, c.Id.Name, shader.ResultId), out var variable);
                    context.AddName(context.Bound, c.Id.Name);
                    context.Bound++;
                    table.CurrentFrame.Add(c.Id.Name, c with { IdRef = variable.ResultId });
                }
                else if (c.Id.Kind == SymbolKind.Method)
                {
                    var functionType = (FunctionType)c.Type;

                    var functionReturnTypeId = context.GetOrRegister(functionType.ReturnType);
                    context.FluentAdd(new OpSDSLImportFunction(functionReturnTypeId, context.Bound, c.Id.Name, shader.ResultId, c.Id.FunctionFlags), out var function);
                    context.AddName(context.Bound, c.Id.Name);
                    context.Bound++;
                    table.CurrentFrame.Add(c.Id.Name, c with { IdRef = function.ResultId });
                }
            }

            // Mark inherit
            context.Add(new OpSDSLMixinInherit(shader.ResultId));
        }

        foreach (var member in Elements.OfType<CBuffer>())
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

        table.CurrentShader = null;
        table.Pop();
    }

    private static ShaderSymbol LoadExternalShaderType(SymbolTable table, string className)
    {
        if (!table.ShaderLoader.LoadExternalBuffer(className, out var shaderBuffer))
            throw new InvalidOperationException($"Type [{className}] not found");

        var shaderType = CreateShaderType(shaderBuffer, className);

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