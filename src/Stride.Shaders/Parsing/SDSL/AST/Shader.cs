using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;




public class ShaderClass(Identifier name, TextLocation info) : ShaderDeclaration(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<Mixin> Mixins { get; set; } = [];

    public static Dictionary<int, SymbolType> ProcessNameAndTypes(NewSpirvBuffer buffer, out Dictionary<int, string> names, out Dictionary<int, SymbolType> types)
    {
        var memberNames = new Dictionary<(int, int), string>();
        names = [];
        types = [];
        foreach (var instruction in buffer)
        {
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
            else if (instruction.Op == Op.OpTypeVoid && (OpTypeVoid)instruction is {} voidInstruction)
            {
                types.Add(voidInstruction.ResultId, ScalarType.From("void"));
            }
            else if (instruction.Op == Op.OpTypeVector && (OpTypeVector)instruction is {} vectorInstruction)
            {
                var innerType = (ScalarType)types[vectorInstruction.ComponentType];
                types.Add(vectorInstruction.ResultId, new VectorType(innerType, vectorInstruction.ComponentCount));
            }
            else if (instruction.Op == Op.OpTypeStruct && (OpTypeStruct)instruction is {} typeStructInstruction)
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
            else if (instruction.Op == Op.OpTypeFunction && (OpTypeFunction)instruction is {} typeFunctionInstruction)
            {
                var returnType = types[typeFunctionInstruction.ReturnType];
                var parameterTypes = new List<SymbolType>();
                foreach (var operand in typeFunctionInstruction.Values)
                {
                    parameterTypes.Add(types[operand]);
                }
                types.Add(typeFunctionInstruction.ResultId, new FunctionType(returnType, parameterTypes));
            }
        }

        return types;
    }

    private static ShaderSymbol LoadShader(IExternalShaderLoader externalShaderLoader, Mixin mixin)
    {
        externalShaderLoader.LoadExternalBuffer(mixin.Name, out var buffer);

        ProcessNameAndTypes(buffer, out var names, out var types);

        var symbols = new List<Symbol>();
        foreach (var instruction in buffer)
        {
            if (instruction.Op == Op.OpVariable && (OpVariable)instruction is {} variableInstruction)
            {
                var variableName = names[variableInstruction.ResultId];
                var variableType = types[variableInstruction.ResultType];

                var sid = new SymbolID(variableName, SymbolKind.Variable, Storage.Stream);
                symbols.Add(new(sid, variableType, variableInstruction.ResultId));
            }

            if (instruction.Op == Op.OpFunction)
            {
                OpFunction functionInstruction = instruction;
                var functionName = names[functionInstruction.ResultId];
                var functionType = types[functionInstruction.FunctionType];

                var sid = new SymbolID(functionName, SymbolKind.Method);
                symbols.Add(new(sid, functionType, functionInstruction.ResultId));
            }
        }

        var shaderType = new ShaderSymbol(mixin.Name, symbols);
        return shaderType;
    }

    private static void RegisterShaderType(SymbolTable table, ShaderSymbol shaderType)
    {
        table.DeclaredTypes.Add(shaderType.Name, shaderType);
    }


    public void Compile(CompilerUnit compiler, SymbolTable table)
    {
        table.Push();
        foreach (var mixin in Mixins)
        {
            // Check if shader isn't already loaded as part of current bytecode
            var shaderType = LoadShader(table.ShaderLoader, mixin);

            RegisterShaderType(table, shaderType);
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
                    arg.Type = argSym;
                    ftype.ParameterTypes.Add(arg.Type);
                }
                func.Type = ftype;

                table.DeclaredTypes.TryAdd(func.Type.ToString(), func.Type);
            }
            else if (member is ShaderMember svar)
            {
                svar.Type = new PointerType(svar.TypeName.ResolveType(table), Specification.StorageClass.Private);
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
        }

        var currentShader = new ShaderSymbol(Name, symbols);
        RegisterShaderType(table, currentShader);

        table.CurrentShader = currentShader;
        foreach (var member in Elements)
        {
            member.ProcessSymbol(table);
        }

        var (builder, context, _) = compiler;
        context.PutShaderName(Name);

        foreach (var mixin in Mixins)
        {
            // Import types and variables/functions
            context.FluentAdd(new OpSDSLImportShader(context.Bound++, new(mixin.Name)), out var shader);

            var shaderType = (ShaderSymbol)table.DeclaredTypes[mixin.Name];

            foreach (var c in shaderType.Components)
            {
                if (c.Id.Kind == SymbolKind.Variable)
                {
                    var variableTypeId = context.GetOrRegister(c.Type);
                    context.FluentAdd(new OpSDSLImportVariable(variableTypeId, context.Bound++, c.Id.Name, shader.ResultId), out var variable);
                    context.Module.InheritedVariables.Add(c.Id.Name, new(variable.ResultId, variable.ResultType, variable.VariableName));
                    table.CurrentFrame.Add(c.Id.Name, c with { IdRef = variable.ResultId });
                }
                else if (c.Id.Kind == SymbolKind.Method)
                {
                    var functionType = (FunctionType)c.Type;

                    var functionReturnTypeId = context.GetOrRegister(functionType.ReturnType);
                    context.FluentAdd(new OpSDSLImportFunction(functionReturnTypeId, context.Bound++, c.Id.Name, shader.ResultId), out var function);
                    context.Module.InheritedFunctions.Add(c.Id.Name, new(function.ResultId, c.Id.Name, functionType));
                    table.CurrentFrame.Add(c.Id.Name, c with { IdRef = function.ResultId });
                }
            }

            // Mark inherit
            context.Add(new OpSDSLMixinInherit(shader.ResultId));
            context.Module.InheritedMixins.Add(shaderType);
        }

        foreach (var member in Elements.OfType<CBuffer>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderMember>())
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