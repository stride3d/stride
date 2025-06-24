using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Runtime.InteropServices;

namespace Stride.Shaders.Parsing.SDSL.AST;




public class ShaderClass(Identifier name, TextLocation info) : ShaderDeclaration(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<Mixin> Mixins { get; set; } = [];

    public static Dictionary<int, SymbolType> ProcessNameAndTypes(SpirvBuffer buffer, out Dictionary<int, string> names, out Dictionary<int, SymbolType> types)
    {
        var memberNames = new Dictionary<(int, int), string>();
        names = new Dictionary<int, string>();
        types = new Dictionary<int, SymbolType>();
        foreach (var instruction in buffer.Instructions)
        {
            if (instruction.OpCode == SDSLOp.OpName)
            {
                var nameInstruction = instruction.UnsafeAs<InstOpName>();
                names.Add(nameInstruction.Target, nameInstruction.Name.Value);
            }
            else if (instruction.OpCode == SDSLOp.OpMemberName)
            {
                var nameInstruction = instruction.UnsafeAs<InstOpMemberName>();
                memberNames.Add((nameInstruction.Type, (int)nameInstruction.Member.Words), nameInstruction.Name.Value);
            }
            else if (instruction.OpCode == SDSLOp.OpTypeFloat)
            {
                var floatInstruction = instruction.UnsafeAs<InstOpTypeFloat>();
                //if (floatInstruction.FloatingPointEncoding != 0)
                //    throw new InvalidOperationException();

                types.Add(floatInstruction.ResultId, floatInstruction.Width.Words switch
                {
                    16 => ScalarType.From("half"),
                    32 => ScalarType.From("float"),
                    64 => ScalarType.From("double"),
                });
            }
            else if (instruction.OpCode == SDSLOp.OpTypeVoid)
            {
                types.Add(instruction.ResultId!.Value, ScalarType.From("void"));
            }
            else if (instruction.OpCode == SDSLOp.OpTypeVector)
            {
                var vectorInstruction = instruction.UnsafeAs<InstOpTypeVector>();
                var innerType = (ScalarType)types[vectorInstruction.ComponentType];
                types.Add(instruction.ResultId!.Value, new VectorType(innerType, (int)vectorInstruction.ComponentCount.Words));
            }
            else if (instruction.OpCode == SDSLOp.OpTypeStruct)
            {
                var typeStructInstruction = instruction.UnsafeAs<InstOpTypeStruct>();
                var structName = names[instruction.ResultId!.Value];
                var fields = new List<(string Name, SymbolType Type)>();
                throw new NotImplementedException();
                types.Add(instruction.ResultId!.Value, new StructType(structName, fields));
            }
            else if (instruction.OpCode == SDSLOp.OpTypeFunction)
            {
                var typeFunctionInstruction = instruction.UnsafeAs<InstOpTypeFunction>();
                var returnType = types[typeFunctionInstruction.ReturnType];
                var parameterTypes = new List<SymbolType>();
                foreach (var operand in instruction.Operands[2..])
                {
                    parameterTypes.Add(types[operand]);
                }
                types.Add(instruction.ResultId!.Value, new FunctionType(returnType, parameterTypes));
            }
        }

        return types;
    }

    private static ShaderSymbol LoadShader(IExternalShaderLoader externalShaderLoader, Mixin mixin)
    {
        externalShaderLoader.LoadExternalReference(mixin.Name, out var bytecode);
        var buffer = new SpirvBuffer(MemoryMarshal.Cast<byte, int>(bytecode));

        ProcessNameAndTypes(buffer, out var names, out var types);

        var symbols = new List<Symbol>();
        foreach (var instruction in buffer.Instructions)
        {
            if (instruction.OpCode == SDSLOp.OpVariable)
            {
                var variableInstruction = instruction.UnsafeAs<InstOpVariable>();
                var variableName = names[variableInstruction.ResultId.Value];
                var variableType = types[variableInstruction.ResultType];

                var sid = new SymbolID(variableName, variableInstruction.ResultId, SymbolKind.Variable, Storage.Stream);
                symbols.Add(new(sid, variableType));
            }

            if (instruction.OpCode == SDSLOp.OpFunction)
            {
                var functionInstruction = instruction.UnsafeAs<InstOpFunction>();
                var functionName = names[functionInstruction.ResultId.Value];
                var functionType = types[functionInstruction.FunctionType];

                var sid = new SymbolID(functionName, functionInstruction.ResultId, SymbolKind.Method);
                symbols.Add(new(sid, functionType));
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
                svar.Type = svar.TypeName.ResolveType(table);
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
            var shader = context.Buffer.AddOpSDSLImportShader(context.Bound++, new(mixin.Name));

            var shaderType = (ShaderSymbol)table.DeclaredTypes[mixin.Name];

            foreach (var c in shaderType.Components)
            {
                if (c.Id.Kind == SymbolKind.Variable)
                {
                    var variableTypeId = context.GetOrRegister(c.Type);
                    var variable = context.Buffer.AddOpSDSLImportVariable(context.Bound++, variableTypeId, c.Id.Name, shader);
                    context.Module.InheritedVariables.Add(c.Id.Name, new(variable, c.Id.Name));
                    table.CurrentFrame.Add(c.Id.Name, c with { Id = c.Id with { IdRef = variable.ResultId.Value } });
                }
                else if (c.Id.Kind == SymbolKind.Method)
                {
                    var functionType = (FunctionType)c.Type;

                    var functionReturnTypeId = context.GetOrRegister(functionType.ReturnType);
                    var function = context.Buffer.AddOpSDSLImportFunction(context.Bound++, functionReturnTypeId, c.Id.Name, shader);
                    context.Module.InheritedFunctions.Add(c.Id.Name, new(function.ResultId.Value, c.Id.Name, functionType));
                    table.CurrentFrame.Add(c.Id.Name, c with { Id = c.Id with { IdRef = function.ResultId.Value } });
                }
            }

            // Mark inherit
            context.Buffer.AddOpSDSLMixinInherit(shader);
            context.Module.InheritedMixins.Add(shaderType);
        }

        foreach (var member in Elements.OfType<CBuffer>())
            member.Compile(table, this, compiler);
        foreach (var member in Elements.OfType<ShaderMember>())
            member.Compile(table, this, compiler);
        foreach(var method in Elements.OfType<ShaderMethod>())
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